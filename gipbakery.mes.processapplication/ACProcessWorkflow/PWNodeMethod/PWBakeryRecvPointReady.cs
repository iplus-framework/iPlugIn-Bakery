using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

            method.ParameterValueList.Add(new ACValue("AutoTareScales", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("AutoTareScales", "en{'Auto tare scales'}de{'Automatische Waagenterierung'}");

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

        protected bool AutoTareScales
        {
            get
            {
                var method = MyConfiguration;
                if (method == null)
                    return false;

                ACValue acValue = method.ParameterValueList.GetACValue("AutoTareScales");
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

        protected ACMonitorObject _70100_ScaleLock = new ACMonitorObject(70100);
        private List<ACRef<PAEScaleBase>> _AckScales;
        public IEnumerable<PAEScaleBase> AckScales
        {
            get
            {
                using (ACMonitor.Lock(_70100_ScaleLock))
                {
                    if (_AckScales == null)
                        return new PAEScaleBase[] { };
                    return _AckScales.Select(c => c.ValueT).ToArray();
                }
            }
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

            bool ackOverScale = AckOverScale;
            bool autoTareScales = AutoTareScales;
            if (!ackOverScale && !autoTareScales)
                return;

            List<ACRef<PAEScaleBase>> ackScales = null;
            using (ACMonitor.Lock(_70100_ScaleLock))
            {
                ackScales = _AckScales;
            }
            if (ackScales == null)
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

                    ackScales = new List<ACRef<PAEScaleBase>>();
                    foreach (PAEScaleBase detScale in detectionScales)
                    {
                        if (autoTareScales)
                        {
                            var gravScale = detScale as PAEScaleGravimetric;
                            if (gravScale != null)
                                gravScale.Tare();
                        }
                        if (ackOverScale)
                        {
                            if ((AckScaleWeight > 0.000001 && detScale.WeightPlacedBin.HasValue)
                                || (AckScaleWeight < -0.000001 && detScale.WeightRemovedBin.HasValue))
                            {
                                ackScales.Add(new ACRef<PAEScaleBase>(detScale, this));
                                detScale.ActualValue.PropertyChanged += ActualValue_PropertyChanged;
                                detScale.NotStandStill.PropertyChanged += ActualValue_PropertyChanged;
                            }
                        }
                    }

                    using (ACMonitor.Lock(_70100_ScaleLock))
                    {
                        _AckScales = ackScales;
                    }
                }
            }
            if (ackOverScale)
            {
                if (!AckStartOverWeight(true))
                    SubscribeToProjectWorkCycle();
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

        private short _CycleCounter = 0;

        private void ActualValue_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT && (CurrentACState == ACStateEnum.SMStarting || CurrentACState == ACStateEnum.SMRunning))
            {
                AckStartOverWeight(false);
            }
        }

        private bool AckStartOverWeight(bool cyclicCall)
        {
            if (cyclicCall)
            {
                using (ACMonitor.Lock(_70100_ScaleLock))
                {
                    if (_CycleCounter > 0)
                    {
                        _CycleCounter--;
                        return false;
                    }
                }
            }

            if (CheckIfBinsRemovedOrPlaced())
            {
                bool doAck = false;
                using (ACMonitor.Lock(_70100_ScaleLock))
                {
                    if (_CycleCounter <= 10)
                    {
                        _CycleCounter = 30;
                        doAck = true;
                    }
                }

                if (doAck && (CurrentACState == ACStateEnum.SMStarting || CurrentACState == ACStateEnum.SMRunning))
                    AckStart();
                return true;
            }

            if (cyclicCall)
            {
                using (ACMonitor.Lock(_70100_ScaleLock))
                {
                    _CycleCounter = 10;
                }
            }
            return false;
        }

        private bool CheckIfBinsRemovedOrPlaced()
        {
            var scales = AckScales;
            if (scales == null || !scales.Any())
                return false;

            return (AckScaleWeight > 0.0000001 && scales.All(c => c.IsBinPlaced.HasValue && c.IsBinPlaced.Value))
                || (AckScaleWeight < -0.0000001 && scales.All(c => c.IsBinRemoved.HasValue && c.IsBinRemoved.Value));
        }

        public override void AckStart()
        {
            BakeryReceivingPoint receivingPoint = ParentPWGroup?.AccessedProcessModule as BakeryReceivingPoint;
            if (receivingPoint != null && (AckScaleWeight > 0.0000001 || AckScaleWeight < -0.0000001))
                receivingPoint.StoreGrossWeightOfEmptyContainer(AckScaleWeight < -0.0000001);

            UnSubscribeToProjectWorkCycle();
            base.AckStart();
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
            using (ACMonitor.Lock(_70100_ScaleLock))
            {
                _CycleCounter = 0;
            }
            using (ACMonitor.Lock(_70100_ScaleLock))
            {
                if (_AckScales != null && _AckScales.Any())
                {
                    foreach (var scaleRef in _AckScales)
                    {
                        if (scaleRef.ValueT != null)
                        {
                            scaleRef.ValueT.ActualValue.PropertyChanged -= ActualValue_PropertyChanged;
                            scaleRef.ValueT.NotStandStill.PropertyChanged -= ActualValue_PropertyChanged;
                        }
                        scaleRef.Detach();
                    }
                }
                _AckScales = null;
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

        public override bool IsSkippable
        {
            get
            {
                return !AckOverScale;
            }
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
