using gip.core.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Workflowclass Dosing bakery'}de{'Workflowklasse Dosieren Backerei'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryDosing : PWDosing
    {
        public PWBakeryDosing(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool GetConfigForACMethod(ACMethod paramMethod, bool isForPAF, params object[] acParameter)
        {
            bool result = base.GetConfigForACMethod(paramMethod, isForPAF, acParameter);

            if (isForPAF)
            {
                PWBakeryTempCalc temperatureCalc = FindPredecessors<PWBakeryTempCalc>(true, c => c is PWBakeryTempCalc).FirstOrDefault();
                if (temperatureCalc != null && temperatureCalc.UseWaterMixer)
                {
                    ACValue temp = paramMethod.ParameterValueList.GetACValue("Temperature");
                    if (temp != null)
                    {
                        temp.Value = Math.Round(temperatureCalc.WaterCalcResult.ValueT,1);
                    }
                }
            }

            return result;
        }
    }
}
