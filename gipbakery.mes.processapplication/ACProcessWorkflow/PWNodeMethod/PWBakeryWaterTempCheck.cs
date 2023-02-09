using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Water temperature check'}de{'Wassertemperaturprüfung'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryWaterTempCheck : PWNodeProcessMethod
    {
        #region c'tors

        static PWBakeryWaterTempCheck()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("SkipCheck", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("SkipCheck", "en{'Skip check'}de{'Ignoriere Prüfung'}");

            var wrapper = new ACMethodWrapper(method, "en{'Configuration'}de{'Konfiguration'}", typeof(PWBakeryWaterTempCheck), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryWaterTempCheck), ACStateConst.SMStarting, wrapper);
            RegisterExecuteHandler(typeof(PWBakeryWaterTempCheck), HandleExecuteACMethod_PWBakeryWaterTempCheck);
        }

        public PWBakeryWaterTempCheck(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public const string PWClassName = "PWBakeryWaterTempCheck";

        #endregion

        #region Properties
        protected bool SkipCheck
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("SkipCheck");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }

        double _TargetWaterTemp = 0.0;
        #endregion

        #region Methods
        public override void Recycle(IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
        {
            base.Recycle(content, parentACObject, parameter, acIdentifier);
            _TargetWaterTemp = 0.0;
        }

        public override void SMIdle()
        {
            base.SMIdle();
            _TargetWaterTemp = 0.0;
        }

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            if (Root == null || !Root.Initialized)
            {
                SubscribeToProjectWorkCycle();
                return;
            }

            bool skipCheck = SkipCheck;
            if (!skipCheck)
            {
                skipCheck = true;
                PWProcessFunction rootPW = RootPW;
                if (rootPW != null)
                {
                    PWBakeryTempCalc pwTempCalc = rootPW.FindChildComponents<PWBakeryTempCalc>(c => c is PWBakeryTempCalc).FirstOrDefault();
                    if (pwTempCalc != null)
                    {
                        double? targetTemp = pwTempCalc.CalculateWaterTargetTemperature();
                        if (targetTemp.HasValue)
                        {
                            _TargetWaterTemp = targetTemp.Value;
                            skipCheck = false;
                        }
                    }
                }
            }
            if (skipCheck)
                CurrentACState = ACStateEnum.SMCompleted;
            else
                base.SMStarting();
        }

        public override bool GetConfigForACMethod(ACMethod paramMethod, bool isForPAF, params object[] acParameter)
        {
            bool result = base.GetConfigForACMethod(paramMethod, isForPAF, acParameter);
            if (!result)
                return result;

            if (isForPAF)
            {
                ACValue waterTempParam = paramMethod.ParameterValueList.GetACValue("WaterTemp");
                if (waterTempParam != null)
                    waterTempParam.Value = _TargetWaterTemp;
            }
            return result;
        }

        private static bool HandleExecuteACMethod_PWBakeryWaterTempCheck(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, object[] acParameter)
        {
            return HandleExecuteACMethod_PWNodeProcessMethod(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

        #endregion
    }
}
