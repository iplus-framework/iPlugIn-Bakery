﻿using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.facility;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Cartoning'}de{'Kartonieren'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryWorkCartoning : PWWorkTaskScanBase
    {
        new public const string PWClassName = nameof(PWBakeryWorkCartoning);

        #region Constructors

        static PWBakeryWorkCartoning()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("Capacity", typeof(int), 0, Global.ParamOption.Required));
            paramTranslation.Add("Capacity", "en{'Capacity'}de{'Kapazität'}");

            method.ParameterValueList.Add(new ACValue("PostingQuantitySuggestionMode", typeof(PostingQuantitySuggestionMode), gip.mes.facility.PostingQuantitySuggestionMode.OrderQuantity, Global.ParamOption.Optional));
            paramTranslation.Add("PostingQuantitySuggestionMode", "en{'Posting quantity suggestion mode'}de{'Buchungsmengen-Vorschlagsmodus'}");

            method.ParameterValueList.Add(new ACValue("ValidSeqNoPostingQSMode", typeof(string), null, Global.ParamOption.Optional));
            paramTranslation.Add("ValidSeqNoPostingQSMode", "en{'Valid sequence no. on posting quantity suggestion'}de{'Gültige laufende Nummer auf Buchungsmengenvorschlag'}");

            method.ParameterValueList.Add(new ACValue("OrderQuantityOnInwardPosting", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("OrderQuantityOnInwardPosting", "en{'Order quantity on inward posting'}de{'Order quantity on inward posting'}");

            var wrapper = new ACMethodWrapper(method, "en{'Configuration'}de{'Konfiguration'}", typeof(PWBakeryWorkCartoning), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryWorkCartoning), ACStateConst.SMStarting, wrapper);
            RegisterExecuteHandler(typeof(PWBakeryWorkCartoning), HandleExecuteACMethod_PWBakeryWorkCartoning);
        }

        public PWBakeryWorkCartoning(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            return base.ACDeInit(deleteACClassTask);
        }

        #endregion

        #region Properties
        #endregion

        #region Methods

        #region Execute-Helper-Handlers
        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            //result = null;
            //switch (acMethodName)
            //{
            //}
            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        public static bool HandleExecuteACMethod_PWBakeryWorkCartoning(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PWWorkTaskScanBase(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
        #endregion

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        #endregion

    }
}
