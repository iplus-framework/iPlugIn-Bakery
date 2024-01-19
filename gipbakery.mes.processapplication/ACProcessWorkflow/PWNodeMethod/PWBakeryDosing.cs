using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Workflowclass Dosing bakery'}de{'Workflowklasse Dosieren Backerei'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryDosing : PWDosing
    {
        #region c'tors

        static PWBakeryDosing()
        {
            ACMethod.InheritFromBase(typeof(PWBakeryDosing), ACStateConst.SMStarting);
            RegisterExecuteHandler(typeof(PWBakeryDosing), HandleExecuteACMethod_PWDosing);
        }

        public PWBakeryDosing(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public new const string PWClassName = "PWBakeryDosing";

        #endregion

        #region Methods

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
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

        #endregion
    }
}
