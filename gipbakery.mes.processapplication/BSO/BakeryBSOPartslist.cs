using gip.bso.masterdata;
using gip.core.datamodel;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioMaterial, "en{'Bill of Materials'}de{'Stückliste'}", Global.ACKinds.TACBSO, Global.ACStorableTypes.NotStorable, true, true, Const.QueryPrefix + gip.mes.datamodel.Partslist.ClassName)]
    public class BakeryBSOPartslist : BSOPartslist
    {
        public BakeryBSOPartslist(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }
    }
}
