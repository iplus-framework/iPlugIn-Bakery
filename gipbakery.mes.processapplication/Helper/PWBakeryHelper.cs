using gip.core.datamodel;
using gip.mes.facility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    public static class PWBakeryHelper
    {
        public static void AddDefaultWorkParameters
            (ACMethod method, Dictionary<string, string> paramTranslation)
        {
            method.ParameterValueList.Add(new ACValue("QuantityPerRack", typeof(double), 0, Global.ParamOption.Required));
            paramTranslation.Add("QuantityPerRack", "en{'Capacity: Quantity per oven rack'}de{'Kapazität: Menge pro Stikkenwagen'}");

            method.ParameterValueList.Add(new ACValue("PostingQuantitySuggestionMode", typeof(PostingQuantitySuggestionMode), gip.mes.facility.PostingQuantitySuggestionMode.OrderQuantity, Global.ParamOption.Optional));
            paramTranslation.Add("PostingQuantitySuggestionMode", "en{'Posting quantity suggestion mode'}de{'Buchungsmengen-Vorschlagsmodus'}");

            method.ParameterValueList.Add(new ACValue("ValidSeqNoPostingQSMode", typeof(string), null, Global.ParamOption.Optional));
            paramTranslation.Add("ValidSeqNoPostingQSMode", "en{'Valid sequence no. on posting quantity suggestion'}de{'Gültige laufende Nummer auf Buchungsmengenvorschlag'}");

            method.ParameterValueList.Add(new ACValue("PostingQuantitySuggestionMode2", typeof(PostingQuantitySuggestionMode), gip.mes.facility.PostingQuantitySuggestionMode.OrderQuantity, Global.ParamOption.Optional));
            paramTranslation.Add("PostingQuantitySuggestionMode2", "en{'Posting quantity suggestion mode 2'}de{'Buchungsmengen-Vorschlagsmodus 2'}");

            method.ParameterValueList.Add(new ACValue("ValidSeqNoPostingQSMode2", typeof(string), null, Global.ParamOption.Optional));
            paramTranslation.Add("ValidSeqNoPostingQSMode2", "en{'Valid sequence no. on posting quantity suggestion 2'}de{'Gültige laufende Nummer auf Buchungsmengenvorschlag 2'}");

            method.ParameterValueList.Add(new ACValue("InwardAutoSplitQuant", typeof(int), 0, Global.ParamOption.Optional));
            paramTranslation.Add("InwardAutoSplitQuant", "en{'Auto split quant on inward posting increment num.'}de{'Auto split quant on inward posting increment num.'}");
        }
    }
}
