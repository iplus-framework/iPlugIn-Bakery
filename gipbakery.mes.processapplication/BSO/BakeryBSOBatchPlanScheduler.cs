using gip.bso.manufacturing;
using gip.core.datamodel;
using gip.mes.datamodel;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioManufacturing, "en{'Bakery Batch scheduler'}de{'Bäckerei Batch Zeitplaner'}", Global.ACKinds.TACBSO, Global.ACStorableTypes.NotStorable, true, true, Const.QueryPrefix + ProdOrderBatchPlan.ClassName)]
    public class BakeryBSOBatchPlanScheduler : BSOBatchPlanScheduler
    {

        #region ctor's

        public BakeryBSOBatchPlanScheduler(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {

        }

        #endregion

    }
}
