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
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Bakery intermediate water tank'}de{'Bakery intermediate water tank'}", Global.ACKinds.TPAProcessModule, Global.ACStorableTypes.Required, false, PWGroupVB.PWClassName, true)]
    public class BakeryIntermWaterTank : PAMIntermediatebin
    {
        public const string SelRuleID_IntermWaterTank = "IntermWaterTank";


        static BakeryIntermWaterTank()
        {
            ACRoutingService.RegisterSelectionQuery(SelRuleID_IntermWaterTank, (c, p) => c.ComponentInstance is BakeryIntermWaterTank, (c, p) => c.ComponentInstance is PAProcessModule);
        }

        public BakeryIntermWaterTank(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }
    }
}
