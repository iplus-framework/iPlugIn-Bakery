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
    }
}
