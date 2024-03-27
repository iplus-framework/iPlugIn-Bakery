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
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Cooking'}de{'Kochen'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryWorkCooking : PWBakeryWorkTask
    {
        static PWBakeryWorkCooking()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("PiecesPerRack", typeof(int), 0, Global.ParamOption.Required));
            paramTranslation.Add("PiecesPerRack", "en{'Capacity: Pieces per oven rack'}de{'Kapazität: Stücke pro Stikkenwagen'}");

            method.ParameterValueList.Add(new ACValue("Temperature", typeof(double), 0.0, Global.ParamOption.Required));
            paramTranslation.Add("Temperature", "en{'Cooking temperature'}de{'Kochtemperatur'}");

            method.ParameterValueList.Add(new ACValue("LabOrderTemplateName", typeof(string), Const_LabOrderTemplateName, Global.ParamOption.Required));
            paramTranslation.Add("LabOrderTemplateName", "en{'Laborder template name'}de{'Name der Laborauftragsvorlage'}");

            Dictionary<string, string> resultTranslation = new Dictionary<string, string>();
            method.ResultValueList.Add(new ACValue("Temperature", typeof(Double), (Double)(-1.0), Global.ParamOption.Optional));
            resultTranslation.Add("Temperature", "en{'Cooking temperature'}de{'Kochtemperatur'}");

            var wrapper = new ACMethodWrapper(method, "en{'Configuration'}de{'Konfiguration'}", typeof(PWBakeryWorkCooking), paramTranslation, resultTranslation);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryWorkCooking), ACStateConst.SMStarting, wrapper);
            RegisterExecuteHandler(typeof(PWBakeryWorkCooking), HandleExecuteACMethod_PWBakeryWorkCooking);
        }

        public PWBakeryWorkCooking(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public const string Const_LabOrderTemplateName = "LOCookingTemplate";
        public const string Const_LabOrderTag = "Temperature";


        private static bool HandleExecuteACMethod_PWBakeryWorkCooking(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, object[] acParameter)
        {
            return HandleExecuteACMethod_PWBakeryWorkTask(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

        public ACLabOrderManager LabOrderManager
        {
            get
            {
                PWMethodProduction pwMethodProduction = ParentPWMethod<PWMethodProduction>();
                return pwMethodProduction != null ? pwMethodProduction.LabOrderManager : null;
            }
        }

        public string LabOrderTemplateName
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    ACValue acValue = method.ParameterValueList.GetACValue("LabOrderTemplateName");
                    if (acValue != null)
                        return !String.IsNullOrEmpty(acValue.ParamAsString) ? acValue.ParamAsString : Const_LabOrderTemplateName;
                }
                return Const_LabOrderTemplateName;
            }
        }

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        protected override Msg OnValidateChangedACMethod(ACMethod acMethod)
        {
            using (Database db = new gip.core.datamodel.Database())
            using (DatabaseApp dbApp = new DatabaseApp(db))
            {
                ProdOrderPartslistPos intermediateChildPos;
                ProdOrderPartslistPos intermediatePosition;
                MaterialWFConnection matWFConnection;
                ProdOrderBatch batch;
                ProdOrderBatchPlan batchPlan;
                ProdOrderPartslistPos endBatchPos;
                MaterialWFConnection[] matWFConnections;

                PWMethodProduction pwMethodProduction = ParentPWMethod<PWMethodProduction>();
                if (pwMethodProduction == null)
                    return null;

                bool posFound = PWDosing.GetRelatedProdOrderPosForWFNode(this, db, dbApp, pwMethodProduction, out intermediateChildPos, out intermediatePosition,
                                                                            out endBatchPos, out matWFConnection, out batch, out batchPlan, out matWFConnections);
                if (!posFound)
                    return null;

                var labOrderManager = LabOrderManager;
                if (labOrderManager == null)
                    return new Msg(eMsgLevel.Error, "The laboratory order manager is not available!");

                Msg msg = labOrderManager.CreateOrUpdateLabOrderForPWNode(dbApp, LabOrderTemplateName, acMethod, intermediateChildPos, intermediatePosition, endBatchPos, new string[] { Const_LabOrderTag }, null);
                if (msg != null)
                    return msg;
             
                dbApp.ACSaveChanges();
            }

            return base.OnValidateChangedACMethod(acMethod);
        }

        public override Msg OnGetMessageOnReleasingProcessModule(PAFWorkTaskScanBase invoker, bool pause)
        {
            if (!pause)
            {
                using (Database db = new gip.core.datamodel.Database())
                using (DatabaseApp dbApp = new DatabaseApp(db))
                {
                    ProdOrderPartslistPos intermediateChildPos;
                    ProdOrderPartslistPos intermediatePosition;
                    MaterialWFConnection matWFConnection;
                    ProdOrderBatch batch;
                    ProdOrderBatchPlan batchPlan;
                    ProdOrderPartslistPos endBatchPos;
                    MaterialWFConnection[] matWFConnections;

                    PWMethodProduction pwMethodProduction = ParentPWMethod<PWMethodProduction>();
                    if (pwMethodProduction == null)
                        return null;

                    bool posFound = PWDosing.GetRelatedProdOrderPosForWFNode(this, db, dbApp, pwMethodProduction, out intermediateChildPos, out intermediatePosition,
                                                                             out endBatchPos, out matWFConnection, out batch, out batchPlan, out matWFConnections);
                    if (!posFound)
                        return null;

                    var labOrderManager = LabOrderManager;
                    if (labOrderManager == null)
                        return new Msg(eMsgLevel.Error, "The laboratory order manager is not available!");

                    Msg msg = labOrderManager.CompleteLabOrderForPWNode(dbApp, intermediateChildPos, intermediatePosition, endBatchPos);
                    if (msg != null)
                        return msg;

                    dbApp.ACSaveChanges();
                }
            }

            return base.OnGetMessageOnReleasingProcessModule(invoker, pause);
        }

    }
}
