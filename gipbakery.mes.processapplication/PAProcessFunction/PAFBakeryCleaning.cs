using gip.core.autocomponent;
using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Cleaning'}de{'Reinigen'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, PWBakeryCleaning.PWClassName, true)]
    public class PAFBakeryCleaning : PAProcessFunction
    {
        static PAFBakeryCleaning()
        {
            ACMethod.RegisterVirtualMethod(typeof(PAFBakeryCleaning), ACStateConst.TMStart, CreateVirtualMethod("BakeryCleaning", "en{'Cleaning'}de{'Reinigen'}", typeof(PWBakeryCleaning)));
            RegisterExecuteHandler(typeof(PAFBakeryCleaning), HandleExecuteACMethod_PAFBakeryCleaning);
        }



        public PAFBakeryCleaning(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public const string ClassName = "PAFBakeryCleaning";

        [ACMethodAsync("Process", "en{'Start'}de{'Start'}", (short)MISort.Start, false)]
        public override ACMethodEventArgs Start(ACMethod acMethod)
        {
            return base.Start(acMethod);
        }

        protected static ACMethodWrapper CreateVirtualMethod(string acIdentifier, string captionTranslation, Type pwClass)
        {
            ACMethod method = new ACMethod(acIdentifier);

            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();
            method.ParameterValueList.Add(new ACValue("CleaningTarget", typeof(int), 0, Global.ParamOption.Required));
            paramTranslation.Add("CleaningTarget", "en{'Cleaning target'}de{'Reinigungsziel'}");
            method.ParameterValueList.Add(new ACValue("TargetQuantity", typeof(double), 0.0, Global.ParamOption.Required));
            paramTranslation.Add("TargetQuantity", "en{'Target quantity'}de{'Sollmenge'}");


            return new ACMethodWrapper(method, captionTranslation, pwClass, paramTranslation, null);
        }

        private static bool HandleExecuteACMethod_PAFBakeryCleaning(out object result, IACComponent acComponent, string acMethodName, ACClassMethod acClassMethod, object[] acParameter)
        {
            return HandleExecuteACMethod_PAProcessFunction(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
    }
}
