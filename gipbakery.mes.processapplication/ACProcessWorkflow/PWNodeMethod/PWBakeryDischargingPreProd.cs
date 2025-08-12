using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
using gip.mes.datamodel;
using gip.mes.facility;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'PWDischargingPreProd'}de{'PWDischargingPreProd'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryDischargingPreProd : PWDischarging
    {
        #region c'tors

        static PWBakeryDischargingPreProd()
        {
            List<ACMethodWrapper> wrappers = ACMethod.OverrideFromBase(typeof(PWBakeryDischargingPreProd), ACStateConst.SMStarting);
            if (wrappers != null)
            {
                foreach (ACMethodWrapper wrapper in wrappers)
                {
                    wrapper.Method.ParameterValueList.Add(new ACValue("NextFreeDestinationPMCheck", typeof(bool), false, Global.ParamOption.Required));
                    wrapper.ParameterTranslation.Add("NextFreeDestinationPMCheck", "en{'Get next free destination via predecessor process module occupation check'}de{'Nächstes freies Ziel über Vorgängerprozessmodul Belegungsprüfung ermitteln'}");
                }
            }


            RegisterExecuteHandler(typeof(PWBakeryDischargingPreProd), HandleExecuteACMethod_PWBakeryDischargingPreProd);
        }

        public PWBakeryDischargingPreProd(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACInit(Global.ACStartTypes startChildMode = Global.ACStartTypes.Automatic)
        {
            return base.ACInit(startChildMode);
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            StopMonitorSourceStore();
            return base.ACDeInit(deleteACClassTask);
        }

        public override void Recycle(IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
        {
            using (ACMonitor.Lock(_20015_LockValue))
            {
                _BookingProcessed = false;
            }
            StopMonitorSourceStore();
            base.Recycle(content, parentACObject, parameter, acIdentifier);
        }

        public new const string PWClassName = "PWBakeryDischargingPreProd";

        #endregion

        #region Properties

        private bool _BookingProcessed = false;
        private bool _SourceStoreMonitored = false;

        private ACRef<PAMParkingspace> _VirtualSourceStore;
        public PAMParkingspace VirtualSourceStore
        {
            get
            {   
                ACRef<PAMParkingspace> virtualSourceStore = null;
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    virtualSourceStore = _VirtualSourceStore;
                }
                return virtualSourceStore?.ValueT;
            }
        }

        private ACRef<PAMSilo> _VirtualTargetStore;
        public PAMSilo VirtualTargetStore
        {
            get
            {
                ACRef<PAMSilo> virtualTargetStore = null;
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    virtualTargetStore = _VirtualTargetStore;
                }
                return virtualTargetStore?.ValueT;
            }
        }

        public bool UseScaleWeightOnPost
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("UseScaleWeightOnPost");
                    if (acValue != null)
                        return acValue.ParamAsBoolean;
                }
                return false;
            }
        }

        public bool NextFreeDestinationPMCheck
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("NextFreeDestinationPMCheck");
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

        public override void SMRunning()
        {
            base.SMRunning();

            StartMonitorSourceStore();
            TryRelocateFromSourceStore();

            UnSubscribeToProjectWorkCycle();
        }

        public override void SMIdle()
        {
            using (ACMonitor.Lock(_20015_LockValue))
            {
                _BookingProcessed = false;
            }
            StopMonitorSourceStore();
            base.SMIdle();
        }

        public override void TaskCallback(IACPointNetBase sender, ACEventArgs e, IACObject wrapObject)
        {
            _InCallback = true;
            try
            {
                if (e != null)
                {
                    IACTask taskEntry = wrapObject as IACTask;
                    ACMethodEventArgs eM = e as ACMethodEventArgs;
                    _CurrentMethodEventArgs = eM;
                    if (taskEntry.State == PointProcessingState.Accepted && CurrentACState != ACStateEnum.SMIdle && eM.ResultState == Global.ACMethodResultState.InProcess)
                    {
                        CheckIfAutomaticTargetChangePossible = null;
                        ACMethod acMethod = e.ParentACMethod;
                        if (acMethod == null)
                            acMethod = taskEntry.ACMethod;
                        if (ParentPWGroup == null)
                        {

                            Messages.LogError(this.GetACUrl(), "TaskCallback()", "ParentPWGroup is null");
                            return;
                        }
                        PAProcessFunction discharging = ParentPWGroup.GetExecutingFunction<PAProcessFunction>(taskEntry.RequestID);
                        CheckIfAutomaticTargetChangePossible = null;

                        bool bookingProcessed = false;
                        using (ACMonitor.Lock(_20015_LockValue))
                        {
                            bookingProcessed = _BookingProcessed;
                        }

                        if (discharging != null && discharging.CurrentACState == ACStateEnum.SMRunning && !bookingProcessed)
                        {
                            double actualQuantity = 0;
                            var acValue = e.GetACValue("ActualQuantity");
                            if (acValue != null)
                                actualQuantity = acValue.ParamAsDouble;
                            //short simulationWeight = (short)acMethod["Source"];

                            if (UseScaleWeightOnPost && actualQuantity <= 0.000001)
                            {
                                PAFBakeryYeastProducing preProdFunc = discharging.ParentACComponent.FindChildComponents<PAFBakeryYeastProducing>(c => c is PAFBakeryYeastProducing)
                                                                                                   .FirstOrDefault();
                                if (preProdFunc != null)
                                {
                                    PAEScaleBase scaleBase = preProdFunc.GetFermentationStarterScale();
                                    if (scaleBase != null)
                                    {
                                        double actValue = scaleBase.ActualValue.ValueT;
                                        //PAEScaleTotalizing scaleTotal = scaleBase as PAEScaleTotalizing;
                                        //if (scaleTotal != null)
                                        //    actValue = scaleTotal.TotalActualWeight.ValueT;

                                        actualQuantity = actValue;
                                    }
                                }
                            }

                            using (var dbIPlus = new Database())
                            using (var dbApp = new DatabaseApp(dbIPlus))
                            {
                                ProdOrderPartslistPos currentBatchPos = null;
                                if (IsProduction)
                                {
                                    currentBatchPos = ParentPWMethod<PWMethodProduction>().CurrentProdOrderPartslistPos.FromAppContext<ProdOrderPartslistPos>(dbApp);
                                    // Wenn kein Istwert von der Funktion zurückgekommen, dann berechne Zugangsmenge über die Summe der dosierten Mengen
                                    // Minus der bereits zugebuchten Menge (falls zyklische Zugagnsbuchungen im Hintergrund erfolgten)
                                    if (actualQuantity <= 0.000001
                                        && (eM == null
                                           || eM.ResultState < Global.ACMethodResultState.Failed))
                                    {
                                        ACProdOrderManager prodOrderManager = ACProdOrderManager.GetServiceInstance(this);
                                        if (prodOrderManager != null)
                                        {
                                            double calculatedBatchWeight = 0;
                                            if (prodOrderManager.CalcProducedBatchWeight(dbApp, currentBatchPos, LossCorrectionFactor, out calculatedBatchWeight) == null)
                                            {
                                                double diff = calculatedBatchWeight - currentBatchPos.ActualQuantityUOM;
                                                if (diff > 0.00001)
                                                    actualQuantity = diff;
                                            }
                                        }
                                    }

                                    if ((this.IsSimulationOn/* || simulationWeight == 1*/)
                                        && actualQuantity <= 0.000001
                                        && currentBatchPos != null)
                                    {
                                        actualQuantity = currentBatchPos.TargetQuantityUOM;
                                    }
                                    // Entleerschritt liefert keine Menge
                                    else if (actualQuantity <= 0.000001)
                                    {
                                        actualQuantity = -0.001;
                                    }

                                    var routeItem = CurrentDischargingDest(dbIPlus);
                                    PAProcessModule targetModule = TargetPAModule(dbIPlus); // If Discharging is to Processmodule, then targetSilo ist null
                                    if (routeItem != null && targetModule != null)
                                    {
                                        try
                                        {
                                            DoInwardBooking(actualQuantity, dbApp, routeItem, null, currentBatchPos, e, true);
                                        }
                                        finally
                                        {
                                            routeItem.Detach();
                                        }
                                    }

                                    _BookingProcessed = true;

                                    if (ParentPWGroup != null)
                                    {
                                        List<PWDosing> previousDosings = PWDosing.FindPreviousDosingsInPWGroup<PWDosing>(this);
                                        if (previousDosings != null)
                                        {
                                            foreach (var pwDosing in previousDosings)
                                            {
                                                if (((ACSubStateEnum)ParentPWGroup.CurrentACSubState).HasFlag(ACSubStateEnum.SMDisThenNextComp))
                                                    pwDosing.ResetDosingsAfterInterDischarging(dbApp);
                                                else
                                                    pwDosing.SetDosingsCompletedAfterDischarging(dbApp);
                                            }
                                        }
                                    }
                                    if (dbApp.IsChanged)
                                    {
                                        dbApp.ACSaveChanges();
                                    }
                                }
                                else if (IsTransport)
                                {
                                    if (this.IsSimulationOn && actualQuantity <= 0.000001)
                                    {
                                        ACValue acValueTargetQ = acMethod.ParameterValueList.GetACValue("TargetQuantity");
                                        if (acValueTargetQ != null)
                                            actualQuantity = acValueTargetQ.ParamAsDouble;
                                    }

                                    var routeItem = CurrentDischargingDest(dbIPlus);
                                    PAProcessModule targetModule = TargetPAModule(dbIPlus); // If Discharging is to Processmodule, then targetSilo ist null
                                    if (routeItem != null && targetModule != null)
                                    {
                                        try
                                        {
                                            PAMSilo targetSilo = targetModule as PAMSilo;
                                            Facility inwardFacility = null;
                                            if (targetSilo != null && targetSilo.Facility.ValueT != null && targetSilo.Facility.ValueT.ValueT != null)
                                            {
                                                Guid facilityId = targetSilo.Facility.ValueT.ValueT.FacilityID;
                                                inwardFacility = dbApp.Facility.Where(c => c.FacilityID == facilityId).FirstOrDefault();
                                            }

                                            if (IsIntake)
                                            {
                                                var pwMethod = ParentPWMethod<PWMethodIntake>();
                                                Picking picking = null;
                                                DeliveryNotePos notePos = null;
                                                if (pwMethod.CurrentPicking != null)
                                                {
                                                    picking = pwMethod.CurrentPicking.FromAppContext<Picking>(dbApp);
                                                    PickingPos pickingPos = pwMethod.CurrentPickingPos != null ? pwMethod.CurrentPickingPos.FromAppContext<PickingPos>(dbApp) : null;
                                                    if (picking != null)
                                                        DoInwardBooking(actualQuantity, dbApp, routeItem, inwardFacility, picking, pickingPos, e, true);
                                                }
                                                else if (pwMethod.CurrentDeliveryNotePos != null)
                                                {
                                                    notePos = pwMethod.CurrentDeliveryNotePos.FromAppContext<DeliveryNotePos>(dbApp);
                                                    if (notePos != null)
                                                        DoInwardBooking(actualQuantity, dbApp, routeItem, inwardFacility, notePos, e, true);
                                                }
                                            }
                                            else if (IsRelocation)
                                            {
                                                var pwMethod = ParentPWMethod<PWMethodRelocation>();
                                                Picking picking = null;
                                                FacilityBooking fBooking = null;
                                                if (pwMethod.CurrentPicking != null)
                                                {
                                                    picking = pwMethod.CurrentPicking.FromAppContext<Picking>(dbApp);
                                                    PickingPos pickingPos = pwMethod.CurrentPickingPos != null ? pwMethod.CurrentPickingPos.FromAppContext<PickingPos>(dbApp) : null;
                                                    if (picking != null)
                                                    {
                                                        if (pickingPos != null)
                                                        {
                                                            MDDelivPosLoadState loadToTruck = DatabaseApp.s_cQry_GetMDDelivPosLoadState(dbApp, MDDelivPosLoadState.DelivPosLoadStates.LoadToTruck).FirstOrDefault();
                                                            if (loadToTruck != null)
                                                            {
                                                                pickingPos.MDDelivPosLoadState = loadToTruck;
                                                                Msg msg = dbApp.ACSaveChanges();
                                                                
                                                                if (msg != null)
                                                                {
                                                                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                                                                    {
                                                                        OnNewAlarmOccurred(ProcessAlarm, msg);
                                                                        Messages.LogMessageMsg(msg);
                                                                    }
                                                                }
                                                            }
                                                        }

                                                        if (this.IsSimulationOn && actualQuantity <= 0.000001 && pickingPos != null)
                                                            actualQuantity = pickingPos.TargetQuantityUOM;
                                                        DoInwardBooking(actualQuantity, dbApp, routeItem, null, picking, pickingPos, e, true);
                                                    }
                                                }
                                                else if (pwMethod.CurrentFacilityBooking != null)
                                                {
                                                    fBooking = pwMethod.CurrentFacilityBooking.FromAppContext<FacilityBooking>(dbApp);
                                                    if (fBooking != null)
                                                        DoInwardBooking(actualQuantity, dbApp, routeItem, fBooking, e, true);
                                                }
                                            }
                                            else if (IsLoading)
                                            {
                                                var pwMethod = ParentPWMethod<PWMethodLoading>();
                                                Picking picking = null;
                                                DeliveryNotePos notePos = null;
                                                if (pwMethod.CurrentPicking != null)
                                                {
                                                    picking = pwMethod.CurrentPicking.FromAppContext<Picking>(dbApp);
                                                    PickingPos pickingPos = pwMethod.CurrentPickingPos != null ? pwMethod.CurrentPickingPos.FromAppContext<PickingPos>(dbApp) : null;
                                                    if (picking != null)
                                                        DoOutwardBooking(actualQuantity, dbApp, routeItem, picking, pickingPos, e, true);
                                                }
                                                else if (pwMethod.CurrentDeliveryNotePos != null)
                                                {
                                                    notePos = pwMethod.CurrentDeliveryNotePos.FromAppContext<DeliveryNotePos>(dbApp);
                                                    if (notePos != null)
                                                        DoOutwardBooking(actualQuantity, dbApp, routeItem, notePos, e, true);
                                                }
                                            }
                                        }
                                        finally
                                        {
                                            routeItem.Detach();
                                        }
                                    }
                                }
                            }
                        }

                    }
                    if (PWPointRunning != null && eM != null && eM.ResultState == Global.ACMethodResultState.InProcess && taskEntry.State == PointProcessingState.Accepted)
                    {
                        PAProcessModule module = sender.ParentACComponent as PAProcessModule;
                        if (module != null)
                        {
                            PAProcessFunction function = module.GetExecutingFunction<PAProcessFunction>(eM.ACRequestID);
                            if (function != null)
                            {
                                if (function.CurrentACState == ACStateEnum.SMRunning)
                                {
                                    ACEventArgs eventArgs = ACEventArgs.GetVirtualEventArgs("PWPointRunning", VirtualEventArgs);
                                    eventArgs.GetACValue("TimeInfo").Value = RecalcTimeInfo();
                                    PWPointRunning.Raise(eventArgs);
                                }
                            }
                        }
                    }
                    else if (taskEntry.State == PointProcessingState.Deleted && CurrentACState != ACStateEnum.SMIdle)
                    {
                        //if (NoPostingOnRelocation)
                        //{
                        using (DatabaseApp dbApp = new DatabaseApp())
                        {
                            var pwMethod = ParentPWMethod<PWMethodRelocation>();
                            if (pwMethod != null)
                            {
                                PickingPos pickingPos = pwMethod.CurrentPickingPos != null ? pwMethod.CurrentPickingPos.FromAppContext<PickingPos>(dbApp) : null;

                                if (pickingPos != null)
                                {
                                    MDDelivPosLoadState loadToTruck = DatabaseApp.s_cQry_GetMDDelivPosLoadState(dbApp, MDDelivPosLoadState.DelivPosLoadStates.LoadToTruck).FirstOrDefault();
                                    if (loadToTruck != null)
                                    {
                                        pickingPos.MDDelivPosLoadState = loadToTruck;
                                        Msg msg = dbApp.ACSaveChanges();

                                        if (msg != null)
                                        {
                                            if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                                            {
                                                OnNewAlarmOccurred(ProcessAlarm, msg);
                                                Messages.LogMessageMsg(msg);
                                            }
                                        }
                                    }
                                    _BookingProcessed = true;
                                }
                            }
                        }
                        //}

                        UnSubscribeToProjectWorkCycle();
                        _LastCallbackResult = e;
                        CurrentACState = ACStateEnum.SMCompleted;
                    }

                    else if (taskEntry.State == PointProcessingState.Rejected)
                    {
                        //ACMethodEventArgs eMethodEventArgs = e as ACMethodEventArgs;
                        //if (eMethodEventArgs != null && eMethodEventArgs.ResultState == Global.ACMethodResultState.Failed)
                        //{
                        //}
                    }
                }
            }
            finally
            {
                _InCallback = false;
            }
        }

        public override Msg DoInwardBooking(double actualWeight, DatabaseApp dbApp, RouteItem dischargingDest, Facility facilityDest, Picking picking, PickingPos pickingPos, ACEventArgs e, bool isDischargingEnd)
        {
            //if (NoPostingOnRelocation)
            //{
            MDDelivPosLoadState loadToTruck = DatabaseApp.s_cQry_GetMDDelivPosLoadState(dbApp, MDDelivPosLoadState.DelivPosLoadStates.LoadToTruck).FirstOrDefault();
            MDDelivPosState completed = DatabaseApp.s_cQry_GetMDDelivPosState(dbApp, MDDelivPosState.DelivPosStates.CompletelyAssigned).FirstOrDefault();

            if (loadToTruck != null)
            {
                pickingPos.MDDelivPosLoadState = loadToTruck;

                if (pickingPos.OutOrderPos != null)
                    pickingPos.OutOrderPos.MDDelivPosState = completed;

                if (pickingPos.InOrderPos != null)
                    pickingPos.InOrderPos.MDDelivPosState = completed;

                return dbApp.ACSaveChanges();
            }
            //}
            return base.DoInwardBooking(actualWeight, dbApp, dischargingDest, facilityDest, picking, pickingPos, e, isDischargingEnd);
        }

        public override FacilityReservation GetNextFreeDestination(IList<FacilityReservation> plannedSilos, ProdOrderPartslistPos pPos, bool changeReservationStateIfFull, FacilityReservation ignoreFullSilo = null, PAFDischarging discharging = null)
        {
            if (NextFreeDestinationPMCheck)
            {
                if (plannedSilos == null || !plannedSilos.Any())
                    return null;

                List<Tuple<FacilityReservation, short>> plannedSilosFree = new List<Tuple<FacilityReservation, short>>();

                foreach (FacilityReservation plannedSilo in plannedSilos)
                {
                    string siloACUrl = plannedSilo.VBiACClass.ACURLComponentCached;
                    ACRoutingParameters param = new ACRoutingParameters();
                    param.Direction = RouteDirections.Backwards;
                    param.SelectionRuleID = PAProcessModule.SelRuleID_ProcessModule;
                    param.ResultMode = RouteResultMode.ShortRoute;

                    RoutingResult result = ACRoutingService.FindSuccessors(siloACUrl, param);
                    if (result != null && result.Routes != null && result.Routes.Any())
                    {
                        RouteItem rItem = result.Routes.FirstOrDefault().GetRouteSource();
                        if (rItem != null)
                        {
                            PAProcessModule module = rItem.SourceACComponent as PAProcessModule;
                            if (module != null)
                            {
                                string[] accessedFrom = module.SemaphoreAccessedFrom();
                                if (accessedFrom == null)
                                {
                                    plannedSilosFree.Add(new Tuple<FacilityReservation, short>(plannedSilo, 1));
                                }
                                else
                                {
                                    plannedSilosFree.Add(new Tuple<FacilityReservation, short>(plannedSilo, 2));
                                }
                            }
                        }
                    }
                }

                if (!plannedSilosFree.Any())
                    plannedSilosFree = plannedSilos.Select(c => new Tuple<FacilityReservation, short>(c, 1)).ToList();

                foreach (FacilityReservation plannedSilo in plannedSilosFree.OrderBy(c => c.Item2).Select(c => c.Item1).Where(c => c.ReservationState == GlobalApp.ReservationState.Active))
                {
                    if (CheckPlannedDestinationSilo(plannedSilo, pPos, changeReservationStateIfFull, ignoreFullSilo))
                        return plannedSilo;
                }
                foreach (FacilityReservation plannedSilo in plannedSilosFree.OrderBy(c => c.Item2).Select(c => c.Item1).Where(c => c.ReservationState == GlobalApp.ReservationState.New))
                {
                    if (CheckPlannedDestinationSilo(plannedSilo, pPos, changeReservationStateIfFull, ignoreFullSilo))
                        return plannedSilo;
                }
                foreach (FacilityReservation plannedSilo in plannedSilosFree.OrderBy(c => c.Item2).Select(c => c.Item1).Where(c => c.ReservationState == GlobalApp.ReservationState.Finished))
                {
                    if (CheckPlannedDestinationSilo(plannedSilo, pPos, changeReservationStateIfFull, ignoreFullSilo))
                    {
                        plannedSilo.ReservationState = GlobalApp.ReservationState.New;
                        return plannedSilo;
                    }
                }
                return null;
            }
            else
            {
                return base.GetNextFreeDestination(plannedSilos, pPos, changeReservationStateIfFull, ignoreFullSilo, discharging);
            }
        }

        private void StartMonitorSourceStore()
        {
            if (_SourceStoreMonitored)
                return;

            StopMonitorSourceStore();

            PAProcessModule module = ParentPWGroup?.AccessedProcessModule;
            if (module == null)
                return;
            PAFBakeryYeastProducing function = module.FindChildComponents<PAFBakeryYeastProducing>(c => c is PAFBakeryYeastProducing).FirstOrDefault();
            if (function == null)
                return;

            PAMParkingspace sourceStore = function.VirtualSourceStore;
            PAMSilo targetStore = function.VirtualTargetStore;
            if (sourceStore != null)
                sourceStore.RefreshParkingSpace.PropertyChanged += RefreshParkingSpace_PropertyChanged;

            var virtualSourceStore = new ACRef<PAMParkingspace>(sourceStore, this);
            var virtualTargetStore = new ACRef<PAMSilo>(targetStore, this);
            using (ACMonitor.Lock(_20015_LockValue))
            {
                _VirtualSourceStore = virtualSourceStore;
                _VirtualTargetStore = virtualTargetStore;
            }
            _SourceStoreMonitored = true;
            UnSubscribeToProjectWorkCycle();
        }

        [ACMethodInteraction("", "en{'Restart monitor source store'}de{'Monitor-Quellenspeicher neu starten'}", 650, true)]
        public void RestartMonitorSourceStore()
        {
            _SourceStoreMonitored = false;
            SubscribeToProjectWorkCycle();
        }

        private void StopMonitorSourceStore()
        {
            ACRef<PAMParkingspace> virtualSourceStore = null;
            ACRef<PAMSilo> virtualTargetStore = null;
            using (ACMonitor.Lock(_20015_LockValue))
            {
                virtualSourceStore = _VirtualSourceStore;
                _VirtualSourceStore = null;
                virtualTargetStore = _VirtualTargetStore;
                _VirtualTargetStore = null;
            }

            if (virtualSourceStore != null)
            {
                if (virtualSourceStore.ValueT != null)
                    virtualSourceStore.ValueT.RefreshParkingSpace.PropertyChanged -= RefreshParkingSpace_PropertyChanged;
                virtualSourceStore.Detach();
            }
            if (virtualTargetStore != null)
                virtualTargetStore.Detach();
            _SourceStoreMonitored = false;
        }

        private void TryRelocateFromSourceStore()
        {
            var virtualSourceStore = VirtualSourceStore;
            var virtualTargetStore = VirtualTargetStore;
            if (virtualSourceStore == null  || virtualTargetStore == null)
                return;

            using (DatabaseApp dbApp = new DatabaseApp())
            {
                Facility sourceFacility = dbApp.Facility.FirstOrDefault(c => c.VBiFacilityACClassID == virtualSourceStore.ComponentClass.ACClassID);
                Facility targetFacility = virtualTargetStore.Facility?.ValueT?.ValueT?.FromAppContext<Facility>(dbApp);

                if (sourceFacility == null || targetFacility == null)
                    return;

                FacilityCharge[] quantsInSource = sourceFacility.FacilityCharge_Facility
                                                                .Where(c => !c.NotAvailable 
                                                                          && c.MaterialID == targetFacility.MaterialID
                                                                          && c.FacilityBookingCharge_InwardFacilityCharge.FirstOrDefault()?.FacilityBookingType != GlobalApp.FacilityBookingType.InventoryNewQuant)
                                                                .ToArray();

                foreach (FacilityCharge quant in quantsInSource)
                {
                    RelocateFromSourceStoreToTarget(dbApp, quant, sourceFacility, targetFacility, quant.AvailableQuantity);
                }
            }
        }

        private void RelocateFromSourceStoreToTarget(DatabaseApp dbApp, FacilityCharge quant, Facility source, Facility target, double actualQuantity)
        {
            Msg msg;

            if (ACFacilityManager == null)
            {
                //Error50442: FacilityManager is not installed/configured!
                msg = new Msg(this, eMsgLevel.Error, PWClassName, "RelocateFromSourceStoreToTarget(10)", 526, "Error50442");
                ActivateProcessAlarmWithLog(msg);
                return;
            }

            bool outwardEnabled = source.OutwardEnabled;

            if (!source.OutwardEnabled)
                source.OutwardEnabled = true;


            ACMethodBooking bookParamRelocationClone = ACFacilityManager.ACUrlACTypeSignature("!" + GlobalApp.FBT_Relocation_Facility_BulkMaterial, gip.core.datamodel.Database.GlobalDatabase) as ACMethodBooking; // Immer Globalen context um Deadlock zu vermeiden 
            var bookingParam = bookParamRelocationClone.Clone() as ACMethodBooking;

            bookingParam.InwardFacility = target;
            bookingParam.OutwardFacility = source;

            //bookingParam.InwardFacilityCharge = quant;
            bookingParam.OutwardFacilityCharge = quant;

            bookingParam.InwardQuantity = actualQuantity;
            bookingParam.OutwardQuantity = actualQuantity;

            //bookingParam.InwardFacilityLot = quant.FacilityLot;
            bookingParam.OutwardFacilityLot = quant.FacilityLot;

            //bookingParam.InwardFacilityLocation = source;

            ACMethodEventArgs resultBooking = ACFacilityManager.BookFacility(bookingParam, dbApp);

            if (resultBooking.ResultState == Global.ACMethodResultState.Failed || resultBooking.ResultState == Global.ACMethodResultState.Notpossible)
            {
                msg = new Msg(bookingParam.ValidMessage.InnerMessage, this, eMsgLevel.Error, PWClassName, "RelocateFromSourceStoreToTarget(20)", 568);
                ActivateProcessAlarm(msg, false);
                return;
            }
            else
            {
                if (!bookingParam.ValidMessage.IsSucceded() || bookingParam.ValidMessage.HasWarnings())
                {
                    //collectedMessages.AddDetailMessage(resultBooking.ValidMessage);
                    msg = new Msg(bookingParam.ValidMessage.InnerMessage, this, eMsgLevel.Error, PWClassName, "RelocateFromSourceStoreToTarget(30)", 577);
                    ActivateProcessAlarmWithLog(msg, false);
                }
            }

            msg = dbApp.ACSaveChanges();
            if (msg != null)
                ActivateProcessAlarmWithLog(msg);

            source.OutwardEnabled = outwardEnabled;
            msg = dbApp.ACSaveChanges();
            if (msg != null)
                ActivateProcessAlarmWithLog(msg);
        }

        private void RefreshParkingSpace_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                SubscribeToProjectWorkCycle();
            }
        }

        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            switch (acMethodName)
            {
                case nameof(RestartMonitorSourceStore):
                    RestartMonitorSourceStore();
                    return true;
            }

            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }


        protected static bool HandleExecuteACMethod_PWBakeryDischargingPreProd(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, object[] acParameter)
        {
            return HandleExecuteACMethod_PWDischarging(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

        protected override void DumpPropertyList(XmlDocument doc, XmlElement xmlACPropertyList, ref DumpStats dumpStats)
        {
            base.DumpPropertyList(doc, xmlACPropertyList, ref dumpStats);

            XmlElement xmlChild = xmlACPropertyList["SourceStoreMonitored"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("SourceStoreMonitored");
                if (xmlChild != null)
                    xmlChild.InnerText = _SourceStoreMonitored.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["SourceStore"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("SourceStore");
                if (xmlChild != null)
                {
                    var virtualSourceStore = VirtualSourceStore;
                    if (virtualSourceStore != null)
                        xmlChild.InnerText = virtualSourceStore.GetACUrl();
                    else
                        xmlChild.InnerText = "null";
                }
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["TargetStore"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("TargetStore");
                if (xmlChild != null)
                {
                    var virtualTargetStore = VirtualTargetStore;
                    if (virtualTargetStore != null)
                        xmlChild.InnerText = virtualTargetStore.GetACUrl();
                    else
                        xmlChild.InnerText = "null";
                }
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["UseScaleWeightOnPost"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("UseScaleWeightOnPost");
                if (xmlChild != null)
                    xmlChild.InnerText = UseScaleWeightOnPost.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }
        }

        #endregion
    }
}
