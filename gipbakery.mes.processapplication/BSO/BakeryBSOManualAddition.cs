using gip.bso.manufacturing;
using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioManufacturing, "en{'Manual addition'}de{'Manuelle Zugabe'}", Global.ACKinds.TACBSO, Global.ACStorableTypes.NotStorable, true, true, SortIndex = 200)]
    public class BakeryBSOManualAddition : BSOManualAddition
    {
        public BakeryBSOManualAddition(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool AddToMessageList(MessageItem messageItem)
        {
            if (messageItem != null && messageItem.UserAckPWNode != null)
            {
                string acIdentifier = messageItem.UserAckPWNode.ValueT?.ComponentClass.ACIdentifier;

                if (acIdentifier.Contains(PWBakeryFlourDischargingAck.PWClassName) || acIdentifier.Contains(PWBakeryTempCalc.PWClassName))
                {
                    return true;
                }
            }

            return base.AddToMessageList(messageItem);
        }
    }
}
