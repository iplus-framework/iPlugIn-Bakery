using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Cutting'}de{'Schneiden'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, PWBakeryWorkForming.PWClassName, true)]
    public class PAFBakeryWorkCutting : PAFWorkTaskScanBase
    {
        static PAFBakeryWorkCutting()
        {
            ACMethod.RegisterVirtualMethod(typeof(PAFBakeryWorkCutting), ACStateConst.TMStart, CreateVirtualMethod("Cutting", "en{'Cutting'}de{'Schneiden'}", typeof(PWBakeryWorkCutting)));
            RegisterExecuteHandler(typeof(PAFBakeryWorkCutting), HandleExecuteACMethod_PAFBakeryWorkCutting);
        }

        public PAFBakeryWorkCutting(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        protected override bool PWWorkTaskScanDeSelector(IACComponent c)
        {
            return c is PWBakeryWorkCooking;
        }

        protected override bool PWWorkTaskScanSelector(IACComponent c)
        {
            return c is PWBakeryWorkCooking;
        }

        [ACMethodAsync("Process", "en{'Start'}de{'Start'}", (short)MISort.Start, false)]
        public override ACMethodEventArgs Start(ACMethod acMethod)
        {
            return base.Start(acMethod);
        }

        private static bool HandleExecuteACMethod_PAFBakeryWorkCutting(out object result, IACComponent acComponent, string acMethodName, ACClassMethod acClassMethod, object[] acParameter)
        {
            return HandleExecuteACMethod_PAFWorkTaskScanBase(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

        protected static ACMethodWrapper CreateVirtualMethod(string acIdentifier, string captionTranslation, Type pwClass)
        {
            ACMethod method = new ACMethod(acIdentifier);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();
            Dictionary<string, string> resultTranslation = new Dictionary<string, string>();

            return new ACMethodWrapper(method, captionTranslation, pwClass, paramTranslation, resultTranslation);
        }
    }
}
