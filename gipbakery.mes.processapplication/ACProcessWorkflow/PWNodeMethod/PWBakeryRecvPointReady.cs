using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Receiving point ready'}de{'Abnahmestelle bereit'}", Global.ACKinds.TPWNodeStatic, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryRecvPointReady : PWNodeUserAck
    {
        #region c'tors

        static PWBakeryRecvPointReady()
        {
            RegisterExecuteHandler(typeof(PWBakeryRecvPointReady), HandleExecuteACMethod_PWBakeryRecvPointReady);

            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("MessageText", typeof(string), "", Global.ParamOption.Required));
            paramTranslation.Add("MessageText", "en{'Question text'}de{'Abfragetext'}");

            method.ParameterValueList.Add(new ACValue("AckOverScale", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("AckOverScale", "en{'Acknowledge over scale target weight'}de{'Quittieren über Waagenzielgewicht'}");

            method.ParameterValueList.Add(new ACValue("AckScaleWeight", typeof(double), 0, Global.ParamOption.Optional));
            paramTranslation.Add("AckScaleWeight", "en{'Scale weight for auto acknowledge [kg]'}de{'Waagengewicht für automatische Quittierung [kg]'}");

            method.ParameterValueList.Add(new ACValue("PasswordDlg", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("PasswordDlg", "en{'With password dialogue'}de{'Mit Passwort-Dialog'}");

            var wrapper = new ACMethodWrapper(method, "en{'Receiving point ready'}de{'Abnahmestelle bereit'}", typeof(PWBakeryRecvPointReady), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryRecvPointReady), ACStateConst.SMStarting, wrapper);
        }

        public PWBakeryRecvPointReady(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            ResetMembers();
            return base.ACDeInit(deleteACClassTask);
        }

        public new const string PWClassName = nameof(PWBakeryRecvPointReady);

        #endregion

        #region Properties

        protected bool AckOverScale
        {
            get
            {
                var method = MyConfiguration;
                if (method == null)
                    return false;

                ACValue acValue = method.ParameterValueList.GetACValue("AckOverScale");
                if (acValue == null)
                    return false;

                return acValue.ParamAsBoolean;
            }
        }

        protected double AckScaleWeight
        {
            get
            {
                var method = MyConfiguration;
                if (method == null)
                    return 0;

                ACValue acValue = method.ParameterValueList.GetACValue("AckScaleWeight");
                if (acValue == null)
                    return 0;

                return acValue.ParamAsDouble;
            }
        }

        private List<ACRef<PAEScaleBase>> _AckScales;

        private bool? _DischargeOverHose = null;

        #endregion

        #region Methods

        public override void Start()
        {
            base.Start();
        }

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            if (Root == null || !Root.Initialized)
            {
                SubscribeToProjectWorkCycle();
                return;
            }

            bool ack = AckRecvPointReadyOverTempCalc();
            if (ack)
                AckStart();

            AttachToRecvPointReadyScale();
            base.SMStarting();
        }

        public override void SMRunning()
        {
            if (Root == null || !Root.Initialized)
            {
                SubscribeToProjectWorkCycle();
                return;
            }

            bool ack = AckRecvPointReadyOverTempCalc();
            if (ack)
                AckStart();

            AttachToRecvPointReadyScale();
            base.SMRunning();
        }

        private void AttachToRecvPointReadyScale()
        {
            if (!_DischargeOverHose.HasValue)
                _DischargeOverHose = false;

            if (AckOverScale)
            {
                if (_AckScales == null)
                {
                    PAMPlatformscale platScale = ParentPWGroup?.AccessedProcessModule as PAMPlatformscale;
                    if (platScale != null)
                    {
                        List<PAEScaleBase> detectionScales = platScale.GetWeightDetectionScales();
                        if (detectionScales == null || !detectionScales.Any())
                        {
                            //TODO alarm
                            UnSubscribeToProjectWorkCycle();
                            return;
                        }

                        _AckScales = new List<ACRef<PAEScaleBase>>();
                        foreach (PAEScaleBase detScale in detectionScales)
                        {
                            if (AckScaleWeight > 0.000001 && !detScale.IsBinPlaced.HasValue)
                                continue;

                            if (AckScaleWeight < -0.000001 && !detScale.IsBinRemoved.HasValue)
                                continue;

                            _AckScales.Add(new ACRef<PAEScaleBase>(detScale, this));
                        }
                    }
                }

                AckStartOverWeight();
                
            }
        }

        private bool AckRecvPointReadyOverTempCalc()
        {
            if (!AckOverScale)
                return false;

            PWBakeryTempCalc bakeryTempCalc = ParentPWGroup.FindChildComponents<PWBakeryTempCalc>(c => c is PWBakeryTempCalc).FirstOrDefault();
            if (bakeryTempCalc == null)
                return false;

            if (AckScaleWeight > 0.000001)
            {
                bool? result = bakeryTempCalc.IsFirstItemForDosingInPicking();
                return result.HasValue && !result.Value;
            }
            else
            {
                bool? result = bakeryTempCalc.IsLastItemForDosingInPicking();
                return result.HasValue && !result.Value;
            }
        }

        private DateTime _LastCheck = DateTime.MinValue;

        private bool AckStartOverWeight()
        {
            if (_LastCheck > DateTime.MinValue
                && DateTime.Now < _LastCheck.AddSeconds(5))
                return false;

            _LastCheck = DateTime.Now;

            if (_AckScales == null || !_AckScales.Any())
            {
                return false;
            }

            if (AckScaleWeight > 0.0000001 && _AckScales.Select(c => c.ValueT).All(c => c.IsBinPlaced.HasValue && c.IsBinPlaced.Value))
            {
                AckStart();
                return true;
            }
            else if (AckScaleWeight < -0.0000001 && _AckScales.Select(c => c.ValueT).All(c => c.IsBinRemoved.HasValue && c.IsBinRemoved.Value))
            {
                AckStart();
                return true;
            }

            SubscribeToProjectWorkCycle();

            return false;
        }

        public override void SMIdle()
        {
            ResetMembers();
            base.SMIdle();
        }

        public override void Recycle(IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
        {
            base.Recycle(content, parentACObject, parameter, acIdentifier);
            ResetMembers();
        }

        private void ResetMembers()
        {
            //if (_AckScale != null)
            //{
            //    if (AckScale != null)
            //    {
            //        AckScale.ActualValue.PropertyChanged -= ActualValue_PropertyChanged;
            //        AckScale.NotStandStill.PropertyChanged -= ActualValue_PropertyChanged;
            //    }
            //    _AckScale.Detach();
            //    _AckScale = null;
            //}

            _LastCheck = DateTime.MinValue;

            if (_AckScales != null && _AckScales.Any())
            {
                foreach(var scaleRef in _AckScales)
                {
                    scaleRef.Detach();
                }
            }

            _AckScales = null;
            _DischargeOverHose = null;
        }

        public static bool HandleExecuteACMethod_PWBakeryRecvPointReady(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            //switch (acMethodName)
            //{
            //    case MN_AckStartClient:
            //        AckStartClient(acComponent);
            //        return true;
            //    case Const.IsEnabledPrefix + MN_AckStartClient:
            //        result = IsEnabledAckStartClient(acComponent);
            //        return true;
            //}
            return HandleExecuteACMethod_PWNodeUserAck(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }


        public bool IsDischargeOverHose()
        {
            PWBakeryDischargingSingleDos disch = FindSuccessors<PWBakeryDischargingSingleDos>(true, c => c is PWBakeryDischargingSingleDos).FirstOrDefault();
            if (disch == null)
                return false;

            BakeryReceivingPoint recvPoint = ParentPWGroup?.AccessedProcessModule as BakeryReceivingPoint;
            if (recvPoint == null)
                return false;

            ACMethod dischMethod = disch.ContentACClassWF.RefPAACClassMethod.TypeACSignature();
            disch.GetConfigForACMethod(dischMethod, true, null);

            ACValue dest = dischMethod.ParameterValueList.GetACValue("Destination");
            if (dest != null)
            {
                short target = dest.ParamAsInt16;
                if (target == recvPoint.HoseDestination)
                    return true;
            }

            return false;
        }

        protected override void DumpPropertyList(XmlDocument doc, XmlElement xmlACPropertyList)
        {
            base.DumpPropertyList(doc, xmlACPropertyList);
        }

        #endregion
    }
}
