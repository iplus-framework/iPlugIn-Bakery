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
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Kneading'}de{'Kneten'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, PWBakeryKneading.PWClassName, true)]
    public class PAFBakeryKneading : PAFMixing
    {
        #region Constructors

        static PAFBakeryKneading()
        {
            ACMethod.RegisterVirtualMethod(typeof(PAFBakeryKneading), ACStateConst.TMStart, CreateVirtualMethod("Kneading", "en{'Kneading'}de{'Kneten'}", typeof(PWBakeryKneading)));
            RegisterExecuteHandler(typeof(PAFBakeryKneading), HandleExecuteACMethod_PAFBakeryKneading);
        }

        public PAFBakeryKneading(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        #endregion

        #region Properties
        #endregion

        #region override methods

        #region Execute-Helper-Handlers
        public static bool HandleExecuteACMethod_PAFBakeryKneading(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PAFMixing(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        #endregion


        #endregion
    }
}
