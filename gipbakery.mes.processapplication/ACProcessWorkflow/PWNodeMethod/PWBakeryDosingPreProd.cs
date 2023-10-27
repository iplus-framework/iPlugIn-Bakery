using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
using gip.mes.datamodel;
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
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Workflowclass Dosing bakery in preproduction'}de{'Workflowklasse Dosieren Backerei in Vorproduktion'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryDosingPreProd : PWDosing
    {
        #region c'tors

        static PWBakeryDosingPreProd()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("SkipComponents", typeof(DosingSkipMode), DosingSkipMode.False, Global.ParamOption.Required));
            paramTranslation.Add("SkipComponents", "en{'Skip not dosable components'}de{'Überspringe nicht dosierbare Komponenten'}");
            method.ParameterValueList.Add(new ACValue("ComponentsSeqFrom", typeof(Int32), 0, Global.ParamOption.Optional));
            paramTranslation.Add("ComponentsSeqFrom", "en{'Components from Seq.-No.'}de{'Komponenten VON Seq.-Nr.'}");
            method.ParameterValueList.Add(new ACValue("ComponentsSeqTo", typeof(Int32), 0, Global.ParamOption.Optional));
            paramTranslation.Add("ComponentsSeqTo", "en{'Components to Seq.-No.'}de{'Komponenten BIS Seq.-Nr.'}");
            method.ParameterValueList.Add(new ACValue("ScaleOtherComp", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("ScaleOtherComp", "en{'Scale other components after Dosing'}de{'Restliche Komponenten anpassen'}");
            method.ParameterValueList.Add(new ACValue("ReservationMode", typeof(short), (short)0, Global.ParamOption.Optional));
            paramTranslation.Add("ReservationMode", "en{'Allow other lots if reservation'}de{'Erlaube andere Lose bei Reservierungen'}");
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
            method.ParameterValueList.Add(new ACValue("FacilityNoSort", typeof(string), null, Global.ParamOption.Optional));
            paramTranslation.Add("FacilityNoSort", "en{'Priorization order container number'}de{'Priorisierungsreihenfolge Silonummer'}");
            method.ParameterValueList.Add(new ACValue("DosingForFlour", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("DosingForFlour", "en{'Silo change without abort'}de{'Silowechsel ohne Abbrechen'}");
            method.ParameterValueList.Add(new ACValue("DoseAllPosFromPicking", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("DoseAllPosFromPicking", "en{'Dose all picking lines at the same time'}de{'Alle Kommissionierpositionen gleichzeitig dosieren'}");
            var wrapper = new ACMethodWrapper(method, "en{'Configuration'}de{'Konfiguration'}", typeof(PWBakeryDosingPreProd), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryDosingPreProd), ACStateConst.SMStarting, wrapper);
            RegisterExecuteHandler(typeof(PWBakeryDosingPreProd), HandleExecuteACMethod_PWDosing);
        }

        public PWBakeryDosingPreProd(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public new const string PWClassName = "PWBakeryDosingPreProd";

        #endregion

        #region Properties

        public double SourProdDosingUnit
        {
            get
            {
                var method = NewACMethodPAFWithConfiguration();
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("FlowSwitching1");
                    if (acValue != null && acValue.ParamAsDouble >= 0.000001)
                    {
                        return acValue.ParamAsDouble;
                    }
                }
                return 10;
            }
        }

        public TimeSpan SourProdDosingPause
        {
            get
            {
                var method = NewACMethodPAFWithConfiguration();
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("PulsationPauseTime");
                    if (acValue != null)
                    {
                        return acValue.ParamAsTimeSpan;
                    }
                }
                return new TimeSpan(0, 0, 2);
            }
        }

        /// <summary>
        /// Silo change without abort
        /// </summary>
        public bool DosingForFlour
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("DosingForFlour");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }

        #endregion

        #region Methods

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        public TimeSpan CalculateDuration(bool doseSimultaneously, PAProcessModule processModule, out PAFBakeryDosingWater water)
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
                        int nDosImpulse = (int)(targetQuantity / SourProdDosingUnit);
                        nDosImpulse--;
                        double nDosZeit = 0;
                        double dosPauseSec = SourProdDosingPause.TotalSeconds;
                        if ((nDosImpulse > 0) && (dosPauseSec > 0))
                            nDosZeit = nDosImpulse * dosPauseSec;

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
                msg = new Msg(this, eMsgLevel.Error, PWClassName, "GetTargetQuantity(1)", 201, "Error50167");

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
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "GetTargetQuantity(2)", 227, "Error50060");

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
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "GetTargetQuantity(3)", 268, "Error50059");

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
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "GetTargetQuantity(4)", 285, "Error50061");

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
                                Messages.LogException(this.GetACUrl(), "GetTargetQuantity(5)", msg.InnerMessage);
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
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "GetTargetQuantity(5a)", 327, "Error50165");

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
                            gip.core.datamodel.ACClassMethod refPAACClassMethod = this.RefACClassMethodOfContentWF;
                            ACMethod acMethod = refPAACClassMethod?.TypeACSignature();
                            if (acMethod == null)
                            {
                                //Error50154: acMethod is null.
                                msg = new Msg(this, eMsgLevel.Error, PWClassName, "GetTargetQuantity(9a)", 359, "Error50154");
                                OnNewAlarmOccurred(ProcessAlarm, msg, true);
                                return null;
                            }

                            ACPartslistManager.QrySilosResult possibleSilos;

                            RouteQueryParams queryParams = new RouteQueryParams(RouteQueryPurpose.StartDosing,
                                OldestSilo ? ACPartslistManager.SearchMode.OnlyEnabledOldestSilo : ACPartslistManager.SearchMode.SilosWithOutwardEnabled,
                                null, null, ExcludedSilos, ReservationMode);
                            IEnumerable<Route> routes = GetRoutes(relation, dbApp, dbIPlus, queryParams, null, out possibleSilos);

                            if (routes != null && routes.Any())
                            {
                                List<Route> routesList = routes.ToList();
                                processModule.GetACStateOfFunction(acMethod.ACIdentifier, out responsibleFunc);
                                if (responsibleFunc == null)
                                {
                                    //Error50327: Responsible dosingfunction for ACMethod {0} not found. Please check your logical brige from the InPoints of the processmodule to the InPoint of the dosingfunction.
                                    msg = new Msg(this, eMsgLevel.Error, PWClassName, "GetTargetQuantity(9b)", 378, "Error50327", acMethod.ACIdentifier);
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

        //Lack of material - silo change without abort on PAFDosing
        public override void OnHandleStateCheckEmptySilo(PAFDosing dosing)
        {
            // Silo change with abort
            if (!DosingForFlour)
            {
                base.OnHandleStateCheckEmptySilo(dosing);
            }
            // Else Silo change without abort
            else
            {
                double actualQuantity = 0;
                PAEScaleBase scale = dosing.CurrentScaleForWeighing;
                if (scale != null)
                {
                    actualQuantity = scale.ActualWeight.ValueT;
                    if (!IsSimulationOn)
                    {
                        PAEScaleTotalizing totalizingScale = scale as PAEScaleTotalizing;
                        if (totalizingScale != null)
                            actualQuantity = totalizingScale.TotalActualWeight.ValueT;
                    }
                }

                if (!ManuallyChangeSource
                    && dosing.StateLackOfMaterial.ValueT != PANotifyState.Off
                    && (   (dosing.CurrentACState == ACStateEnum.SMRunning && dosing.DosingAbortReason.ValueT == PADosingAbortReason.NotSet) 
                        || (   (dosing.CurrentACState == ACStateEnum.SMRunning || dosing.CurrentACState == ACStateEnum.SMPaused) 
                            && (dosing.DosingAbortReason.ValueT == PADosingAbortReason.EmptySourceNextSource || dosing.DosingAbortReason.ValueT == PADosingAbortReason.MachineMalfunction) )))
                {
                    PAMSilo silo = CurrentDosingSilo(null);
                    if (silo == null)
                        return;

                    DosingRestInfo restInfo = new DosingRestInfo(silo, dosing, null, dosing.IsSourceMarkedAsEmpty);// 10);

                    //if (silo.MatSensorEmtpy == null
                    //    || (silo.MatSensorEmtpy != null && silo.MatSensorEmtpy.SensorState.ValueT != PANotifyState.Off))
                    {
                        //silo.RefreshFacility(false, null);
                        //double zeroTolerance = 10;
                        //if (silo.Facility.ValueT != null && silo.Facility.ValueT.ValueT != null)
                        //    zeroTolerance = silo.Facility.ValueT.ValueT.Tolerance;
                        //if (zeroTolerance <= 0.1)
                        //    zeroTolerance = 10;

                        // Überprüfe Rechnerischen Restbestand des Silos
                        //double rest = silo.FillLevel.ValueT - actualQuantity;
                        bool doZeroBooking = (restInfo.InZeroTolerance && restInfo.IsZeroTolSet) || dosing.DosingAbortReason.ValueT == PADosingAbortReason.EmptySourceNextSource;
                        if (   doZeroBooking
                            || dosing.DosingAbortReason.ValueT == PADosingAbortReason.MachineMalfunction)
                        {
                            // Falls Methode true zurückgibt
                            EmptySiloHandlingOptions handlingOptions = HandleAbortReasonOnEmptySilo(silo);
                            if (handlingOptions.HasFlag(EmptySiloHandlingOptions.OtherSilosAvailable))
                            {
                                bool zeroBookSucceeded = true;
                                if (doZeroBooking)
                                    zeroBookSucceeded = ZeroBookSource(silo);
                                if (zeroBookSucceeded)
                                {
                                    Route nextDosingSource = FindNextSource();
                                    if (nextDosingSource == null)
                                    {
                                        // Warning50005: No Silo/Tank/Container found for component {0}
                                        Msg msg = new Msg(this, eMsgLevel.Warning, PWClassName, "OnHandleStateCheckEmptySilo(100)", 100, "Warning50005",
                                                        silo.MaterialName != null ? silo.MaterialName.ValueT : "");

                                        if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                                            Messages.LogError(this.GetACUrl(), msg.ACIdentifier, msg.InnerMessage);
                                        OnNewAlarmOccurred(ProcessAlarm, msg, true);
                                        OnNextSourceSiloNotFound();
                                    }
                                    else
                                    {
                                        CurrentDosingRoute = nextDosingSource;
                                        PAMSilo sourceSilo = CurrentDosingSilo(null);
                                        if (sourceSilo == null)
                                        {
                                            OnNextSourceSiloNotFound();
                                        }
                                        else
                                        {
                                            ACMethod acMethod = dosing.CurrentACMethod.ValueT.Clone() as ACMethod;
                                            if (acMethod != null)
                                            {
                                                acMethod["Route"] = CurrentDosingRoute;
                                                acMethod["Source"] = sourceSilo.RouteItemIDAsNum;

                                                //dosing.CurrentACMethod.ValueT = acMethod;
                                                Msg msg = dosing.ReSendACMethod(acMethod);
                                                if (msg != null)
                                                {
                                                    OnNewAlarmOccurred(ProcessAlarm, msg);
                                                }

                                                dosing.AcknowledgeAlarms();
                                                OnNextSourceSiloFound(silo.RouteItemIDAsNum, sourceSilo.RouteItemIDAsNum);
                                            }
                                        }
                                    }
                                }
                            }
                            else if (handlingOptions.HasFlag(EmptySiloHandlingOptions.NoSilosAvailable))
                            {
                                // Warning50005: No Silo/Tank/Container found for component {0}
                                Msg msg = new Msg(this, eMsgLevel.Warning, PWClassName, "OnHandleStateCheckEmptySilo(110)", 100, "Warning50005",
                                                silo.MaterialName != null ? silo.MaterialName.ValueT : "");

                                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                                    Messages.LogError(this.GetACUrl(), msg.ACIdentifier, msg.InnerMessage);
                                OnNewAlarmOccurred(ProcessAlarm, msg, true);
                                OnNextSourceSiloNotFound();
                            }
                        }
                        else
                        {
                            // Warning50030:  Lack of Material: Stock in Silo / Tank / Container {0} is to high for automatic switching to another Silo / Tank / Container
                            Msg msg = new Msg(this, eMsgLevel.Warning, PWClassName, "OnHandleStateCheckEmptySilo(120)", 101, "Warning50030",
                                            silo.ACIdentifier);

                            if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                                Messages.LogError(this.GetACUrl(), msg.ACIdentifier, msg.InnerMessage);
                            OnNewAlarmOccurred(ProcessAlarm, msg, true);
                            OnNextSourceSiloNotFound();
                        }
                    }
                }
            }
        }

        public virtual bool WillSiloChangedWithoutAbort
        {
            get
            {
                return DosingForFlour;
            }
        }

        public virtual void OnNextSourceSiloFound(int oldSourceSilo, int newSourceSilo)
        {

        }

        public virtual void OnNextSourceSiloNotFound()
        {

        }

        private Route FindNextSource()
        {
            ACPartslistManager.QrySilosResult possibleSilos = null;
            PAProcessFunction responsibleFunc;

            using (Database dbIPlus = new gip.core.datamodel.Database())
            using (DatabaseApp dbApp = new DatabaseApp(dbIPlus))
            {
                Guid posID = CurrentDosingPos.ValueT;
                Msg msg = null;

                PAProcessModule module = ParentPWGroup.AccessedProcessModule;
                ACMethod acMethod = CurrentACMethod.ValueT;

                RouteQueryParams queryParams = new RouteQueryParams(RouteQueryPurpose.StartDosing,
                    OldestSilo ? ACPartslistManager.SearchMode.OnlyEnabledOldestSilo : ACPartslistManager.SearchMode.SilosWithOutwardEnabled,
                    null, null, ExcludedSilos, ReservationMode);


                IEnumerable<Route> routes = null;

                if (IsProduction)
                {
                    ProdOrderPartslistPosRelation relation = dbApp.ProdOrderPartslistPosRelation.FirstOrDefault(c => c.ProdOrderPartslistPosRelationID == posID);
                    routes = GetRoutes(relation, dbApp, dbIPlus, queryParams, null, out possibleSilos);
                }
                else if (IsTransport)
                {
                    PickingPos pickingPos = dbApp.PickingPos.FirstOrDefault(c => c.PickingPosID == posID);
                    routes = GetRoutes(pickingPos, dbApp, dbIPlus, queryParams, null, out possibleSilos);
                }

                if (routes != null && routes.Any())
                {
                    List<Route> routesList = routes.ToList();
                    module.GetACStateOfFunction(acMethod.ACIdentifier, out responsibleFunc);
                    if (responsibleFunc == null)
                    {
                        //Error50327: Responsible dosingfunction for ACMethod {0} not found. Please check your logical brige from the InPoints of the processmodule to the InPoint of the dosingfunction.
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "FindNextSource(10)", 588, "Error50327", acMethod.ACIdentifier);
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

                if (routes == null || !routes.Any() || possibleSilos == null)
                {
                    return null;
                }

                // 3. Finde die Route mit der höchsten Siloprioriät
                Route dosingRoute = null;
                foreach (var prioSilo in possibleSilos.FilteredResult)
                {
                    if (!prioSilo.StorageBin.VBiFacilityACClassID.HasValue)
                        continue;
                    dosingRoute = routes.Where(c => c.LastOrDefault().Source.ACClassID == prioSilo.StorageBin.VBiFacilityACClassID).FirstOrDefault();
                    if (dosingRoute != null)
                        break;
                }

                return dosingRoute;
            }
        }

        private bool ZeroBookSource(PAMSilo currentSilo)
        {
            MsgWithDetails collectedMessages = new MsgWithDetails();

            using (Database dbIPlus = new gip.core.datamodel.Database())
            using (DatabaseApp dbApp = new DatabaseApp())
            {
                Facility outwardFacility = currentSilo.Facility?.ValueT?.ValueT?.FromAppContext<Facility>(dbApp);

                if (outwardFacility == null)
                {
                    Msg msg = new Msg("The Facility is not assigned to the silo: " + currentSilo.ACUrl, this, eMsgLevel.Error, PWClassName, "ZeroBookSource(10)", 647);
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ProcessAlarm, msg);
                        Messages.LogMessageMsg(msg);
                    }

                    return false;
                }

                bool hasQuants = outwardFacility.FacilityCharge_Facility.Where(c => c.NotAvailable == false).Any();

                bool zeroBookSucceeded = false;
                if (hasQuants)
                {
                    zeroBookSucceeded = true;
                    ACMethodBooking zeroBooking = ACFacilityManager.ACUrlACTypeSignature("!" + GlobalApp.FBT_ZeroStock_Facility_BulkMaterial, gip.core.datamodel.Database.GlobalDatabase) as ACMethodBooking;
                    zeroBooking = zeroBooking.Clone() as ACMethodBooking;
                    zeroBooking.MDZeroStockState = MDZeroStockState.DefaultMDZeroStockState(dbApp, MDZeroStockState.ZeroStockStates.SetNotAvailable);
                    zeroBooking.InwardFacility = outwardFacility;
                    if (ParentPWGroup != null && ParentPWGroup.AccessedProcessModule != null)
                        zeroBooking.PropertyACUrl = ParentPWGroup.AccessedProcessModule.GetACUrl();
                    //zeroBooking.OutwardFacility = outwardFacility;
                    zeroBooking.IgnoreIsEnabled = true;
                    ACMethodEventArgs resultBooking = ACFacilityManager.BookFacilityWithRetry(ref zeroBooking, dbApp);
                    if (resultBooking.ResultState == Global.ACMethodResultState.Failed || resultBooking.ResultState == Global.ACMethodResultState.Notpossible)
                    {
                        collectedMessages.AddDetailMessage(resultBooking.ValidMessage);
                        zeroBookSucceeded = false;
                        OnNewAlarmOccurred(ProcessAlarm, new Msg(zeroBooking.ValidMessage.InnerMessage, this, eMsgLevel.Error, PWClassName, "ZeroBookSource(10)", 636), true);
                    }
                    else
                    {
                        if (!zeroBooking.ValidMessage.IsSucceded() || zeroBooking.ValidMessage.HasWarnings())
                        {
                            if (!zeroBooking.ValidMessage.IsSucceded())
                                collectedMessages.AddDetailMessage(resultBooking.ValidMessage);
                            Messages.LogError(this.GetACUrl(), "ZeroBookSource(20)", zeroBooking.ValidMessage.InnerMessage);
                            OnNewAlarmOccurred(ProcessAlarm, new Msg(zeroBooking.ValidMessage.InnerMessage, this, eMsgLevel.Error, PWClassName, "ZeroBookSource(30)", 465), true);
                        }
                        else
                            zeroBookSucceeded = true;
                    }
                }
                else
                    zeroBookSucceeded = true;

                return zeroBookSucceeded;
            }
        }

        protected override void DumpPropertyList(XmlDocument doc, XmlElement xmlACPropertyList)
        {
            base.DumpPropertyList(doc, xmlACPropertyList);

            XmlElement xmlChild = xmlACPropertyList["SourProdDosingUnit"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("SourProdDosingUnit");
                if (xmlChild != null)
                    xmlChild.InnerText = SourProdDosingUnit.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["SourProdDosingPause"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("SourProdDosingPause");
                if (xmlChild != null)
                    xmlChild.InnerText = SourProdDosingPause.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["DosingForFlour"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("DosingForFlour");
                if (xmlChild != null)
                    xmlChild.InnerText = DosingForFlour.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }
        }

        #endregion
    }
}
