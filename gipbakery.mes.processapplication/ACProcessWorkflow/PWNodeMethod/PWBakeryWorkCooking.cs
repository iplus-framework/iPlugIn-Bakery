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

            method.ParameterValueList.Add(new ACValue("LabOrderTemplateName", typeof(string), PWBakeryWorkCooking.Const_LabOrderTemplateName, Global.ParamOption.Required));
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
            ACValue temperatureResult = acMethod?.ResultValueList.GetACValue("Temperature");
            if (temperatureResult != null)
            {
                double temperature = temperatureResult.ParamAsDouble;
                if (temperature > 0)
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

                        PWMethodProduction pwMethodProduction = ParentPWMethod<PWMethodProduction>();
                        if (pwMethodProduction == null)
                            return null;

                        bool posFound = PWDosing.GetRelatedProdOrderPosForWFNode(this, db, dbApp, pwMethodProduction, out intermediateChildPos, out intermediatePosition,
                                                                                 out endBatchPos, out matWFConnection, out batch, out batchPlan);
                        if (!posFound)
                            return null;

                        Material material = null;
                        if (endBatchPos != null)
                            material = endBatchPos.BookingMaterial;
                        if (material == null)
                            material = intermediatePosition.BookingMaterial;
                        if (material == null)
                        {
                            return new Msg(eMsgLevel.Error, "The material for laboratory order is not available!");
                        }

                        LabOrder template = GetOrCreateLabOrderTemplate(dbApp, material);
                        if (template == null)
                        {
                            return new Msg(eMsgLevel.Error, "The laboratory order template for cooking is not available!");
                        }

                        var labOrderManager = LabOrderManager;
                        if (labOrderManager == null)
                            return new Msg(eMsgLevel.Error, "The laboratory order manager is not available!");

                        LabOrder currentLabOrder = dbApp.LabOrder.FirstOrDefault(c => c.ProdOrderPartslistPosID == intermediateChildPos.ProdOrderPartslistPosID);
                        LabOrderPos loPos = null;

                        if (currentLabOrder == null)
                        {
                            Msg msg = labOrderManager.CreateNewLabOrder(dbApp, template, "", null, null, intermediateChildPos, null, out currentLabOrder);
                            if (msg != null)
                                return msg;
                        }

                        loPos = currentLabOrder.LabOrderPos_LabOrder.OrderByDescending(c => c.Sequence).FirstOrDefault(c => c.MDLabTag.MDLabTagName == Const_LabOrderTag);
                        if (loPos != null && !loPos.ActualValue.HasValue)
                        {
                            loPos.ActualValue = temperature;
                        }
                        else
                        {
                            Msg msg = labOrderManager.CopyLabOrderTemplatePos(dbApp, currentLabOrder, template);
                            if (msg != null)
                                return msg;

                            int nextSequence = loPos.Sequence + 1;

                            loPos = currentLabOrder.LabOrderPos_LabOrder.FirstOrDefault(c => c.MDLabTag.MDLabTagName == Const_LabOrderTag && !c.ActualValue.HasValue);
                            loPos.Sequence = nextSequence;
                            if (loPos != null)
                            {
                                loPos.ActualValue = temperature;
                            }
                        }

                        dbApp.ACSaveChanges();
                    }
                }
                else
                {
                    //Warning50065 : Please enter the cooking temperature!
                    return new Msg(this, eMsgLevel.Warning, null, null, 171, "Warning50065");
                }

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

                    PWMethodProduction pwMethodProduction = ParentPWMethod<PWMethodProduction>();
                    if (pwMethodProduction == null)
                        return null;

                    bool posFound = PWDosing.GetRelatedProdOrderPosForWFNode(this, db, dbApp, pwMethodProduction, out intermediateChildPos, out intermediatePosition,
                                                                             out endBatchPos, out matWFConnection, out batch, out batchPlan);
                    if (!posFound)
                        return null;

                    Material material = null;
                    if (endBatchPos != null)
                        material = endBatchPos.BookingMaterial;
                    if (material == null)
                        material = intermediatePosition.BookingMaterial;
                    if (material == null)
                    {
                        // TODO Error:
                        return null;
                    }

                    MDLabOrderState mdLabOrderState = dbApp.MDLabOrderState.FirstOrDefault(c => c.MDLabOrderStateIndex == (short)MDLabOrderState.LabOrderStates.Finished);
                    if (mdLabOrderState != null)
                    {
                        LabOrder currentLabOrder = dbApp.LabOrder.FirstOrDefault(c => c.ProdOrderPartslistPosID == intermediateChildPos.ProdOrderPartslistPosID);
                        if (currentLabOrder != null)
                            currentLabOrder.MDLabOrderState = mdLabOrderState;

                        dbApp.ACSaveChanges();
                    }
                }
            }

            return base.OnGetMessageOnReleasingProcessModule(invoker, pause);
        }


        private LabOrder GetOrCreateLabOrderTemplate(DatabaseApp dbApp, Material material)
        {
            LabOrder template = dbApp.LabOrder.FirstOrDefault(c => c.LabOrderTypeIndex == (short)GlobalApp.LabOrderType.Template
                                                                && c.TemplateName == Const_LabOrderTemplateName
                                                                && c.MaterialID == material.MaterialID);
            if (template == null)
            {
                string secondaryKey = Root.NoManager.GetNewNo(Database, typeof(LabOrder), LabOrder.NoColumnName, LabOrder.FormatNewNo, this);
                template = LabOrder.NewACObject(dbApp, null, secondaryKey);
                template.LabOrderTypeIndex = (short)GlobalApp.LabOrderType.Template;
                template.MDLabOrderState = dbApp.MDLabOrderState.FirstOrDefault(c => c.IsDefault);
                template.TemplateName = Const_LabOrderTemplateName;
                template.Material = material;
                dbApp.LabOrder.AddObject(template);

                MDLabTag temperatureTag = dbApp.MDLabTag.FirstOrDefault(c => c.MDKey == Const_LabOrderTag);
                if (temperatureTag == null)
                {
                    temperatureTag = MDLabTag.NewACObject(dbApp, null);
                    temperatureTag.MDKey = Const_LabOrderTag;
                    temperatureTag.MDLabTagIndex = (short)MDLabTag.LabTags.Maesure;
                    temperatureTag.MDNameTrans = "en{'Temperature'}de{'Temperatur'}";
                    dbApp.MDLabTag.AddObject(temperatureTag);
                }

                LabOrderPos pos = LabOrderPos.NewACObject(dbApp, template);
                pos.MDLabTag = temperatureTag;

                dbApp.LabOrderPos.AddObject(pos);


                Msg msg = dbApp.ACSaveChanges();
                if (msg != null)
                    OnNewAlarmOccurred(ProcessAlarm, msg);
            }
            return template;
        }

    }
}
