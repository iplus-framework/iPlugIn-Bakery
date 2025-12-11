using gip.bso.masterdata;
using gip.core.datamodel;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioMaterial, "en{'Material'}de{'Material'}", Global.ACKinds.TACBSO, Global.ACStorableTypes.NotStorable, true, true, Const.QueryPrefix + gip.mes.datamodel.Material.ClassName)]
    public class BakeryBSOMaterial : BSOMaterial
    {
        #region c'tors

        public BakeryBSOMaterial(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
                    base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        #endregion
    }
}
