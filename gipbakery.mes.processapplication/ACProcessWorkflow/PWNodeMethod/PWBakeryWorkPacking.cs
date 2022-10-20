using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.facility;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Packing'}de{'Verpacken'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryWorkPacking : PWWorkTaskScanBase
    {
        new public const string PWClassName = "PWBakeryWorkPacking";

        #region Constructors

        static PWBakeryWorkPacking()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            PWBakeryHelper.AddDefaultWorkParameters(method, paramTranslation);

            var wrapper = new ACMethodWrapper(method, "en{'Configuration'}de{'Konfiguration'}", typeof(PWBakeryWorkPacking), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryWorkPacking), ACStateConst.SMStarting, wrapper);
            RegisterExecuteHandler(typeof(PWBakeryWorkPacking), HandleExecuteACMethod_PWBakeryWorkPacking);
        }

        public PWBakeryWorkPacking(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
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

        public static bool HandleExecuteACMethod_PWBakeryWorkPacking(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PWWorkTaskScanBase(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
        #endregion

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        public override Msg OnGetMessageAfterOccupyingProcessModule(PAFWorkTaskScanBase invoker)
        {
            Msg msg = base.OnGetMessageAfterOccupyingProcessModule(invoker);
            if (msg == null)
            {
                PAFBakeryWorkPacking pafPacking = invoker as PAFBakeryWorkPacking;
                if (pafPacking != null)
                {
                    pafPacking.ResetCounter();
                }
            }
            return msg;
        }

        public override Msg OnGetMessageOnReleasingProcessModule(PAFWorkTaskScanBase invoker, bool pause)
        {
            Msg msg = base.OnGetMessageOnReleasingProcessModule(invoker, pause);
            if (msg == null)
            {
                PAFBakeryWorkPacking pafPacking = invoker as PAFBakeryWorkPacking;
                if (pafPacking != null)
                {
                    //pafPacking.PieceCounter;
                }
            }
            return msg;
        }

        #endregion

    }
}
