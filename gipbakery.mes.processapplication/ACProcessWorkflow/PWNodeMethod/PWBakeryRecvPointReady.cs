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

        public new const string PWClassName = "PWBakeryRecvPointReady";

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

        private ACRef<PAEScaleBase> _AckScale;
        public PAEScaleBase AckScale
        {
            get => _AckScale?.ValueT;
        }
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
            bool ack = AckRecvPointReadyOverTempCalc();
            if (ack)
                AckStart();

            AttachToRecvPointReadyScale();
            base.SMStarting();
        }

        public override void SMRunning()
        {
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

            if (AckOverScale
                && (AckScaleWeight > 0.0000001 || AckScaleWeight < -0.0000001)
                && _AckScale == null)
            {
                BakeryReceivingPoint recvPoint = ParentPWGroup?.AccessedProcessModule as BakeryReceivingPoint;
                if (recvPoint != null)
                {
                    PAEScaleBase scale = recvPoint.GetRecvPointReadyScale();
                    if (scale != null)
                    {
                        double ackScaleWeight = Math.Abs(AckScaleWeight);
                        if ((scale.MaxScaleWeight.ValueT > 0.00001 && scale.MaxScaleWeight.ValueT < 0.00001) && scale.MaxScaleWeight.ValueT < ackScaleWeight)
                        {
                            //Error50429: The maximum scale weight is too low. Acknowledge scale weight is {0} kg and maximum scale weight is {1} kg.
                            Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "SMRunning(10)", 100, "Error50429");
                            if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                            {
                                OnNewAlarmOccurred(ProcessAlarm, msg);
                                Root.Messages.LogMessageMsg(msg);
                            }
                        }
                        else
                        {
                            _AckScale = new ACRef<PAEScaleBase>(scale, this);

                            if (AckStartOverWeight())
                            {
                                CurrentACState = ACStateEnum.SMCompleted;
                                return;
                            }

                            AckScale.ActualValue.PropertyChanged += ActualValue_PropertyChanged;
                        }
                    }
                }
                UnSubscribeToProjectWorkCycle();
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

        private void ActualValue_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                AckStartOverWeight();
            }
        }

        private bool AckStartOverWeight()
        {
            if (AckScale == null)
                return false;

            if ((AckScaleWeight > 0.0000001 && AckScale.ActualValue.ValueT >= AckScaleWeight)
             || (AckScaleWeight < -0.0000001 && AckScale.ActualValue.ValueT < Math.Abs(AckScaleWeight))
                )
            {
                AckStart(); //TODO:calming time
                return true;
            }
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
            if (_AckScale != null)
            {
                if (AckScale != null)
                    AckScale.ActualValue.PropertyChanged -= ActualValue_PropertyChanged;
                _AckScale.Detach();
                _AckScale = null;
            }
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
