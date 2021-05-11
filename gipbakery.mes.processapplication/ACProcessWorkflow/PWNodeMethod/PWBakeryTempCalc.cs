using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Dough temperature calculato'}de{'Teigtemperaturberechnung'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryTempCalc : PWNodeProcessMethod
    {
        public const string PWClassName = "PWBakeryTempCalc";

        #region Constructors

        static PWBakeryTempCalc()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();
            method.ParameterValueList.Add(new ACValue("MessageText", typeof(string), "", Global.ParamOption.Required));
            paramTranslation.Add("MessageText", "en{'Question text'}de{'Abfragetext'}");
            method.ParameterValueList.Add(new ACValue("PasswordDlg", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("PasswordDlg", "en{'With password dialogue'}de{'Mit Passwort-Dialog'}");
            method.ParameterValueList.Add(new ACValue("DoughTemp", typeof(double?), false, Global.ParamOption.Required));
            paramTranslation.Add("DoughTemp", "en{'Doughtemperature °C'}de{'Teigtemperatur °C'}");
            method.ParameterValueList.Add(new ACValue("WaterTemp", typeof(double?), false, Global.ParamOption.Required));
            paramTranslation.Add("WaterTemp", "en{'Watertemperature °C'}de{'Wassertemperatur °C'}");
            method.ParameterValueList.Add(new ACValue("UseWaterTemp", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("UseWaterTemp", "en{'Use Watertemperature'}de{'Wassertemperatur verwenden'}");
            var wrapper = new ACMethodWrapper(method, "en{'User Acknowledge'}de{'Benutzerbestätigung'}", typeof(PWBakeryTempCalc), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryTempCalc), ACStateConst.SMStarting, wrapper);


            RegisterExecuteHandler(typeof(PWBakeryTempCalc), HandleExecuteACMethod_PWBakeryTempCalc);
        }

        public PWBakeryTempCalc(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            ClearMyConfiguration();
            return base.ACDeInit(deleteACClassTask);
        }

        public override void Recycle(IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
        {
            ClearMyConfiguration();
            base.Recycle(content, parentACObject, parameter, acIdentifier);
        }

        #endregion

        #region Properties
        public T ParentPWMethod<T>() where T : PWMethodVBBase
        {
            if (ParentRootWFNode == null)
                return null;
            return ParentRootWFNode as T;
        }

        //TODO: clear config on idle or recycle
        private ACMethod _MyConfiguration;
        [ACPropertyInfo(9999)]
        public ACMethod MyConfiguration
        {
            get
            {
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    if (_MyConfiguration != null)
                        return _MyConfiguration;
                }

                var myNewConfig = NewACMethodWithConfiguration();
                _MyConfiguration = myNewConfig;
                return myNewConfig;
            }
        }

        public bool IsProduction
        {
            get
            {
                return ParentPWMethod<PWMethodProduction>() != null;
            }
        }

        protected double? DoughTemp
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("DoughTemp");
                    if (acValue != null)
                    {
                        return (double?) acValue.Value;
                    }
                }
                return null;
            }
        }

        protected double? WaterTemp
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("WaterTemp");
                    if (acValue != null)
                    {
                        return (double?)acValue.Value;
                    }
                }
                return null;
            }
        }

        protected bool UseWaterTemp
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("UseWaterTemp");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }

        #endregion


        #region Methods

        #region Execute-Helper-Handlers
        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            //result = null;
            //switch (acMethodName)
            //{
            //}
            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        public static bool HandleExecuteACMethod_PWBakeryTempCalc(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
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
            return HandleExecuteACMethod_PWBaseNodeProcess(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
        #endregion

        public override void Start()
        {
            base.Start();
        }

        public override void SMIdle()
        {
            ClearMyConfiguration();
            base.SMIdle();
        }

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            double temp = 0;
            bool result = GetKneedingRiseTemperature(out temp);

            if (!result)
                return;


            base.SMStarting();
        }

        public override void SMRunning()
        {
            CalculateTargetTemperature();

            base.SMRunning();
        }

        [ACMethodState("en{'Completed'}de{'Beendet'}", 40, true)]
        public override void SMCompleted()
        {
            base.SMCompleted();
        }

        public override void Reset()
        {
            ClearMyConfiguration();
            base.Reset();
        }

        [ACMethodInteractionClient("", "en{'Acknowledge'}de{'Bestätigen'}", 450, false)]
        new public static void AckStartClient(IACComponent acComponent)
        {
            ACComponent _this = acComponent as ACComponent;
            if (!IsEnabledAckStartClient(acComponent))
                return;
            ACStateEnum acState = (ACStateEnum)_this.ACUrlCommand("ACState");

            // TODO: Open Businesobject with calculation of water components
            string result = _this.Messages.InputBox("Temperatur", "0.0");

            // If needs Password
            if (acState == ACStateEnum.SMStarting)
            {
                string bsoName = "BSOChangeMyPW";
                ACBSO childBSO = acComponent.Root.Businessobjects.ACUrlCommand("?" + bsoName) as ACBSO;
                if (childBSO == null)
                    childBSO = acComponent.Root.Businessobjects.StartComponent(bsoName, null, new object[] { }) as ACBSO;
                if (childBSO == null)
                    return;
                VBDialogResult dlgResult = childBSO.ACUrlCommand("!ShowCheckUserDialog") as VBDialogResult;
                if (dlgResult != null && dlgResult.SelectedCommand == eMsgButton.OK)
                {
                    acComponent.ACUrlCommand("!AckStart");
                }
                childBSO.Stop();
            }
            else
                acComponent.ACUrlCommand("!AckStart");
        }

        new public static bool IsEnabledAckStartClient(IACComponent acComponent)
        {
            ACComponent _this = acComponent as ACComponent;
            ACStateEnum acState = (ACStateEnum)_this.ACUrlCommand("ACState");
            return acState == ACStateEnum.SMRunning || acState == ACStateEnum.SMStarting;
        }


        protected override void DumpPropertyList(XmlDocument doc, XmlElement xmlACPropertyList)
        {
            base.DumpPropertyList(doc, xmlACPropertyList);

            XmlElement xmlChild = xmlACPropertyList["DoughTemp"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("DoughTemp");
                if (xmlChild != null)
                    xmlChild.InnerText = DoughTemp.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["WaterTemp"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("WaterTemp");
                if (xmlChild != null)
                    xmlChild.InnerText = WaterTemp.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }
        }

        public void ClearMyConfiguration()
        {
            using (ACMonitor.Lock(_20015_LockValue))
            {
                _MyConfiguration = null;
            }
        }

        private void CalculateTargetTemperature()
        {
            if (!UseWaterTemp)
            {
                double kneedingRiseTemperature = 0;
                bool kneedingTempResult = GetKneedingRiseTemperature(out kneedingRiseTemperature);
                if (!kneedingTempResult)
                    return;
            }


        }

        //TODO: half quantity
        private bool GetKneedingRiseTemperature(out double kneedingTemperature)
        {
            kneedingTemperature = 0;

            var kneedingNodes = RootPW.FindChildComponents<PWBakeryKneading>();

            if (kneedingNodes != null && kneedingNodes.Any())
            {
                if (kneedingNodes.Count > 1)
                {
                    //TODO: alarm
                    return false;
                }

                PWBakeryKneading kneedingNode = kneedingNodes.FirstOrDefault();

                ACMethod kneedingNodeConfiguration = kneedingNode.MyConfiguration;
                if (kneedingNodeConfiguration == null)
                {
                    //TOOD alarm
                    return false;
                }

                double temperatureRiseSlow = 0, temperatureRiseFast = 0;
                TimeSpan kneedingSlow = TimeSpan.Zero, kneedingFast = TimeSpan.Zero;

                bool fullQuantity = true;

                // Full quantity
                if (fullQuantity)
                {
                    ACValue tempRiseSlow = kneedingNodeConfiguration.ParameterValueList.GetACValue("TempRiseSlow");
                    if (tempRiseSlow != null)
                        temperatureRiseSlow = tempRiseSlow.ParamAsDouble;

                    ACValue tempRiseFast = kneedingNodeConfiguration.ParameterValueList.GetACValue("TempRiseFast");
                    if (tempRiseFast != null)
                        temperatureRiseFast = tempRiseFast.ParamAsDouble;

                    ACValue kTimeSlow = kneedingNodeConfiguration.ParameterValueList.GetACValue("KneadingTimeSlow");
                    if (kTimeSlow != null)
                        kneedingSlow = kTimeSlow.ParamAsTimeSpan;

                    ACValue kTimeFast = kneedingNodeConfiguration.ParameterValueList.GetACValue("KneadingTimeFast");
                    if (kTimeFast != null)
                        kneedingFast = kTimeFast.ParamAsTimeSpan;

                    kneedingTemperature = (kneedingSlow.TotalMinutes * temperatureRiseSlow) + (kneedingFast.TotalMinutes * temperatureRiseFast);
                }
                // Half quantity
                else
                {
                    ACValue tempRiseSlow = kneedingNodeConfiguration.ParameterValueList.GetACValue("TempRiseSlowHalf");
                    if (tempRiseSlow != null)
                        temperatureRiseSlow = tempRiseSlow.ParamAsDouble;

                    ACValue tempRiseFast = kneedingNodeConfiguration.ParameterValueList.GetACValue("TempRiseFastHalf");
                    if (tempRiseFast != null)
                        temperatureRiseFast = tempRiseFast.ParamAsDouble;

                    ACValue kTimeSlow = kneedingNodeConfiguration.ParameterValueList.GetACValue("KneadingTimeSlowHalf");
                    if (kTimeSlow != null)
                        kneedingSlow = kTimeSlow.ParamAsTimeSpan;

                    ACValue kTimeFast = kneedingNodeConfiguration.ParameterValueList.GetACValue("KneadingTimeFastHalf");
                    if (kTimeFast != null)
                        kneedingFast = kTimeFast.ParamAsTimeSpan;

                    kneedingTemperature = (kneedingSlow.TotalMinutes * temperatureRiseSlow) + (kneedingFast.TotalMinutes * temperatureRiseFast);
                }
            }
            return true;
        }

        private void CalculateComponents_Q_()
        {



        }



        #endregion

        #region User Interaction
        #endregion

    }
}
