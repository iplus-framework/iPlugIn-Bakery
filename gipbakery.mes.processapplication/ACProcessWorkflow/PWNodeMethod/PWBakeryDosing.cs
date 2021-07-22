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
            

            var wrapper = new ACMethodWrapper(method, "en{'Configuration'}de{'Konfiguration'}", typeof(PWDosing), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWDosing), ACStateConst.SMStarting, wrapper);
            RegisterExecuteHandler(typeof(PWDosing), HandleExecuteACMethod_PWDosing);
        }

        public PWBakeryDosing(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public new const string PWClassName = "PWBakeryDosing";



        #region Properties

        

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

        public TimeSpan CalculateDuration(bool doseSimultaneously, double dosUnit, int dosPause, PAProcessModule processModule, out PAFBakeryDosingWater water)
        {
            PAProcessFunction responsibleFunc;
            water = null;

            double? targetQuantity = GetTargetQuantity(processModule, out responsibleFunc);

            if (targetQuantity.HasValue && responsibleFunc != null)
            {
                targetQuantity = Math.Abs(targetQuantity.Value);

                PAFBakeryDosingFlour flour = responsibleFunc as PAFBakeryDosingFlour;

                double? dosingTimeSecPerKg = null;
                if (flour != null)
                    dosingTimeSecPerKg = flour.GetFlourDosingTime();
                else 
                {
                    water = responsibleFunc as PAFBakeryDosingWater;
                    if (water != null)
                    {
                        dosingTimeSecPerKg = water.GetWaterDosingTime();
                    }
                }

                if (!dosingTimeSecPerKg.HasValue)
                {
                    return TimeSpan.Zero;
                }

                if (doseSimultaneously)
                {
                    return TimeSpan.FromSeconds(targetQuantity.Value * dosingTimeSecPerKg.Value);
                }
                else
                {
                    if (flour != null)
                    {
                        int nDosImpulse = (int)(targetQuantity / dosUnit);
                        nDosImpulse--;
                        double nDosZeit = 0;
                        if ((nDosImpulse > 0) && (dosPause > 0))
                            nDosZeit = nDosImpulse * dosPause;

                        return TimeSpan.FromSeconds(nDosZeit + (targetQuantity.Value * dosingTimeSecPerKg.Value));
                    }
                    else if (water != null)
                    {
                        return TimeSpan.FromSeconds((targetQuantity.Value * dosingTimeSecPerKg.Value) + water.DosTimeWaterControl.ValueT);
                    }
                }
            }

            return TimeSpan.Zero;
        }

        private double? GetTargetQuantity(PAProcessModule processModule, out PAProcessFunction responsibleFunc)
        {
            responsibleFunc = null;
            PWMethodProduction pwMethodProduction = ParentPWMethod<PWMethodProduction>();
            // If dosing is not for production, then do nothing
            if (pwMethodProduction == null)
            {
                return null;
            }

            Msg msg = null;
            if (ProdOrderManager == null)
            {
                // Error50167: ProdOrderManager is null.
                msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartNextProdComponent(1)", 1000, "Error50167");

                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    Messages.LogError(this.GetACUrl(), msg.ACIdentifier, msg.InnerMessage);
                OnNewAlarmOccurred(ProcessAlarm, msg, true);
                return null;
            }

            // Reduziere zyklische Datenbankabfragen über Zeitstempel
            var currentParallelPWDosings = CurrentParallelPWDosings;
            if (currentParallelPWDosings != null
                && currentParallelPWDosings.Where(c => c.CurrentACState != ACStateEnum.SMIdle).Any()
                && NextCheckIfPWDosingsFinished.HasValue && DateTime.Now < NextCheckIfPWDosingsFinished)
            {
                return null;
            }


            using (var dbIPlus = new Database())
            {
                using (var dbApp = new DatabaseApp(dbIPlus))
                {
                    ProdOrderPartslistPos endBatchPos = pwMethodProduction.CurrentProdOrderPartslistPos.FromAppContext<ProdOrderPartslistPos>(dbApp);
                    if (pwMethodProduction.CurrentProdOrderBatch == null)
                    {
                        // Error50060: No batch assigned to last intermediate material of this workflow-process
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartNextProdComponent(2)", 1010, "Error50060");

                        if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                            Messages.LogError(this.GetACUrl(), msg.ACIdentifier, msg.InnerMessage);
                        OnNewAlarmOccurred(ProcessAlarm, msg, true);
                        return null;
                    }

                    var contentACClassWFVB = ContentACClassWF.FromAppContext<gip.mes.datamodel.ACClassWF>(dbApp);
                    ProdOrderBatch batch = pwMethodProduction.CurrentProdOrderBatch.FromAppContext<ProdOrderBatch>(dbApp);
                    ProdOrderBatchPlan batchPlan = batch.ProdOrderBatchPlan;
                    MaterialWFConnection matWFConnection = null;
                    if (batchPlan != null && batchPlan.MaterialWFACClassMethodID.HasValue)
                    {
                        matWFConnection = dbApp.MaterialWFConnection
                                                .Where(c => c.MaterialWFACClassMethod.MaterialWFACClassMethodID == batchPlan.MaterialWFACClassMethodID.Value
                                                        && c.ACClassWFID == contentACClassWFVB.ACClassWFID)
                                                .FirstOrDefault();
                    }
                    else
                    {
                        PartslistACClassMethod plMethod = endBatchPos.ProdOrderPartslist.Partslist.PartslistACClassMethod_Partslist.FirstOrDefault();
                        if (plMethod != null)
                        {
                            matWFConnection = dbApp.MaterialWFConnection
                                                    .Where(c => c.MaterialWFACClassMethod.MaterialWFACClassMethodID == plMethod.MaterialWFACClassMethodID
                                                            && c.ACClassWFID == contentACClassWFVB.ACClassWFID)
                                                    .FirstOrDefault();
                        }
                        else
                        {
                            matWFConnection = contentACClassWFVB.MaterialWFConnection_ACClassWF
                                .Where(c => c.MaterialWFACClassMethod.MaterialWFID == endBatchPos.ProdOrderPartslist.Partslist.MaterialWFID
                                            && c.MaterialWFACClassMethod.PartslistACClassMethod_MaterialWFACClassMethod.Where(d => d.PartslistID == endBatchPos.ProdOrderPartslist.PartslistID).Any())
                                .FirstOrDefault();
                        }
                    }

                    if (matWFConnection == null)
                    {
                        // Error50059: No relation defined between Workflownode and intermediate material in Materialworkflow
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartNextProdComponent(3)", 1020, "Error50059");

                        if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                            Messages.LogError(this.GetACUrl(), msg.ACIdentifier, msg.InnerMessage);
                        OnNewAlarmOccurred(ProcessAlarm, msg, true);
                        return null;
                    }

                    // Find intermediate position which is assigned to this Dosing-Workflownode
                    var currentProdOrderPartslist = endBatchPos.ProdOrderPartslist.FromAppContext<ProdOrderPartslist>(dbApp);
                    ProdOrderPartslistPos intermediatePosition = currentProdOrderPartslist.ProdOrderPartslistPos_ProdOrderPartslist
                        .Where(c => c.MaterialID.HasValue && c.MaterialID == matWFConnection.MaterialID
                            && c.MaterialPosType == GlobalApp.MaterialPosTypes.InwardIntern
                            && !c.ParentProdOrderPartslistPosID.HasValue).FirstOrDefault();
                    if (intermediatePosition == null)
                    {
                        // Error50061: Intermediate line not found which is assigned to this Dosing-Workflownode
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartNextProdComponent(4)", 1030, "Error50061");

                        if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                            Messages.LogError(this.GetACUrl(), msg.ACIdentifier, msg.InnerMessage);
                        OnNewAlarmOccurred(ProcessAlarm, msg, true);
                        return null;
                    }

                    ProdOrderPartslistPos intermediateChildPos = null;
                    // Lock, if a parallel Dosing also creates a child Position for this intermediate Position

                    using (ACMonitor.Lock(pwMethodProduction._62000_PWGroupLockObj))
                    {
                        // Find intermediate child position, which is assigned to this Batch
                        intermediateChildPos = intermediatePosition.ProdOrderPartslistPos_ParentProdOrderPartslistPos
                            .Where(c => c.ProdOrderBatchID.HasValue
                                        && c.ProdOrderBatchID.Value == pwMethodProduction.CurrentProdOrderBatch.ProdOrderBatchID)
                            .FirstOrDefault();

                        // If intermediate child position not found, generate childposition for this Batch/Intermediate
                        if (intermediateChildPos == null)
                        {
                            List<object> resultNewEntities = new List<object>();
                            msg = ProdOrderManager.BatchCreate(dbApp, intermediatePosition, batch, endBatchPos.BatchFraction, batch.BatchSeqNo, resultNewEntities); // Toleranz ist max. ein Batch mehr
                            if (msg != null)
                            {
                                Messages.LogException(this.GetACUrl(), "StartNextProdComponent(5)", msg.InnerMessage);
                                dbApp.ACUndoChanges();
                                return null;
                            }
                            else
                            {
                                dbApp.ACSaveChanges();
                            }
                            intermediateChildPos = resultNewEntities.Where(c => c is ProdOrderPartslistPos).FirstOrDefault() as ProdOrderPartslistPos;
                            if (intermediateChildPos != null && endBatchPos.FacilityLot != null)
                                endBatchPos.FacilityLot = endBatchPos.FacilityLot;
                        }
                    }
                    if (intermediateChildPos == null)
                    {
                        //Error50165:intermediateChildPos is null.
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartNextProdComponent(5a)", 1040, "Error50165");

                        OnNewAlarmOccurred(ProcessAlarm, msg, true);
                        return null;
                    }

                    ProdOrderPartslistPosRelation[] queryOpenDosings = OnGetOpenDosingsForNextComponent(dbIPlus, dbApp, intermediateChildPos);
                    if ((ComponentsSeqFrom > 0 || ComponentsSeqTo > 0) && queryOpenDosings != null && queryOpenDosings.Any())
                        queryOpenDosings = queryOpenDosings.Where(c => c.Sequence >= ComponentsSeqFrom && c.Sequence <= ComponentsSeqTo)
                                                            .OrderBy(c => c.Sequence)
                                                            .ToArray();

                    // Falls noch Dosierungen anstehen, dann dosiere nächste Komponente
                    if (queryOpenDosings != null && queryOpenDosings.Any())
                    {
                        queryOpenDosings = OnSortOpenDosings(queryOpenDosings, dbIPlus, dbApp);
                        foreach (ProdOrderPartslistPosRelation relation in queryOpenDosings)
                        {
                            if (!relation.SourceProdOrderPartslistPos.Material.UsageACProgram)
                                continue;
                            double dosingQuantity = relation.RemainingDosingQuantityUOM;

                            responsibleFunc = null;
                            gip.core.datamodel.ACClassMethod refPAACClassMethod = null;
                            using (ACMonitor.Lock(this.ContextLockForACClassWF))
                            {
                                refPAACClassMethod = this.ContentACClassWF.RefPAACClassMethod;
                            }
                            ACMethod acMethod = refPAACClassMethod.TypeACSignature();
                            if (acMethod == null)
                            {
                                //Error50154: acMethod is null.
                                msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartNextProdComponent(9a)", 1120, "Error50154");
                                OnNewAlarmOccurred(ProcessAlarm, msg, true);
                                return null;
                            }

                            IList<Facility> possibleSilos;

                            RouteQueryParams queryParams = new RouteQueryParams(RouteQueryPurpose.StartDosing,
                                OldestSilo ? ACPartslistManager.SearchMode.OnlyEnabledOldestSilo : ACPartslistManager.SearchMode.SilosWithOutwardEnabled,
                                null, null, ExcludedSilos);
                            IEnumerable<Route> routes = GetRoutes(relation, dbApp, dbIPlus, queryParams, out possibleSilos);

                            if (routes != null && routes.Any())
                            {
                                List<Route> routesList = routes.ToList();
                                processModule.GetACStateOfFunction(acMethod.ACIdentifier, out responsibleFunc);
                                if (responsibleFunc == null)
                                {
                                    //Error50327: Responsible dosingfunction for ACMethod {0} not found. Please check your logical brige from the InPoints of the processmodule to the InPoint of the dosingfunction.
                                    msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartNextProdComponent(9b)", 1121, "Error50327", acMethod.ACIdentifier);
                                    OnNewAlarmOccurred(ProcessAlarm, msg, true);
                                    return null;
                                }

                                PAFDosing dosingFunc = responsibleFunc as PAFDosing;
                                if (dosingFunc != null)
                                {
                                    foreach (Route currRoute in routes)
                                    {
                                        RouteItem lastRouteItem = currRoute.Items.LastOrDefault();
                                        if (lastRouteItem != null && lastRouteItem.TargetProperty != null)
                                        {
                                            // Gehe zur nächsten Komponente, weil es mehrere Dosierfunktionen gibt und der Eingangspunkt des Prozessmoduls nicht mit dem Eingangspunkt dieser Funktion übereinstimmt.
                                            // => eine andere Funktion ist dafür zuständig
                                            if (!dosingFunc.PAPointMatIn1.ConnectionList.Where(c => ((c as PAEdge).Source as PAPoint).ACIdentifier == lastRouteItem.TargetProperty.ACIdentifier).Any())
                                            {
                                                routesList.Remove(currRoute);
                                                //hasOpenDosings = true;
                                                //continue;
                                            }
                                        }
                                    }
                                }

                                routes = routesList;
                            }

                            if (routes == null || !routes.Any())
                            {
                                continue;
                            }

                            return dosingQuantity;
                        }
                    }
                }
            }
        

            return null;
        }

        #endregion
    }
}
