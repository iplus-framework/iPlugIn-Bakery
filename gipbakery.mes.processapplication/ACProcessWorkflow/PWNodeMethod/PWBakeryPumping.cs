using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
using gip.mes.datamodel;
using gip.mes.facility;
using gip.mes.processapplication;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'PWBakeryPumping'}de{'PWBakeryPumping'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryPumping : PWDischarging
    {
        #region c'tors

        static PWBakeryPumping()
        {
            List<ACMethodWrapper> wrappers = ACMethod.OverrideFromBase(typeof(PWBakeryPumping), ACStateConst.SMStarting);
            if (wrappers != null)
            {
                foreach (ACMethodWrapper wrapper in wrappers)
                {
                    wrapper.Method.ParameterValueList.Add(new ACValue("BlockSourceAtEnd", typeof(bool), false, Global.ParamOption.Required));
                    wrapper.ParameterTranslation.Add("BlockSourceAtEnd", "en{'Block Source when function completes'}de{'Quelle sperren bei Funktionsende'}");
                    wrapper.Method.ParameterValueList.Add(new ACValue("RelocateToFinalDest", typeof(bool), false, Global.ParamOption.Required));
                    wrapper.ParameterTranslation.Add("RelocateToFinalDest", "en{'Post quant into virtual destination bin'}de{'Quant direkt in virtuellen Zielbehälter umbuchen'}");
                    wrapper.Method.ParameterValueList.Add(new ACValue("PreventPumpAtProd", typeof(bool), true, Global.ParamOption.Optional));
                    wrapper.ParameterTranslation.Add("PreventPumpAtProd", "en{'No pumping during production'}de{'Kein Umpumpen während der Produktion'}");
                    wrapper.Method.ParameterValueList.Add(new ACValue("RouteItemIDChangeMode", typeof(ushort), 0, Global.ParamOption.Optional));
                    wrapper.ParameterTranslation.Add("RouteItemIDChangeMode", "en{'Changemode of RouteItemID (1=Remove last digit)'}de{'Änderungsmodus der RouteItemID (1=letzte Stelle entfernen)'}");
                    wrapper.Method.ParameterValueList.Add(new ACValue("BookTargetQuantity", typeof(bool), false, Global.ParamOption.Optional));
                    wrapper.ParameterTranslation.Add("BookTargetQuantity", "en{'Post target quantity'}de{'Zielmenge buchen'}");
                }
            }
            RegisterExecuteHandler(typeof(PWBakeryPumping), HandleExecuteACMethod_PWBakeryPumping);
        }

        public PWBakeryPumping(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            if (_TargetScale != null)
            {
                _TargetScale.Detach();
                _TargetScale = null;
            }
            if (_SourceModuleRef != null)
            {
                _SourceModuleRef.Detach();
                _SourceModuleRef = null;
            }

            return base.ACDeInit(deleteACClassTask);
        }

        public override void Recycle(IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
        {
            if (_TargetScale != null)
            {
                _TargetScale.Detach();
                _TargetScale = null;
            }
            if (_SourceModuleRef != null)
            {
                _SourceModuleRef.Detach();
                _SourceModuleRef = null;
            }
            base.Recycle(content, parentACObject, parameter, acIdentifier);
        }

        public new const string PWClassName = "PWBakeryPumping";

        #endregion

        #region Properties

        private double _ScaleWeightOnStart;
        [ACPropertyInfo(true, 9999)]
        public double ScaleWeightOnStart
        {
            get => _ScaleWeightOnStart;
            set
            {
                _ScaleWeightOnStart = value;
                OnPropertyChanged("ScaleWeightOnStart");
            }
        }

        private ACRef<PAEScaleBase> _TargetScale;
        private PAEScaleBase TargetScale
        {
            get
            {
                return _TargetScale?.ValueT;
            }
        }

        public bool BlockSourceAtEnd
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("BlockSourceAtEnd");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }

        public bool RelocateToFinalDest
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("RelocateToFinalDest");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }

        public ushort RouteItemIDChangeMode
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("RouteItemIDChangeMode");
                    if (acValue != null)
                    {
                        return acValue.ParamAsUInt16;
                    }
                }
                return 0;
            }
        }

        public bool PreventPumpAtProd
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("PreventPumpAtProd");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return true;
            }
        }

        public bool BookTargetQuantity
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("BookTargetQuantity");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }

        ACRef<PAProcessModule> _SourceModuleRef;
        protected PAProcessModule SourceModule
        {
            get
            {
                if (_SourceModuleRef != null)
                    return _SourceModuleRef.ValueT;
                if (ParentPWGroup == null || ParentPWGroup.AccessedProcessModule == null)
                    return null;
                string thisPumpGroup = ParentPWGroup.AccessedProcessModule.GetACUrl();
                PAFBakeryYeastProducing pafYProd = ApplicationManager.FindChildComponents<PAFBakeryYeastProducing>(c => c is PAFBakeryYeastProducing && (c as PAFBakeryYeastProducing).PumpOverProcessModuleACUrl == thisPumpGroup).FirstOrDefault();
                if (pafYProd == null)
                    return null;
                PAProcessModule yeast = pafYProd.ParentACComponent as PAProcessModule;
                if (yeast == null)
                    return null;
                _SourceModuleRef = new ACRef<PAProcessModule>(yeast, this);
                return yeast;
            }
        }

        #endregion

        #region Methods

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            if (PreventPumpAtProd)
            {
                bool canStartPump = true;
                PAProcessModule sourceModule = SourceModule;
                if (sourceModule != null)
                {
                    canStartPump = string.IsNullOrEmpty(sourceModule.OrderInfo.ValueT);
                    if (!canStartPump)
                    {
                        PAFDischarging pafDis = sourceModule.FindChildComponents<PAFDischarging>(c => c is PAFDischarging).FirstOrDefault();
                        if (pafDis == null || pafDis.CurrentACState != ACStateEnum.SMIdle)
                            canStartPump = true;
                    }
                }
                // Wait
                if (!canStartPump)
                {
                    SubscribeToProjectWorkCycle();
                    return;
                }
                else
                    base.SMStarting();
            }
            else
                base.SMStarting();
        }

        public override void SMIdle()
        {
            ScaleWeightOnStart = 0;
            base.SMIdle();
        }

        public override bool AfterConfigForACMethodIsSet(ACMethod paramMethod, bool isForPAF, params object[] acParameter)
        {
            DatabaseApp dbApp = acParameter[0] as DatabaseApp;
            PickingPos pickingPos = acParameter[1] as PickingPos;
            PAProcessModule targetModule = acParameter[2] as PAProcessModule;

            if (dbApp == null || pickingPos == null || pickingPos.FromFacility == null)
                return false;

            PAProcessModule sModule, tModule = null;

            //Helper route with source virtual store
            Route currentRoute = null;

            using (Database db = new Database())
            {
                gip.core.datamodel.ACClass sourceFacilityClass = pickingPos.FromFacility.GetFacilityACClass(db);
                gip.core.datamodel.ACClass targetModuleClass = targetModule.ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(db);

                ACRoutingParameters routingParameters = new ACRoutingParameters()
                {
                    RoutingService = this.RoutingService,
                    Database = db,
                    AttachRouteItemsToContext = false,
                    SelectionRuleID = PAProcessModule.SelRuleID_ProcessModule,
                    Direction = RouteDirections.Backwards,
                    MaxRouteAlternativesInLoop = 0,
                    IncludeReserved = true,
                    IncludeAllocated = true
                };

                RoutingResult rResult = ACRoutingService.FindSuccessors(sourceFacilityClass, routingParameters);

                currentRoute = rResult.Routes.FirstOrDefault();

                sModule = currentRoute?.GetRouteSource()?.SourceACComponent as PAProcessModule;

                routingParameters.Direction = RouteDirections.Forwards;

                rResult = ACRoutingService.FindSuccessors(targetModuleClass, routingParameters);

                tModule = rResult?.Routes?.FirstOrDefault()?.GetRouteTarget()?.TargetACComponent as PAProcessModule;
            }

            if (sModule == null || tModule == null)
                return false;

            var pafPreProd = sModule.FindChildComponents<PAFBakeryYeastProducing>().FirstOrDefault();
            PAEScaleBase scaleBase = pafPreProd?.GetFermentationStarterScale();

            if (scaleBase == null)
            {
                //Error50476: Can not get the scale from a pump source. Please configure the FermentationStarterScale on the PAFSourdoughProduction under pump source process module.
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "AfterConfigForACMethodIsSet", 95, "Error50476");
                OnNewAlarmOccurred(ProcessAlarm, msg);
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                {
                    Messages.LogMessageMsg(msg);
                }

                MDDelivPosLoadState loadToTruck = DatabaseApp.s_cQry_GetMDDelivPosLoadState(dbApp, MDDelivPosLoadState.DelivPosLoadStates.LoadToTruck).FirstOrDefault();
                if (loadToTruck != null)
                {
                    PickingPos pPos = pickingPos.FromAppContext<PickingPos>(dbApp);
                    pPos.MDDelivPosLoadState = loadToTruck;
                    dbApp.ACSaveChanges();
                }

                return false;
            }

            _TargetScale = new ACRef<PAEScaleBase>(scaleBase, this);

            SaveScaleWeightOnStart(scaleBase);

            int sourceID = sModule.RouteItemIDAsNum;
            int targetID = tModule.RouteItemIDAsNum;
            if (RouteItemIDChangeMode == 1)
            {
                sourceID = sourceID / 10;
                targetID = targetID / 10;
            }

            paramMethod["Source"] = sourceID;
            paramMethod["Destination"] = targetID;
            paramMethod["Route"] = currentRoute;
            paramMethod["TargetQuantity"] = pickingPos.PickingQuantityUOM;
            paramMethod["ScaleACUrl"] = scaleBase?.ACUrl;

            return true;
        }

        private bool SaveScaleWeightOnStart(PAEScaleBase scaleBase)
        {
            ScaleWeightOnStart = scaleBase.ActualValue.ValueT;
            return true;
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

                    if (taskEntry.State == PointProcessingState.Deleted && CurrentACState != ACStateEnum.SMIdle)
                    {
                        ACMethod acMethod = e.ParentACMethod;
                        if (acMethod == null)
                            acMethod = taskEntry.ACMethod;

                        PAProcessModule module = sender.ParentACComponent as PAProcessModule;
                        if (module != null)
                        {
                            PAFBakeryPumping function = module.GetExecutingFunction<PAFBakeryPumping>(eM.ACRequestID);
                            if (function != null)
                            {
                                PAEScaleBase scaleBase = TargetScale;

                                if (scaleBase == null)
                                {
                                    ACValue scaleACUrl = acMethod.ParameterValueList.GetACValue("ScaleACUrl");
                                    if (scaleACUrl != null)
                                    {
                                        string acUrl = scaleACUrl.ParamAsString;
                                        scaleBase = Root.ACUrlCommand(acUrl) as PAEScaleBase;
                                    }
                                }

                                if (scaleBase != null)
                                {
                                    double scaleWeightAfterPumping = scaleBase.ActualValue.ValueT;

                                    double actualQuantity = ScaleWeightOnStart - scaleWeightAfterPumping;

                                    if (actualQuantity <= -0.0000001)
                                        actualQuantity = 0.0;
                                    acMethod.ResultValueList["ActualQuantity"] = actualQuantity;
                                }

                                if (BookTargetQuantity)
                                {
                                    ACValue tQ = acMethod.ParameterValueList.GetACValue("TargetQuantity");
                                    if (tQ != null)
                                    {
                                        acMethod.ResultValueList["ActualQuantity"] = tQ.ParamAsDouble;
                                    }
                                }

                                base.TaskCallback(sender, e, wrapObject);

                                if (BlockSourceAtEnd)
                                {
                                    ACValue rValue = acMethod.ParameterValueList.GetACValue("Route");
                                    if (rValue != null && rValue.Value != null)
                                    {
                                        Route route = rValue.Value as Route;
                                        if (route != null)
                                        {

                                            using (DatabaseApp dbApp = new DatabaseApp())
                                            {
                                                if (!route.IsAttached)
                                                {
                                                    route.AttachTo(dbApp);
                                                }

                                                PAMSilo silo = route.GetRouteTarget()?.TargetACComponent as PAMSilo;
                                                route.Detach();
                                                if (silo != null)
                                                {
                                                    Facility facility = silo.Facility?.ValueT?.ValueT;
                                                    if (facility != null)
                                                    {

                                                        Facility siloFacility = facility.FromAppContext<Facility>(dbApp);
                                                        siloFacility.OutwardEnabled = false;
                                                        Msg msg = dbApp.ACSaveChanges();
                                                        if (msg != null)
                                                        {
                                                            OnNewAlarmOccurred(ProcessAlarm, msg);
                                                            Messages.LogMessageMsg(msg);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                return;
                            }
                        }
                    }
                    else if (taskEntry.State == PointProcessingState.Accepted && taskEntry.InProcess)
                    {
                        if (IsTransport && IsRelocation)
                        {
                            using (var dbIPlus = new Database())
                            using (var dbApp = new DatabaseApp(dbIPlus))
                            {
                                var pwMethod = ParentPWMethod<PWMethodRelocation>();
                                Picking picking = null;
                                if (pwMethod.CurrentPicking != null)
                                {
                                    var routeItem = CurrentDischargingDest(dbIPlus);
                                    double actualWeight = 0.001;
                                    picking = pwMethod.CurrentPicking.FromAppContext<Picking>(dbApp);
                                    PickingPos pickingPos = pwMethod.CurrentPickingPos != null ? pwMethod.CurrentPickingPos.FromAppContext<PickingPos>(dbApp) : null;
                                    if (picking != null)
                                    {
                                        DoInwardBooking(actualWeight, dbApp, routeItem, null, picking, pickingPos, e, true);
                                    }
                                }
                            }
                        }
                        base.TaskCallback(sender, e, wrapObject);
                    }
                }
            }
            finally
            {
                _InCallback = false;
            }

            base.TaskCallback(sender, e, wrapObject);
        }

        public override Msg DoInwardBooking(double actualWeight, DatabaseApp dbApp, RouteItem dischargingDest, Facility facilityDest, Picking picking, PickingPos pickingPos, ACEventArgs e, bool isDischargingEnd)
        {
            Msg msg = base.DoInwardBooking(actualWeight, dbApp, dischargingDest, facilityDest, picking, pickingPos, e, isDischargingEnd);

            if (msg != null && msg.MessageLevel >= eMsgLevel.Failure)
                return msg;

            if (RelocateToFinalDest && pickingPos.Material != null && ACFacilityManager != null)
            {
                ACComponent virtStoreComp = dischargingDest.TargetACComponent as ACComponent;
                if (virtStoreComp != null)
                {
                    PAProcessModule tModule = null;
                    using (Database db = new Database())
                    {
                        ACRoutingParameters routingParameters = new ACRoutingParameters()
                        {
                            RoutingService = this.RoutingService,
                            Database = db,
                            AttachRouteItemsToContext = false,
                            SelectionRuleID = PAMSilo.SelRuleID_Storage,
                            Direction = RouteDirections.Forwards,
                            MaxRouteAlternativesInLoop = 0,
                            IncludeReserved = true,
                            IncludeAllocated = true
                        };

                        RoutingResult rResult = ACRoutingService.FindSuccessors(virtStoreComp.GetACUrl(), routingParameters);

                        tModule = rResult?.Routes?.FirstOrDefault()?.GetRouteTarget()?.TargetACComponent as PAProcessModule;
                        if (tModule != null)
                        {
                            Facility virtDestStore = dbApp.Facility.Where(c => c.VBiFacilityACClassID == tModule.ComponentClass.ACClassID).FirstOrDefault();
                            if (    virtDestStore != null
                                && (!virtDestStore.MaterialID.HasValue || virtDestStore.MaterialID == pickingPos.Material.MaterialID))
                            {
                                foreach (FacilityBookingCharge fbc in dbApp.FacilityBookingCharge
                                                                            .Include(c => c.InwardFacilityCharge)
                                                                            .Where(c => c.PickingPosID == pickingPos.PickingPosID)
                                                                            .ToArray())
                                {
                                    if (fbc.InwardFacilityCharge == null)
                                        continue;
                                    ACMethodBooking relocationPosting = ACFacilityManager.ACUrlACTypeSignature("!" + GlobalApp.FBT_Relocation_FacilityCharge_Facility.ToString(), gip.core.datamodel.Database.GlobalDatabase) as ACMethodBooking;
                                    if (relocationPosting != null)
                                        relocationPosting = relocationPosting.Clone() as ACMethodBooking;
                                    if (relocationPosting != null)
                                    {
                                        relocationPosting.OutwardFacilityCharge = fbc.InwardFacilityCharge;
                                        relocationPosting.InwardFacility = virtDestStore;
                                        relocationPosting.OutwardQuantity = fbc.InwardQuantityUOM;
                                        relocationPosting.InwardQuantity = fbc.InwardQuantityUOM;
                                        ACMethodEventArgs resultBooking = ACFacilityManager.BookFacilityWithRetry(ref relocationPosting, dbApp);
                                        if (resultBooking.ResultState == Global.ACMethodResultState.Failed || resultBooking.ResultState == Global.ACMethodResultState.Notpossible)
                                        {
                                            msg = new Msg(resultBooking.ValidMessage.InnerMessage, this, eMsgLevel.Error, PWClassName, "BookFermentationStarter(20)", 629);
                                            ActivateProcessAlarm(msg, false);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

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

            return msg;
        }

        protected override void DumpPropertyList(XmlDocument doc, XmlElement xmlACPropertyList, ref DumpStats dumpStats)
        {
            base.DumpPropertyList(doc, xmlACPropertyList, ref dumpStats);

            XmlElement xmlChild = xmlACPropertyList["TargetScale"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("TargetScale");
                if (xmlChild != null)
                    xmlChild.InnerText = _TargetScale?.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }
        }

        #endregion

        public static bool HandleExecuteACMethod_PWBakeryPumping(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, object[] acParameter)
        {
            return HandleExecuteACMethod_PWDischarging(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

    }
}
