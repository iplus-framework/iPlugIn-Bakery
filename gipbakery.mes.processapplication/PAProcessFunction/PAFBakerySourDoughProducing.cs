using gip.core.autocomponent;
using gip.core.datamodel;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Sour dough'}de{'Sauerteig'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, true, "", BSOConfig = BakeryBSOSourDoughProducing.ClassName, SortIndex = 50)]
    public class PAFBakerySourDoughProducing : PAFBakeryYeastProducing
    {
        #region c'tors

        public PAFBakerySourDoughProducing(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {

        }

        public override bool ACPostInit()
        {
            bool result = base.ACPostInit();

            return result;
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            return base.ACDeInit(deleteACClassTask);
        }

        #endregion

        #region Methods

        public override void SMRunning()
        {
            UnSubscribeToProjectWorkCycle();
        }

        #endregion

        #region Handle Excute
        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            switch (acMethodName)
            {
                case "GetVirtualStoreACUrl":
                    result = GetVirtualStoreACUrl();
                    return true;
                case "SwitchVirtualStoreOutwardEnabled":
                    result = SwitchVirtualStoreOutwardEnabled();
                    return true;
                case "Clean":
                    result = Clean((short)acParameter[0]);
                    return true;
                case "GetPumpOverTargets":
                    result = GetPumpOverTargets();
                    return true;
                case "GetSourceVirtualStoreID":
                    result = GetSourceVirtualStoreID();
                    return true;
            }
            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        public static bool HandleExecuteACMethod_PAFBakerySourDoughProducing(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PAFBakeryYeastProducing(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
        #endregion
    }
}
