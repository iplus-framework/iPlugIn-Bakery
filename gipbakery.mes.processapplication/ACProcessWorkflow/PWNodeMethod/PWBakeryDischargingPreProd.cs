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
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'PWDischargingPreProd'}de{'PWDischargingPreProd'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryDischargingPreProd : PWDischarging
    {
        static PWBakeryDischargingPreProd()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("SkipIfNoComp", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("SkipIfNoComp", "en{'Skip if no components dosed'}de{'Überspringe wenn keine Komponente dosiert'}");
            method.ParameterValueList.Add(new ACValue("LimitToMaxCapOfDest", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("LimitToMaxCapOfDest", "en{'Limit filling of destination to available space'}de{'Limitiere Zielbefüllung auf rechnerischen Restinhalt'}");
            method.ParameterValueList.Add(new ACValue("PrePostQOnDest", typeof(double), 0.0, Global.ParamOption.Optional));
            paramTranslation.Add("PrePostQOnDest", "en{'Pre posting quantity to destination at start'}de{'Vorbuchungsmenge auf Ziel bei Start'}");
            method.ParameterValueList.Add(new ACValue("NoPostingOnRelocation", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("NoPostingOnRelocation", "en{'No posting at relocation'}de{'Keine Buchung bei Umlagerung'}");
            method.ParameterValueList.Add(new ACValue("NoPostingOnProd", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("NoPostingOnProd", "en{'No posting at production'}de{'Keine Buchung bei Producion'}");

            var wrapper = new ACMethodWrapper(method, "en{'Configuration'}de{'Konfiguration'}", typeof(PWBakeryDischargingPreProd), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryDischargingPreProd), ACStateConst.SMStarting, wrapper);
            RegisterExecuteHandler(typeof(PWBakeryDischargingPreProd), HandleExecuteACMethod_PWBakeryDischargingPreProd);
        }

        

        public PWBakeryDischargingPreProd(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        private bool _BookingProcessed = false;

        protected bool NoPostingOnProd
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("NoPostingOnProd");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }

        public override Msg DoInwardBooking(double actualQuantity, DatabaseApp dbApp, RouteItem dischargingDest, Facility inwardFacility, ProdOrderPartslistPos currentBatchPos, ACEventArgs e, bool isDischargingEnd, bool blockQuant = false)
        {
            if (!NoPostingOnProd)
                return base.DoInwardBooking(actualQuantity, dbApp, dischargingDest, inwardFacility, currentBatchPos, e, isDischargingEnd, blockQuant);
            return null;
        }

        public override StartDisResult CheckCachedModuleDestinations(ref ACComponent dischargeToModule, ref Msg msg)
        {
            if (!NoPostingOnProd)
            {
                return base.CheckCachedModuleDestinations(ref dischargeToModule, ref msg);
            }
            else
            {
                using (Database db = new gip.core.datamodel.Database())
                {
                    RoutingResult rResult = ACRoutingService.FindSuccessors(RoutingService, db, RoutingService != null && RoutingService.IsProxy,
                                        ParentPWGroup.AccessedProcessModule, PAProcessModule.SelRuleID_ProcessModule, RouteDirections.Forwards, new object[] { },
                                        (c, p, r) => c.ACKind == Global.ACKinds.TPAProcessModule,
                                        null,
                                        0, true, true, false, false);

                    if (rResult != null && rResult.Routes.Any())
                    {
                        dischargeToModule = rResult.Routes.FirstOrDefault()?.GetRouteTarget()?.TargetACComponent as ACComponent;
                    }

                }
                
                return StartDisResult.WaitForCallback;
            }
        }

        public override void SMIdle()
        {
            using (ACMonitor.Lock(_20015_LockValue))
            {
                _BookingProcessed = false;
            }
            base.SMIdle();
        }

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        private static bool HandleExecuteACMethod_PWBakeryDischargingPreProd(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, object[] acParameter)
        {
            return HandleExecuteACMethod_PWDischarging(out result, acComponent, acMethodName, acClassMethod, acParameter);
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

                            if (actualQuantity <= 0.000001)
                            {
                                PAFBakeryDosingFlour flourDosing = discharging.ParentACComponent.FindChildComponents<PAFBakeryDosingFlour>(c => c is PAFBakeryDosingFlour).FirstOrDefault();
                                if (flourDosing != null)
                                {
                                    actualQuantity = flourDosing.CurrentScaleForWeighing.ActualValue.ValueT;
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
                                            if (prodOrderManager.CalcProducedBatchWeight(dbApp, currentBatchPos, out calculatedBatchWeight) == null)
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
                                                        DoInwardBooking(actualQuantity, dbApp, routeItem, picking, pickingPos, e, true);
                                                }
                                                else if (pwMethod.CurrentDeliveryNotePos != null)
                                                {
                                                    notePos = pwMethod.CurrentDeliveryNotePos.FromAppContext<DeliveryNotePos>(dbApp);
                                                    if (notePos != null)
                                                        DoInwardBooking(actualQuantity, dbApp, routeItem, notePos, e, true);
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
                                                        if (this.IsSimulationOn && actualQuantity <= 0.000001 && pickingPos != null)
                                                            actualQuantity = pickingPos.TargetQuantityUOM;
                                                        DoInwardBooking(actualQuantity, dbApp, routeItem, picking, pickingPos, e, true);
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
    }
}
