using gip.core.autocomponent;
using gip.core.datamodel;
using System;
using System.Collections.Generic;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Water temperature check'}de{'Wassertemperaturprüfung'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, PWBakeryWaterTempCheck.PWClassName, true)]
    public class PAFBakeryWaterTempCheck : PAProcessFunction
    {
        #region c'tors

        static PAFBakeryWaterTempCheck()
        {
            ACMethod.RegisterVirtualMethod(typeof(PAFBakeryWaterTempCheck), ACStateConst.TMStart, CreateVirtualMethod("WaterTempCheck", "en{'Water temperature check'}de{'Wassertemperaturprüfung'}", typeof(PWBakeryWaterTempCheck)));
            RegisterExecuteHandler(typeof(PAFBakeryWaterTempCheck), HandleExecuteACMethod_PAFBakeryWaterTempCheck);
        }

        public PAFBakeryWaterTempCheck(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public const string ClassName = "PAFBakeryWaterTempCheck";

        #endregion

        #region Methods

        [ACMethodAsync("Process", "en{'Start'}de{'Start'}", (short)MISort.Start, false)]
        public override ACMethodEventArgs Start(ACMethod acMethod)
        {
            return base.Start(acMethod);
        }

        protected static ACMethodWrapper CreateVirtualMethod(string acIdentifier, string captionTranslation, Type pwClass)
        {
            ACMethod method = new ACMethod(acIdentifier);

            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();
            method.ParameterValueList.Add(new ACValue("WaterTemp", typeof(double), 0.0, Global.ParamOption.Required));
            paramTranslation.Add("WaterTemp", "en{'Water temperature'}de{'Wassertemperatur'}");

            return new ACMethodWrapper(method, captionTranslation, pwClass, paramTranslation, null);
        }

        private static bool HandleExecuteACMethod_PAFBakeryWaterTempCheck(out object result, IACComponent acComponent, string acMethodName, ACClassMethod acClassMethod, object[] acParameter)
        {
            return HandleExecuteACMethod_PAProcessFunction(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

        #endregion
    }
}
