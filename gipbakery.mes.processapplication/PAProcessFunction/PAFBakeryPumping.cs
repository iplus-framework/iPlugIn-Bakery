using gip.core.autocomponent;
using gip.core.datamodel;
using System;
using System.Collections.Generic;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Pump over'}de{'Umpumpen'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, PWBakeryPumping.PWClassName, true)]
    public class PAFBakeryPumping : PAProcessFunction
    {
        static PAFBakeryPumping()
        {
            ACMethod.RegisterVirtualMethod(typeof(PAFBakeryPumping), ACStateConst.TMStart, CreateVirtualMethod("BakeryPumping", "en{'Pump over'}de{'Umpumpen'}", typeof(PWBakeryPumping)));
            RegisterExecuteHandler(typeof(PAFBakeryPumping), HandleExecuteACMethod_PAFBakeryPumping);
        }

        public PAFBakeryPumping(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public const string ClassName = "PAFBakeryPumping";

        [ACMethodAsync("Process", "en{'Start'}de{'Start'}", (short)MISort.Start, false)]
        public override ACMethodEventArgs Start(ACMethod acMethod)
        {
            return base.Start(acMethod);
        }

        private static bool HandleExecuteACMethod_PAFBakeryPumping(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, object[] acParameter)
        {
            return HandleExecuteACMethod_PAProcessFunction(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

        protected override MsgWithDetails CompleteACMethodOnSMStarting(ACMethod acMethod)
        {
            return base.CompleteACMethodOnSMStarting(acMethod);
        }

        protected static ACMethodWrapper CreateVirtualMethod(string acIdentifier, string captionTranslation, Type pwClass)
        {
            ACMethod method = new ACMethod(acIdentifier);

            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();
            method.ParameterValueList.Add(new ACValue("Source", typeof(Int16), (Int16)0, Global.ParamOption.Optional));
            paramTranslation.Add("Source", "en{'Source'}de{'Quelle'}");
            method.ParameterValueList.Add(new ACValue("Route", typeof(Route), null, Global.ParamOption.Required));
            paramTranslation.Add("Route", "en{'Route'}de{'Route'}");
            method.ParameterValueList.Add(new ACValue("Destination", typeof(Int16), (Int16)0, Global.ParamOption.Required));
            paramTranslation.Add("Destination", "en{'Destination'}de{'Ziel'}");
            method.ParameterValueList.Add(new ACValue("TargetQuantity", typeof(Double), (Double)0.0, Global.ParamOption.Optional));
            paramTranslation.Add("TargetQuantity", "en{'Target quantity'}de{'Sollmenge'}");

            Dictionary<string, string> resultTranslation = new Dictionary<string, string>();
            method.ResultValueList.Add(new ACValue("ActualQuantity", typeof(Double), (Double)0.0, Global.ParamOption.Optional));
            resultTranslation.Add("ActualQuantity", "en{'Actual quantity'}de{'Istmenge'}");

            return new ACMethodWrapper(method, captionTranslation, pwClass, paramTranslation, resultTranslation);
        }
    }
}
