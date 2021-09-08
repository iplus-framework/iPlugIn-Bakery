using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.datamodel;
using gip.mes.facility;
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
        static PWBakeryDosing()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("SkipComponents", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("SkipComponents", "en{'Skip not dosable components'}de{'Überspringe nicht dosierbare Komponenten'}");
            method.ParameterValueList.Add(new ACValue("ComponentsSeqFrom", typeof(Int32), 0, Global.ParamOption.Optional));
            paramTranslation.Add("ComponentsSeqFrom", "en{'Components from Seq.-No.'}de{'Komponenten VON Seq.-Nr.'}");
            method.ParameterValueList.Add(new ACValue("ComponentsSeqTo", typeof(Int32), 0, Global.ParamOption.Optional));
            paramTranslation.Add("ComponentsSeqTo", "en{'Components to Seq.-No.'}de{'Komponenten BIS Seq.-Nr.'}");
            method.ParameterValueList.Add(new ACValue("ScaleOtherComp", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("ScaleOtherComp", "en{'Scale other components after Dosing'}de{'Restliche Komponenten anpassen'}");
            method.ParameterValueList.Add(new ACValue("ManuallyChangeSource", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("ManuallyChangeSource", "en{'Manually change source'}de{'Manueller Quellenwechsel'}");
            method.ParameterValueList.Add(new ACValue("MinDosQuantity", typeof(double), 0.0, Global.ParamOption.Optional));
            paramTranslation.Add("MinDosQuantity", "en{'Minimum dosing quantity'}de{'Minimale Dosiermenge'}");
            method.ParameterValueList.Add(new ACValue("OldestSilo", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("OldestSilo", "en{'Dosing from oldest Silo only'}de{'Nur aus ältestem Silo dosieren'}");
            method.ParameterValueList.Add(new ACValue("AutoChangeScale", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("AutoChangeScale", "en{'Automatically change scale'}de{'Automatischer Waagenwechsel'}");
            method.ParameterValueList.Add(new ACValue("CheckScaleEmpty", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("CheckScaleEmpty", "en{'Check if scale empty'}de{'Prüfung Waage leer'}");
            method.ParameterValueList.Add(new ACValue("BookTargetQIfZero", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("BookTargetQIfZero", "en{'Post target quantity when actual = 0'}de{'Sollmenge buchen wenn Istgewicht = 0'}");
            method.ParameterValueList.Add(new ACValue("DoseFromFillingSilo", typeof(bool?), null, Global.ParamOption.Optional));
            paramTranslation.Add("DoseFromFillingSilo", "en{'Dose from silo that is filling'}de{'Dosiere aus Silo das befüllt wird'}");

            var wrapper = new ACMethodWrapper(method, "en{'Configuration'}de{'Konfiguration'}", typeof(PWBakeryDosing), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryDosing), ACStateConst.SMStarting, wrapper);
            RegisterExecuteHandler(typeof(PWBakeryDosing), HandleExecuteACMethod_PWDosing);
        }

        public PWBakeryDosing(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public new const string PWClassName = "PWBakeryDosing";

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
