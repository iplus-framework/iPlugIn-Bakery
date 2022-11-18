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
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Kneading-Start'}de{'Kneter-Start'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryWorkKneading : PWBakeryWorkTask
    {
        new public const string PWClassName = nameof(PWBakeryWorkKneading);

        #region Constructors

        static PWBakeryWorkKneading()
        {
            Type thisType = typeof(PWBakeryWorkKneading);
            ACMethodWrapper wrapper = PWBakeryWorkTask.CreateACMethodWrapper(thisType);
            ACMethod.RegisterVirtualMethod(thisType, ACStateConst.SMStarting, wrapper);
            RegisterExecuteHandler(thisType, HandleExecuteACMethod_PWBakeryWorkKneading);
        }

        public PWBakeryWorkKneading(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            return base.ACDeInit(deleteACClassTask);
        }

        #endregion

        #region Properties
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

        public static bool HandleExecuteACMethod_PWBakeryWorkKneading(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PWBakeryWorkTask(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
        #endregion

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        #endregion

    }
}
