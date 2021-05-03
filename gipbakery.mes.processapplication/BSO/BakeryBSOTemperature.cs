using gip.bso.manufacturing;
using gip.core.autocomponent;
using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioManufacturing, "en{'Temperatures'}de{'Temperaturen'}", Global.ACKinds.TACBSO, Global.ACStorableTypes.NotStorable, true, true, SortIndex = 700)]
    public class BakeryBSOTemperature : BSOWorkCenterChild
    {
        public BakeryBSOTemperature(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public const string ClassName = "BakeryBSOTemperature";

        public override void Activate(ACComponent selectedProcessModule)
        {
            BakeryBSOWorkCenterSelector workCenter = ParentBSOWCS as BakeryBSOWorkCenterSelector;
            if (workCenter != null)
            {
                if (string.IsNullOrEmpty(workCenter.BakeryTemperatureServiceACUrl))
                {
                    //TODO: message
                    return;
                }

                //workCenter.BakeryTemperatureServiceACUrl = "\\Service\\BakeryTempService";

                ACValueList result = Root.ACUrlCommand(workCenter.BakeryTemperatureServiceACUrl + "!GetTemperaturesInfo", selectedProcessModule.ComponentClass.ACClassID) as ACValueList;
                if (result != null && result.Any())
                {

                }
            }
        }

        public override void DeActivate()
        {

        }
    }
}
