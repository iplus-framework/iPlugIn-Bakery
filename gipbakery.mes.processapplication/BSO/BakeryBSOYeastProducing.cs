using gip.bso.manufacturing;
using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.datamodel;
using gip.mes.facility;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioManufacturing, "en{'Yeast'}de{'Hefe'}", Global.ACKinds.TACBSO, Global.ACStorableTypes.NotStorable, true, true, SortIndex = 50)]
    public class BakeryBSOYeastProducing : BSOWorkCenterMessages
    {
        #region c'tors

        public BakeryBSOYeastProducing(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACInit(Global.ACStartTypes startChildMode = Global.ACStartTypes.Automatic)
        {
            return base.ACInit(startChildMode);
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            if (FermentationStarterRef != null)
            {
                FermentationStarterRef.Detach();
                FermentationStarterRef = null;
            }

            if (FermentationQuantityProp != null)
            {
                FermentationQuantityProp.PropertyChanged -= FermentationQuantityProp_PropertyChanged;
                FermentationQuantityProp = null;
            }

            if (ProcessModuleOrderInfo != null)
            {
                ProcessModuleOrderInfo.PropertyChanged -= ProcessModuleOrderInfo_PropertyChanged;
                ProcessModuleOrderInfo = null;
            }

            DeActivate();

            _PWFermentationStarterType = null;
            _PAFBakeryYeastProdType = null;
            _PAFDischargingType = null;

            return base.ACDeInit(deleteACClassTask);
        }

        public const string ClassName = "BakeryBSOYeastProducing";

        #endregion

        #region Properties

        protected Type _PWFermentationStarterType = typeof(PWBakeryFermentationStarter);
        private Type _PAFBakeryYeastProdType = typeof(PAFBakeryYeastProducing);
        protected Type _PAFDischargingType = typeof(PAFDischarging);

        #region Properties => VirtualSourceStoreInfo

        public Guid? VirtualSourceStoreID
        {
            get;
            set;
        }

        public Facility VirtualSourceFacility
        {
            get;
            set;
        }

        private FacilityCharge _SelectedSourceFC;
        [ACPropertySelected(9999, "SourceFC")]
        public FacilityCharge SelectedSourceFC
        {
            get => _SelectedSourceFC;
            set
            {
                _SelectedSourceFC = value;
                OnPropertyChanged("SelectedSourceFC");
            }
        }

        public List<FacilityCharge> _SourceFCList;
        [ACPropertyList(9999, "SourceFC")]
        public List<FacilityCharge> SourceFCList
        {
            get => _SourceFCList;
            set
            {
                _SourceFCList = value;
                OnPropertyChanged("SourceFCList");
            }
        }

        #endregion

        private string _ReadyForDosing;
        [ACPropertyInfo(806, "", "en{'Ready for dosing'}de{'Dosierbereitschaft'}")]
        public string ReadyForDosing
        {
            get => _ReadyForDosing;
            set
            {
                _ReadyForDosing = value;
                OnPropertyChanged("ReadyForDosing");
            }
        }

        private string _MixingSpeed;
        [ACPropertyInfo(807, "", "en{'Mixing speed'}de{'Drehzahl'}")]
        public string MixingSpeed
        {
            get => _MixingSpeed;
            set
            {
                _MixingSpeed = value;
                OnPropertyChanged("MixingSpeed");
            }
        }

        public ACValue _SelectedPumpTarget;
        [ACPropertySelected(850, "PumpTargets")]
        public ACValue SelectedPumpTarget
        {
            get => _SelectedPumpTarget;
            set
            {
                _SelectedPumpTarget = value;
                OnPropertyChanged("SelectedPumpTarget");
            }
        }

        private ACValueList _PumpTargets;
        [ACPropertyList(850, "PumpTargets")]
        public ACValueList PumpTargets
        {
            get => _PumpTargets;
            set
            {
                _PumpTargets = value;
                OnPropertyChanged("PumpTargets");
            }
        }

        private double _PumpOverTargetQuantity;
        [ACPropertyInfo(852, "", "en{'Target quantity'}de{'Sollmenge'}")]
        public double PumpOverTargetQuantity
        {
            get => _PumpOverTargetQuantity;
            set
            {
                _PumpOverTargetQuantity = value;
                OnPropertyChanged("PumpOverTargetQuantity");
            }
        }

        private ACRef<ACComponent> _PAFYeastProducing;

        public virtual ACComponent PAFPreProducing
        {
            get => _PAFYeastProducing?.ValueT;
        }

        private ACRef<ACComponent> _PreProdScale
        {
            get;
            set;
        }

        [ACPropertyInfo(807)]
        public ACComponent PreProdScale
        {
            get => _PreProdScale?.ValueT;
            set
            {
                _PreProdScale = new ACRef<ACComponent>(value, this);
                OnPropertyChanged("PreProdScale");
            }
        }

        private ACRef<ACComponent> _VirtualStore
        {
            get;
            set;
        }

        [ACPropertyInfo(808)]
        public ACComponent VirtualStore
        {
            get => _VirtualStore?.ValueT;
            set
            {
                _VirtualStore = new ACRef<ACComponent>(value, this);
                OnPropertyChanged("VirtualStore");
            }
        }

        private ACRef<ACComponent> _PumpOverProcessModule;
        [ACPropertyInfo(809)]
        public ACComponent PumpOverProcessModule
        {
            get => _PumpOverProcessModule?.ValueT;
            set
            {
                _PumpOverProcessModule = new ACRef<ACComponent>(value, this);
                OnPropertyChanged("PumpOverProcessModule");
            }
        }


        public IACContainerTNet<bool> RefreshParkingSpace;

        public IACContainerTNet<string> ProcessModuleOrderInfo;

        protected ACRef<IACComponentPWGroup> PWGroupFermentation
        {
            get;
            set;
        }

        protected ACRef<IACComponentPWNode> FermentationStarterRef
        {
            get;
            set;
        }

        protected IACContainerTNet<double?> FermentationQuantityProp
        {
            get;
            set;
        }

        protected IACContainerTNet<ACStateEnum> DischargingACStateProp
        {
            get;
            set;
        }

        private bool _IsDischargingActive;
        public bool IsDischargingActive
        {
            get => _IsDischargingActive;
            set
            {
                _IsDischargingActive = value;
                OnPropertyChanged("IsDischargingActive");
            }
        }

        #region Properties => Clean

        [ACPropertySelected(860, "CleanItem")]
        public BakeryCleanInfoItem SelectedCleanItem
        {
            get;
            set;
        }

        [ACPropertyList(860, "CleanItem")]
        public IEnumerable<BakeryCleanInfoItem> CleanItemsList
        {
            get;
            set;
        }

        [ACPropertyInfo(860, "", "en{'Target quantity'}de{'Sollmenge'}")]
        public double CleanTargetQuantity
        {
            get;
            set;
        }

        #endregion

        #endregion

        #region Methods

        public override void Activate(ACComponent selectedProcessModule)
        {
            base.Activate(selectedProcessModule);
            InitBSO(selectedProcessModule);
        }

        public override void DeActivate()
        {
            Deactivate();

            if (_PreProdScale != null)
            {
                _PreProdScale.Detach();
                _PreProdScale = null;
            }

            if (_VirtualStore != null)
            {
                _VirtualStore.Detach();
                _VirtualStore = null;
            }

            if (_PAFYeastProducing != null)
            {
                _PAFYeastProducing.Detach();
                _PAFYeastProducing = null;
            }

            if (DischargingACStateProp != null)
            {
                DischargingACStateProp.PropertyChanged -= DischargingACStateProp_PropertyChanged;
                DischargingACStateProp = null;
            }

            if (ProcessModuleOrderInfo != null)
            {
                ProcessModuleOrderInfo.PropertyChanged -= ProcessModuleOrderInfo_PropertyChanged;
                ProcessModuleOrderInfo = null;
            }

            if (RefreshParkingSpace != null)
            {
                RefreshParkingSpace.PropertyChanged -= RefreshParkingSpace_PropertyChanged;
                RefreshParkingSpace = null;
            }

            if (_RoutingService != null)
            {
                _RoutingService.Detach();
                _RoutingService = null;
            }

            if (_ACFacilityManager != null)
            {
                _ACFacilityManager.Detach();
                _ACFacilityManager = null;
            }

            if (_ACPickingManager != null)
            {
                _ACPickingManager.Detach();
                _ACPickingManager = null;
            }

            base.DeActivate();
        }

        protected virtual void Deactivate()
        {
            if (FermentationStarterRef != null)
            {
                FermentationStarterRef.Detach();
                FermentationStarterRef = null;
            }

            if (FermentationQuantityProp != null)
            {
                FermentationQuantityProp.PropertyChanged -= FermentationQuantityProp_PropertyChanged;
                FermentationQuantityProp = null;
            }

            if (PWGroupFermentation != null)
            {
                PWGroupFermentation.Detach();
                PWGroupFermentation = null;
            }

            _MessagesListSafe.Clear();
            RefreshMessageList();
        }

        protected virtual void InitBSO(ACComponent processModule)
        {
            if (ProcessModuleOrderInfo != null)
                ProcessModuleOrderInfo.PropertyChanged -= ProcessModuleOrderInfo_PropertyChanged;

            ProcessModuleOrderInfo = null;

            var childInstances = processModule.GetChildInstanceInfo(1, false);
            if (childInstances == null || !childInstances.Any())
                return;

            InitPreProdFunction(processModule, childInstances);

            if (PAFPreProducing != null)
            {
                gip.core.datamodel.ACClass funcClass = null;
                
                using(ACMonitor.Lock(gip.core.datamodel.Database.GlobalDatabase.QueryLock_1X000))
                    funcClass = PAFPreProducing?.ComponentClass?.FromIPlusContext<gip.core.datamodel.ACClass>(DatabaseApp.ContextIPlus);

                if (funcClass == null)
                {
                    Messages.Error(this, "The component class of pre production function can not be found!");
                    return;
                }

                string scaleACUrl = GetConfigValue(funcClass, PAFBakeryYeastProducing.PN_FermentationStarterScaleACUrl) as string;

                ACComponent scale = PAFPreProducing?.ACUrlCommand(scaleACUrl) as ACComponent;
                if (scale != null)
                {
                    PreProdScale = scale;
                }
                else
                {
                    //error
                }

                string storeACUrl = PAFPreProducing.ExecuteMethod(PAFBakeryYeastProducing.MN_GetVirtualStoreACUrl) as string;
                ACComponent store = PAFPreProducing?.ACUrlCommand(storeACUrl) as ACComponent;
                if (store != null)
                {
                    VirtualStore = store;
                }
                else
                {
                    //error
                }

                string pumpOverModuleACUrl = GetConfigValue(funcClass, PAFBakeryYeastProducing.PN_PumpOverProcessModuleACUrl) as string;
                if (!string.IsNullOrEmpty(pumpOverModuleACUrl))
                    PumpOverProcessModule = Root.ACUrlCommand(pumpOverModuleACUrl) as ACComponent;
            }

            ACChildInstanceInfo dischFunc = childInstances.FirstOrDefault(c => _PAFDischargingType.IsAssignableFrom(c.ACType.ValueT.ObjectType));
            if (dischFunc != null)
            {
                ACComponent funcComp = processModule.ACUrlCommand(dischFunc.ACIdentifier) as ACComponent;
                if (funcComp == null)
                    return;

                DischargingACStateProp = funcComp?.GetPropertyNet(Const.ACState) as IACContainerTNet<ACStateEnum>;
                if (DischargingACStateProp != null)
                {
                    DischargingACStateProp.PropertyChanged += DischargingACStateProp_PropertyChanged;
                    HandleDischargingACState();
                }
                else
                {
                    //error
                }
            }

            ProcessModuleOrderInfo = processModule.GetPropertyNet("OrderInfo") as IACContainerTNet<string>;
            if (ProcessModuleOrderInfo == null)
            {
                //error
                return;
            }

            ProcessModuleOrderInfo.PropertyChanged += ProcessModuleOrderInfo_PropertyChanged;
            string orderInfo = ProcessModuleOrderInfo.ValueT;
            ParentBSOWCS.ApplicationQueue.Add(() => HandleOrderInfoPropChanged(orderInfo));

            VirtualSourceStoreID = PAFPreProducing?.ExecuteMethod(PAFBakeryYeastProducing.MN_GetSourceVirtualStoreID) as Guid?;

            if (VirtualSourceStoreID.HasValue)
            {
                VirtualSourceFacility = DatabaseApp.Facility.FirstOrDefault(c => c.VBiFacilityACClassID == VirtualSourceStoreID);

                if (VirtualSourceFacility == null)
                {
                    //TODO:error
                    return;
                }

                RefreshVirtualSourceStore();

                gip.core.datamodel.ACClass module = VirtualSourceFacility.GetFacilityACClass(DatabaseApp.ContextIPlus);

                if (module != null)
                {
                    var component = Root.ACUrlCommand(module.GetACUrlComponent()) as ACComponent;
                    if (component != null)
                    {
                        RefreshParkingSpace = component.GetPropertyNet("RefreshParkingSpace") as IACContainerTNet<bool>;
                        if (RefreshParkingSpace != null)
                        {
                            RefreshParkingSpace.PropertyChanged += RefreshParkingSpace_PropertyChanged;
                        }
                    }
                }
            }
        }

        public virtual void InitPreProdFunction(ACComponent processModule, IEnumerable<ACChildInstanceInfo> childInstances)
        {
            ACChildInstanceInfo func = childInstances.FirstOrDefault(c => _PAFBakeryYeastProdType.IsAssignableFrom(c.ACType.ValueT.ObjectType));
            if (func != null)
            {
                ACComponent funcComp = processModule.ACUrlCommand(func.ACIdentifier) as ACComponent;
                if (funcComp == null)
                    return;

                _PAFYeastProducing = new ACRef<ACComponent>(funcComp, this);
            }
        }

        protected object GetConfigValue(gip.core.datamodel.ACClass acClass, string configName)
        {
            if (acClass == null || string.IsNullOrEmpty(configName))
                return null;

            var config = acClass.ACClassConfig_ACClass.FirstOrDefault(c => c.ConfigACUrl == configName);
            if (config == null)
                return null;

            return config.Value;
        }

        protected void RefreshParkingSpace_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                ParentBSOWCS.ApplicationQueue.Add(() => RefreshVirtualSourceStore());
            }
        }

        protected void RefreshVirtualSourceStore()
        {
            if (VirtualSourceFacility == null)
                return;

            VirtualSourceFacility.FacilityCharge_Facility.AutoLoad();
            VirtualSourceFacility.FacilityCharge_Facility.AutoRefresh();
            SourceFCList = VirtualSourceFacility.FacilityCharge_Facility.Where(c => !c.NotAvailable).ToList();
        }

        protected void DischargingACStateProp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                HandleDischargingACState();
            }
        }

        protected virtual void HandleDischargingACState()
        {
            if (DischargingACStateProp.ValueT == ACStateEnum.SMRunning)
            {
                IsDischargingActive = true;
            }
            else
            {
                IsDischargingActive = false;
            }
        }

        protected void ProcessModuleOrderInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                string orderInfo = ProcessModuleOrderInfo.ValueT;
                ParentBSOWCS.ApplicationQueue.Add(() => HandleOrderInfoPropChanged(orderInfo));
            }
        }

        protected void HandleOrderInfoPropChanged(string orderInfo)
        {
            if (string.IsNullOrEmpty(orderInfo))
            {
                Deactivate();
            }
            else
            {
                string[] accessArr = (string[])ParentBSOWCS.CurrentProcessModule?.ACUrlCommand("!SemaphoreAccessedFrom");
                if (accessArr == null || !accessArr.Any())
                {
                    Deactivate();
                    return;
                }

                string pwGroupACUrl = accessArr[0];
                IACComponentPWGroup pwGroup = Root.ACUrlCommand(pwGroupACUrl) as IACComponentPWGroup;

                if (pwGroup == null)
                {
                    //todo: error
                    return;
                }

                PWGroupFermentation = new ACRef<IACComponentPWGroup>(pwGroup, this);

                IEnumerable<ACChildInstanceInfo> pwNodes = pwGroup.GetChildInstanceInfo(1, new ChildInstanceInfoSearchParam() { OnlyWorkflows = true });

                if (pwNodes == null || !pwNodes.Any())
                    return;

                ACChildInstanceInfo fermentationStarter = pwNodes.FirstOrDefault(c => _PWFermentationStarterType.IsAssignableFrom(c.ACType.ValueT.ObjectType));

                if (fermentationStarter != null)
                {
                    IACComponentPWNode pwNode = pwGroup.ACUrlCommand(fermentationStarter.ACUrlParent + "\\" + fermentationStarter.ACIdentifier) as IACComponentPWNode;
                    if (pwNode == null)
                    {
                        //Error50290: The user does not have access rights for class PWManualWeighing ({0}).
                        // Der Benutzer hat keine Zugriffsrechte auf Klasse PWManualWeighing ({0}).
                        Messages.Error(this, "Error50290", false, fermentationStarter.ACUrlParent + "\\" + fermentationStarter.ACIdentifier);
                        return;
                    }

                    FermentationStarterRef = new ACRef<IACComponentPWNode>(pwNode, this);
                    FermentationQuantityProp = FermentationStarterRef.ValueT.GetPropertyNet(PWBakeryFermentationStarter.PN_FSTargetQuantity) as IACContainerTNet<double?>;

                    if (FermentationQuantityProp == null)
                    {
                        //TODO
                        return;
                    }
                }

                if (FermentationQuantityProp != null)
                {
                    FermentationQuantityProp.PropertyChanged += FermentationQuantityProp_PropertyChanged;
                    HandleFermentationQunatity();
                }

                OnHandleOrderInfoPropChanged();
                
            }
        }

        public virtual void OnHandleOrderInfoPropChanged()
        {

        }

        protected void FermentationQuantityProp_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                ParentBSOWCS.ApplicationQueue.Add(() => HandleFermentationQunatity());
            }
        }

        protected void HandleFermentationQunatity()
        {
            if (FermentationQuantityProp.ValueT != null)
            {
                MessageItem msgItem = _MessagesListSafe.FirstOrDefault(c => c.UserAckPWNode != null && c.UserAckPWNode.ValueT == FermentationStarterRef.ValueT);

                if (msgItem == null)
                {
                    string message = Root.Environment.TranslateText(this, "msgFermentationStarter", Math.Round(FermentationQuantityProp.ValueT.Value,2));
                    msgItem = new MessageItem(FermentationStarterRef.ValueT, this);
                    msgItem.Message = message;
                    AddToMessageList(msgItem);
                    RefreshMessageList();
                }
            }
            else
            {
                MessageItem msgItem = _MessagesListSafe.FirstOrDefault(c => c.UserAckPWNode != null && c.UserAckPWNode.ValueT == FermentationStarterRef.ValueT);
                if (msgItem != null)
                {
                    RemoveFromMessageList(msgItem);
                    RefreshMessageList();
                }
            }
        }

        [ACMethodInfo("", "en{'Acknowledge - Start'}de{'Quittieren - Start'}", 800, true)]
        public virtual void Acknowledge()
        {
            if (FermentationStarterRef != null)
            {
                MessageItem msgItem = MessagesList.FirstOrDefault(c => c.UserAckPWNode != null && c.UserAckPWNode.ValueT == FermentationStarterRef.ValueT);
                if (msgItem != null)
                {
                    bool? result = FermentationStarterRef.ValueT.ExecuteMethod(PWBakeryFermentationStarter.MN_AckFermentationStarter, false) as bool?;
                    if (result.HasValue && !result.Value)
                    {
                        if (Messages.Question(this, "Question50063") == Global.MsgResult.Yes)
                        {
                            FermentationStarterRef.ValueT.ExecuteMethod(PWBakeryFermentationStarter.MN_AckFermentationStarter, true);
                        }
                    }
                }
            }
        }

        public virtual bool IsEnabledAcknowledge()
        {
            return true;
        }

        [ACMethodInfo("", "en{'Clean'}de{'Reinigen'}", 801, true)]
        public void Clean()
        {
            RoutingResult rResult = ACRoutingService.FindSuccessors(RoutingService, DatabaseApp.ContextIPlus, true, ParentBSOWCS.CurrentProcessModule.ComponentClass, 
                                                                    BakeryReceivingPoint.SelRuleID_RecvPoint, RouteDirections.Forwards, null, null, null, 0, true, false);

            IEnumerable<IACComponent> possbileDestinations = rResult.Routes.SelectMany(c => c.GetRouteTargets()).Select(x => x.TargetACComponent);

            List<BakeryCleanInfoItem> items = new List<BakeryCleanInfoItem>();

            BakeryCleanInfoItem drain = new BakeryCleanInfoItem();
            drain.ACCaption = Root.Environment.TranslateText(this, "txtDrainItem");
            drain.RouteItemID = 0;
            items.Add(drain);

            foreach (IACComponent possibleDest in possbileDestinations)
            {
                string routeItemID = possibleDest.ACUrlCommand("RouteItemID") as string;

                int routeItemAsNum = -1;

                if (int.TryParse(routeItemID, out routeItemAsNum))
                {
                    BakeryCleanInfoItem cleanItem = new BakeryCleanInfoItem();
                    cleanItem.ACCaption = possibleDest.ACCaption;
                    cleanItem.RouteItemID = routeItemAsNum;
                    items.Add(cleanItem);
                }
            }

            CleanItemsList = items;

            ShowDialog(this, "CleaningDialog");
        }

        public bool IsEnabledClean()
        {
            return ProcessModuleOrderInfo?.ValueT == null && PAFPreProducing != null;
        }

        [ACMethodInfo("", "en{'Start clean'}de{'Reinigen starten'}", 801, true)]
        public void StartClean()
        {
            gip.core.datamodel.ACClass pafClass = PAFPreProducing?.ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(DatabaseApp.ContextIPlus);
            if (pafClass == null)
            {
                //error
                return;
            }

            var config = pafClass.ACClassConfig_ACClass.FirstOrDefault(c => c.ConfigACUrl == PAFBakeryYeastProducing.PN_CleaningMode);
            if (config == null)
            {
                //error
                return;
            }

            BakeryPreProdCleaningMode? mode = config.Value as BakeryPreProdCleaningMode?;
            if (mode == null)
            {
                //Error
                return;
            }

            if (mode.Value == BakeryPreProdCleaningMode.OverBits)
            {
                Msg resultMsg = PAFPreProducing.ExecuteMethod(PAFBakeryYeastProducing.MN_Clean, (short)11) as Msg;
                if (resultMsg != null)
                {
                    Messages.Msg(resultMsg);
                }
            }
            else
            {
                bool managers = CheckAndInitManagers();
                if (!managers)
                    return;

                ClearBookingData();

                if (_VirtualStore == null || _VirtualStore.ValueT == null)
                    return;

                var outwardFacilityRef = _VirtualStore.ValueT.GetPropertyNet("Facility") as IACContainerTNet<ACRef<Facility>>;

                if (outwardFacilityRef == null)
                {
                    //TODO: error
                }

                Facility outFacility = outwardFacilityRef.ValueT?.ValueT;

                if (outFacility == null)
                {
                    //TODO: error
                    return;
                }

                Facility outwardFacility = outFacility.FromAppContext<Facility>(DatabaseApp);

                bool outFacilityOutwardEnabled = outFacility.OutwardEnabled;

                outwardFacility.OutwardEnabled = true;

                CurrentBookParamRelocation.InwardFacility = outwardFacility;
                CurrentBookParamRelocation.OutwardFacility = outwardFacility;
                CurrentBookParamRelocation.InwardQuantity = 0.0001;
                CurrentBookParamRelocation.OutwardQuantity = 0.0001;

                config = pafClass.ACClassConfig_ACClass.FirstOrDefault(c => c.ConfigACUrl == PAFBakeryYeastProducing.PN_CleaningProdACClassWF);
                if (config == null)
                {
                    //error
                    return;
                }

                string configValue = config.Value as string;

                var parts = configValue.Split(';');
                string wfIdentifier = parts.FirstOrDefault().Trim();
                string acUrl = parts.LastOrDefault().Trim();

                var wfClass = DatabaseApp.ContextIPlus.ACClassWF.Where(c => c.ACClassMethod != null && c.ACClassMethod.ACIdentifier == wfIdentifier).ToArray().FirstOrDefault(c => c.ConfigACUrl == acUrl);
                var wfMethod = wfClass?.ACClassMethod;

                RunWorkflow(wfClass, wfMethod);

                outFacility.OutwardEnabled = outFacilityOutwardEnabled;
                CloseTopDialog();
            }
        }

        public bool IsEnabledStartClean()
        {
            return SelectedCleanItem != null;
        }

        [ACMethodInfo("", "", 802, true)]
        public void StoreOutwardEnabledOn()
        {
            if (IsEnabledStoreOutwardEnabledOn())
            {
                PAFPreProducing?.ExecuteMethod(PAFBakeryYeastProducing.MN_SwitchVirtualStoreOutwardEnabled);

                if (!IsDischargingActive)
                {
                    if (Messages.Question(this, "Question50066") == Global.MsgResult.Yes)
                    {
                        bool managers = CheckAndInitManagers();
                        if (!managers)
                            return;

                        ClearBookingData();

                        if (_VirtualStore == null || _VirtualStore.ValueT == null)
                            return;

                        var outwardFacilityRef = _VirtualStore.ValueT.GetPropertyNet("Facility") as IACContainerTNet<ACRef<Facility>>;

                        if (outwardFacilityRef == null)
                        {
                            //TODO: error
                        }

                        Facility outFacility = outwardFacilityRef.ValueT?.ValueT;

                        if (outFacility == null)
                        {
                            //TODO: error
                            return;
                        }

                        try
                        {
                            Facility outwardFacility = outFacility.FromAppContext<Facility>(DatabaseApp);
                            outFacility.AutoRefresh();

                            outwardFacility.OutwardEnabled = outFacility.OutwardEnabled;

                            CurrentBookParamRelocation.InwardFacility = outwardFacility;
                            CurrentBookParamRelocation.OutwardFacility = outwardFacility;
                            CurrentBookParamRelocation.InwardQuantity = 0.0001;
                            CurrentBookParamRelocation.OutwardQuantity = 0.0001;

                            gip.core.datamodel.ACClass compClass = PAFPreProducing?.ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(DatabaseApp.ContextIPlus);

                            var config = compClass?.ACClassConfig_ACClass.FirstOrDefault(c => c.ConfigACUrl == "ContinueProdACClassWF");
                            if (config == null)
                                return;

                            string configValue = config.Value as string;

                            var parts = configValue.Split(';');
                            string wfIdentifier = parts.FirstOrDefault().Trim();
                            string acUrl = parts.LastOrDefault().Trim();

                            var wfClass = DatabaseApp.ContextIPlus.ACClassWF.Where(c => c.ACClassMethod != null && c.ACClassMethod.ACIdentifier == wfIdentifier).ToArray().FirstOrDefault(c => c.ConfigACUrl == acUrl);
                            var wfMethod = wfClass?.ACClassMethod;

                            RunWorkflow(wfClass, wfMethod);
                        }
                        catch (Exception e)
                        {
                            Messages.LogException(this.GetACUrl(), "StoreOutwardEnabledOn(10)", e);
                        }
                    }
                }
            }
        }

        public bool IsEnabledStoreOutwardEnabledOn()
        {
            return PAFPreProducing != null;
        }

        [ACMethodInfo("", "", 802, true)]
        public void StoreOutwardEnabledOff()
        {
            if (IsEnabledStoreOutwardEnabledOff())
            {
                if (IsDischargingActive)
                {
                    //The sourdough is ready for dosing. Do you want to finish the sourdough order? 
                    //Pressing the "No" key only removes the dosing availability and locks the storage container.

                    var questionResult = Messages.Question(this, "Question50067");
                    if (questionResult == Global.MsgResult.Yes && DischargingACStateProp != null)
                    {
                        DischargingACStateProp.ValueT = ACStateEnum.SMCompleted;
                    }
                    else if (questionResult == Global.MsgResult.Cancel)
                    {
                        return;
                    }
                }

                PAFPreProducing?.ExecuteMethod(PAFBakeryYeastProducing.MN_SwitchVirtualStoreOutwardEnabled);
            }
        }

        public bool IsEnabledStoreOutwardEnabledOff()
        {
            return PAFPreProducing != null;
        }

        [ACMethodInfo("", "en{'Pump over'}de{'Umpumpen'}", 802, true)]
        public void PumpOver()
        {
            ACValueList targets = PAFPreProducing?.ExecuteMethod(PAFBakeryYeastProducing.MN_GetPumpOverTargets) as ACValueList;

            if (VirtualSourceFacility != null && targets != null)
            {
                ACValue pTarget = targets.FirstOrDefault(c => c.ParamAsGuid == VirtualSourceFacility.FacilityID);
                if (pTarget != null)
                    targets.Remove(pTarget);
            }

            PumpTargets = targets;

            if (PumpTargets == null || !PumpTargets.Any())
            {
                //TODO: error
                return;
            }



            CheckAndInitManagers();

            ClearBookingData();
            ShowDialog(this, "PumpOverDialog");
        }

        public bool IsEnabledPumpOver()
        {
            return PAFPreProducing != null;
        }

        [ACMethodInfo("", "en{'Pump over'}de{'Umpumpen'}", 803, true)]
        public void PumpOverStart()
        {
            if (_VirtualStore == null || _VirtualStore.ValueT == null)
                return;

            var outwardFacilityRef = _VirtualStore.ValueT.GetPropertyNet("Facility") as IACContainerTNet<ACRef<Facility>>;

            if (outwardFacilityRef == null)
            {
                //TODO: error
            }

            Facility outFacility = outwardFacilityRef.ValueT?.ValueT;

            if (outFacility == null)
            {
                //TODO: error
                return;
            }

            Facility outwardFacility = outFacility.FromAppContext<Facility>(DatabaseApp);

            Guid? inwardFacilityID = SelectedPumpTarget.Value as Guid?;

            if (!inwardFacilityID.HasValue)
            {
                //TODO: error
                return;
            }

            Facility inwardFacility = DatabaseApp.Facility.FirstOrDefault(c => c.FacilityID == inwardFacilityID.Value);
            if (inwardFacility == null)
            {
                //error
                return;
            }

            CurrentBookParamRelocation.InwardFacility = inwardFacility;
            CurrentBookParamRelocation.OutwardFacility = outwardFacility;
            CurrentBookParamRelocation.InwardQuantity = PumpOverTargetQuantity;
            CurrentBookParamRelocation.OutwardQuantity = PumpOverTargetQuantity;

            //todo: lock or from another context
            var config = PAFPreProducing?.ComponentClass.ACClassConfig_ACClass.FirstOrDefault(c => c.ConfigACUrl == "PumpOverACClassWF");
            if (config == null)
                return;

            string configValue = config.Value as string;

            var parts = configValue.Split(';');
            string wfIdentifier = parts.FirstOrDefault().Trim();
            string acUrl = parts.LastOrDefault().Trim();

            var wfClass = DatabaseApp.ContextIPlus.ACClassWF.Where(c => c.ACClassMethod != null && c.ACClassMethod.ACIdentifier == wfIdentifier).ToArray().FirstOrDefault(c => c.ConfigACUrl == acUrl);
            var wfMethod = wfClass?.ACClassMethod;

            RunWorkflow(wfClass, wfMethod);

            PumpOverTargetQuantity = 0;
            SelectedPumpTarget = null;

            CloseTopDialog();
        }

        public bool IsEnabledPumpOverStart()
        {
            return PAFPreProducing != null && SelectedPumpTarget != null && PumpOverTargetQuantity > 0;
        }

        public bool CheckAndInitManagers()
        {
            if (_ACFacilityManager == null)
            {
                _ACFacilityManager = FacilityManager.ACRefToServiceInstance(this);
                if (_ACFacilityManager == null)
                {
                    //Error50432: The facility manager is null.
                    Messages.Error(this, "Error50432");
                    return false;
                }
            }

            if (_ACPickingManager == null)
            {
                _ACPickingManager = ACRefToPickingManager();
            }

            return true;
        }

        public override bool OnPreStartWorkflow(Picking picking, List<SingleDosingConfigItem> configItems, Route validRoute, gip.core.datamodel.ACClassWF rootWF)
        {
            gip.core.datamodel.ACClassWF cleaning = configItems?.FirstOrDefault()?.PWGroup?.ACClassWF_ParentACClassWF
                                                                .FirstOrDefault(c => c.PWACClass.ACIdentifier.Contains(PWBakeryCleaning.PWClassName));

            if (cleaning == null)
            {
                //TODO: translation
                Messages.Error(this, "Can not find the PWBakeryCleaning ACClassWF");
                return false;
            }

            ACMethod acMethod = cleaning.RefPAACClassMethod.ACMethod;

            string preConfigACUrl = rootWF.ConfigACUrl + "\\";
            string configACUrl = string.Format("{0}\\{1}\\CleaningTarget", cleaning.ConfigACUrl, acMethod.ACIdentifier);

            IACConfig targetConfig = picking.ConfigurationEntries.FirstOrDefault(c => c.PreConfigACUrl == preConfigACUrl && c.LocalConfigACUrl == configACUrl);
            
            if (targetConfig == null)
            {
                ACConfigParam param = new ACConfigParam()
                {
                    ACIdentifier = "CleaningTarget",
                    ACCaption = acMethod.GetACCaptionForACIdentifier("CleaningTarget"),
                    ValueTypeACClassID = DatabaseApp.ContextIPlus.GetACType("Int32").ACClassID,
                    ACClassWF = cleaning
                };

                targetConfig = ConfigManagerIPlus.ACConfigFactory(picking, param, preConfigACUrl, configACUrl, null);
                param.ConfigurationList.Insert(0, targetConfig);

                picking.ConfigurationEntries.Append(targetConfig);
            }
            targetConfig.Value = SelectedCleanItem.RouteItemID;

            if (CleanTargetQuantity > 0)
            {
                configACUrl = string.Format("{0}\\{1}\\TargetQuantity", cleaning.ConfigACUrl, acMethod.ACIdentifier);
                targetConfig = picking.ConfigurationEntries.FirstOrDefault(c => c.PreConfigACUrl == preConfigACUrl && c.LocalConfigACUrl == configACUrl);

                if (targetConfig == null)
                {
                    ACConfigParam param = new ACConfigParam()
                    {
                        ACIdentifier = "TargetQuantity",
                        ACCaption = acMethod.GetACCaptionForACIdentifier("TargetQuantity"),
                        ValueTypeACClassID = DatabaseApp.ContextIPlus.GetACType("Double").ACClassID,
                        ACClassWF = cleaning
                    };

                    targetConfig = ConfigManagerIPlus.ACConfigFactory(picking, param, preConfigACUrl, configACUrl, null);
                    param.ConfigurationList.Insert(0, targetConfig);

                    picking.ConfigurationEntries.Append(targetConfig);
                }
                targetConfig.Value = CleanTargetQuantity;
            }


            Msg msg = DatabaseApp.ACSaveChanges();
            if (msg != null)
            {
                Messages.Msg(msg);
                return false;
            }

            return true;
        }

        public override Global.ControlModes OnGetControlModes(IVBContent vbControl)
        {

            return base.OnGetControlModes(vbControl);
        }

        #endregion
    }
}

