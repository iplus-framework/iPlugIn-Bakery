using gip.core.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Manual addition bakery'}de{'Manuelle Zugabe Backerei'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, true, PWInfoACClass = PWManualWeighing.PWClassName, BSOConfig = "BakeryBSOManualAddition", SortIndex = 200)]
    public class PAFBakeryManualAddition : PAFManualAddition
    {
        public PAFBakeryManualAddition(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }
    }
}
