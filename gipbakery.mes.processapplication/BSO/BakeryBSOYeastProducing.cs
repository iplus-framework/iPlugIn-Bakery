﻿using gip.bso.manufacturing;
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
            //if (FermentationStarterRef != null)
            //{
            //    FermentationStarterRef.Detach();
            //    FermentationStarterRef = null;
            //}

            //if (FermentationQuantityProp != null)
            //{
            //    FermentationQuantityProp.PropertyChanged -= FermentationQuantityProp_PropertyChanged;
            //    FermentationQuantityProp = null;
            //}

            //if (ProcessModuleOrderInfo != null)
            //{
            //    ProcessModuleOrderInfo.PropertyChanged -= ProcessModuleOrderInfo_PropertyChanged;
            //    ProcessModuleOrderInfo = null;
            //}

            DeActivate();

            SelectedSourceFC = null;
            SourceFCList = null;

            _PWFermentationStarterType = null;
            _PAFBakeryYeastProdType = null;
            _PAFDischargingType = null;

            _BookParamNotAvailable = null;
            _BookParamNotAvailableClone = null;

            return base.ACDeInit(deleteACClassTask);
        }

        public const string ClassName = "BakeryBSOYeastProducing";

        #endregion

        #region Properties

        protected Type _PWFermentationStarterType = typeof(PWBakeryFermentationStarter);
        private Type _PAFBakeryYeastProdType = typeof(PAFBakeryYeastProducing);
        protected Type _PAFDischargingType = typeof(PAFDischarging);

        protected ACMonitorObject _70100_MembersLock = new ACMonitorObject(70100);

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

        ACMethodBooking _BookParamNotAvailableClone = null;
        ACMethodBooking _BookParamNotAvailableSiloClone = null;

        ACMethodBooking _BookParamNotAvailable;
        public ACMethodBooking CurrentBookParamNotAvailable
        {
            get
            {
                return _BookParamNotAvailable;
            }
            protected set
            {
                _BookParamNotAvailable = value;
                OnPropertyChanged("CurrentBookParamNotAvailable");
            }
        }

        ACMethodBooking _BookParamNotAvailableSilo;
        public ACMethodBooking CurrentBookParamNotAvailableSilo
        {
            get
            {
                return _BookParamNotAvailableSilo;
            }
            protected set
            {
                _BookParamNotAvailableSilo = value;
                OnPropertyChanged("CurrentBookParamNotAvailableSilo");
            }
        }

        #endregion

        private ACComponent _CurrentProcessModule;
        public override ACComponent CurrentProcessModule
        {
            get
            {
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    return _CurrentProcessModule;
                }
            }
            protected set
            {
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    _CurrentProcessModule = value;
                }
            }
        }

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

        private double _PreProdTemperature;
        [ACPropertyInfo(808, "", "en{'Temperature'}de{'Temperatur'}")]
        public double PreProdTemperature
        {
            get
            {
                return _PreProdTemperature;
            }
            set
            {
                _PreProdTemperature = value;
                OnPropertyChanged("PreProdTemperature");
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

        private ACRef<ACComponent> _PreProdScale;

        [ACPropertyInfo(807)]
        public ACComponent PreProdScale
        {
            get => _PreProdScale?.ValueT;
            private set
            {
                _PreProdScale = new ACRef<ACComponent>(value, this);
                OnPropertyChanged("PreProdScale");
            }
        }

        private IACContainerTNet<double> _PreProdScaleActValue;

        private double _PreProdScaleActualValue;
        [ACPropertyInfo(809, "", "")]
        public double PreProdScaleActualValue
        {
            get => _PreProdScaleActualValue;
            set
            {
                _PreProdScaleActualValue = value;
                OnPropertyChanged("PreProdScaleActualValue");
            }
        }

        private ACRef<ACComponent> _VirtualStore;

        [ACPropertyInfo(808)]
        public ACComponent VirtualStore
        {
            get => _VirtualStore?.ValueT;
            private set
            {
                _VirtualStore = new ACRef<ACComponent>(value, this);
                OnPropertyChanged("VirtualStore");
            }
        }

        private IACContainerTNet<bool> _VirtStoreOutEnabled;

        private bool _VirutalStoreOutwardEnabled;
        public bool VirutalStoreOutwardEnabled
        {
            get => _VirutalStoreOutwardEnabled;
            private set
            {
                _VirutalStoreOutwardEnabled = value;
                OnPropertyChanged("VirutalStoreOutwardEnabled");
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

        private IACContainerTNet<double> _TempSensorActualValue;

        private ACRef<ACComponent> _VirtualSourceParkingSpace;

        public IACContainerTNet<int> RefreshParkingSpace;

        public IACContainerTNet<string> ProcessModuleOrderInfo
        {
            get;
            protected set;
        }
        private bool _IsOrderInfoEmpy = true;

        protected ACRef<IACComponentPWNode> PWGroupFermentation
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

        private short _DischargingState;
        public short DischargingState
        {
            get => _DischargingState;
            set
            {
                _DischargingState = value;
                OnPropertyChanged("DischargingState");
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

        #region Methods => Activation/Deactivation

        public override void Activate(ACComponent selectedProcessModule)
        {
            base.Activate(selectedProcessModule);
            InitBSO(selectedProcessModule);
        }

        public override void DeActivate()
        {
            Deactivate();

            if (_PreProdScaleActValue != null)
            {
                _PreProdScaleActValue.PropertyChanged -= _PreProdScaleActValue_PropertyChanged;
                _PreProdScaleActValue = null;
            }

            if (_PreProdScale != null)
            {
                _PreProdScale.Detach();
                _PreProdScale = null;
            }

            if (_VirtStoreOutEnabled != null)
            {
                _VirtStoreOutEnabled.PropertyChanged -= _VirtStoreOutEnabled_PropertyChanged;
                _VirtStoreOutEnabled = null;
            }

            if (_VirtualStore != null)
            {
                _VirtualStore.Detach();
                _VirtualStore = null;
            }

            if (_VirtualSourceParkingSpace != null)
            {
                _VirtualSourceParkingSpace.Detach();
                _VirtualSourceParkingSpace = null;
            }

            if (_PumpOverProcessModule != null)
            {
                _PumpOverProcessModule.Detach();
                _PumpOverProcessModule = null;
            }

            if (_TempSensorActualValue != null)
            {
                _TempSensorActualValue.PropertyChanged -= TempSensorActualValue_PropertyChanged;
                _TempSensorActualValue = null;
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

            CurrentProcessModule = null;

            base.DeActivate();
        }

        //Deactivation for FermentationStarter
        protected virtual void Deactivate()
        {
            using (ACMonitor.Lock(_70100_MembersLock))
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
            }

            MessagesListSafe.Clear();
            RefreshMessageList();
        }

        protected virtual void InitBSO(ACComponent processModule)
        {
            PreProdTemperature = 0;
            CurrentProcessModule = processModule;

            if (ProcessModuleOrderInfo != null)
                ProcessModuleOrderInfo.PropertyChanged -= ProcessModuleOrderInfo_PropertyChanged;

            ProcessModuleOrderInfo = null;

            var childInstances = processModule.GetChildInstanceInfo(1, false);
            if (childInstances == null || !childInstances.Any())
                return;

            InitPreProdFunction(processModule, childInstances);

            if (PAFPreProducing != null)
            {
                gip.core.datamodel.ACClass funcClass = PAFPreProducing?.ComponentClass?.FromIPlusContext<gip.core.datamodel.ACClass>(DatabaseApp.ContextIPlus);

                if (funcClass == null)
                {
                    Messages.Error(this, "The component class of the pre production function can not be found!");
                    return;
                }

                string scaleACUrl = GetConfigValue(funcClass, PAFBakeryYeastProducing.PN_FermentationStarterScaleACUrl) as string;

                ACComponent scale = PAFPreProducing?.ACUrlCommand(scaleACUrl) as ACComponent;
                if (scale != null)
                {
                    PreProdScale = scale;

                    _PreProdScaleActValue = scale.GetPropertyNet("TotalActualWeight") as IACContainerTNet<double>;
                    if (_PreProdScaleActValue != null)
                    {
                        PreProdScaleActualValue = _PreProdScaleActValue.ValueT;
                        _PreProdScaleActValue.PropertyChanged += _PreProdScaleActValue_PropertyChanged;
                    }
                }
                else
                {
                    //Error50463: The fermentation starter scale can not be found! Please configure a fermentation scale ACUrl on the pre production function.
                    Messages.Error(this, "Error50463");
                }

                string storeACUrl = PAFPreProducing.ExecuteMethod(PAFBakeryYeastProducing.MN_GetVirtualStoreACUrl) as string;
                ACComponent store = PAFPreProducing?.ACUrlCommand(storeACUrl) as ACComponent;
                if (store != null)
                {
                    VirtualStore = store;

                    _VirtStoreOutEnabled = store.GetPropertyNet("OutwardEnabled") as IACContainerTNet<bool>;
                    if (_VirtStoreOutEnabled != null)
                    {
                        VirutalStoreOutwardEnabled = _VirtStoreOutEnabled.ValueT;
                        _VirtStoreOutEnabled.PropertyChanged += _VirtStoreOutEnabled_PropertyChanged;
                    }
                }
                else
                {
                    //Error50463: The fermentation starter scale can not be found! Please configure a fermentation scale ACUrl on the pre production function.
                    Messages.Error(this, "Error50463");
                }

                string pumpOverModuleACUrl = GetConfigValue(funcClass, PAFBakeryYeastProducing.PN_PumpOverProcessModuleACUrl) as string;
                if (!string.IsNullOrEmpty(pumpOverModuleACUrl))
                    PumpOverProcessModule = Root.ACUrlCommand(pumpOverModuleACUrl) as ACComponent;

                string tempSensorACUrl = GetConfigValue(funcClass, PAFBakeryYeastProducing.PN_TemperatureSensorACUrl) as string;
                if (!string.IsNullOrEmpty(tempSensorACUrl))
                {
                    ACComponent tempSensor = Root.ACUrlCommand(tempSensorACUrl) as ACComponent;
                    _TempSensorActualValue = tempSensor?.GetPropertyNet("ActualValue") as IACContainerTNet<double>;

                    if (_TempSensorActualValue != null)
                    {
                        _TempSensorActualValue.PropertyChanged += TempSensorActualValue_PropertyChanged;
                    }
                }
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
                    HandleDischargingACState(DischargingACStateProp.ValueT);
                }
                else
                {
                    //Error50464: Initialization is not complete. The property {0} can not be found!
                    Messages.Error(this, "Error50463", false, Const.ACState);
                }
            }

            ProcessModuleOrderInfo = processModule.GetPropertyNet("OrderInfo") as IACContainerTNet<string>;
            if (ProcessModuleOrderInfo == null)
            {
                //Error50464: Initialization is not complete. The property {0} can not be found!
                Messages.Error(this, "Error50463", false, "OrderInfo");
                return;
            }

            string orderInfo = ProcessModuleOrderInfo.ValueT;
            ParentBSOWCS.ApplicationQueue.Add(() => HandleOrderInfoPropChanged(orderInfo));

            ProcessModuleOrderInfo.PropertyChanged += ProcessModuleOrderInfo_PropertyChanged;

            VirtualSourceStoreID = PAFPreProducing?.ExecuteMethod(PAFBakeryYeastProducing.MN_GetSourceVirtualStoreID) as Guid?;

            if (VirtualSourceStoreID.HasValue)
            {
                VirtualSourceFacility = DatabaseApp.Facility.FirstOrDefault(c => c.VBiFacilityACClassID == VirtualSourceStoreID);

                if (VirtualSourceFacility == null)
                {
                    //Error50465: The virtual source facility can not be found!
                    Messages.Error(this, "Error50465");
                    return;
                }

                RefreshVirtualSourceStore();

                gip.core.datamodel.ACClass module = VirtualSourceFacility.GetFacilityACClass(DatabaseApp.ContextIPlus);

                if (module != null)
                {
                    var component = Root.ACUrlCommand(module.GetACUrlComponent()) as ACComponent;
                    if (component != null)
                    {
                        _VirtualSourceParkingSpace = new ACRef<ACComponent>(component, this);
                        RefreshParkingSpace = component.GetPropertyNet("RefreshParkingSpace") as IACContainerTNet<int>;
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

            var config = acClass.ConfigurationEntries.FirstOrDefault(c => c.ConfigACUrl == configName);
            if (config == null)
                return null;

            return config.Value;
        }

        #endregion

        #region Methods => Handle PropertyChanged

        protected void RefreshParkingSpace_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                ParentBSOWCS.ApplicationQueue.Add(() => RefreshVirtualSourceStore());
            }
        }

        protected void DischargingACStateProp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<ACStateEnum> senderProp = sender as IACContainerTNet<ACStateEnum>;
                if (senderProp != null)
                    HandleDischargingACState(senderProp.ValueT);
            }
        }

        protected void ProcessModuleOrderInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<string> senderProp = sender as IACContainerTNet<string>;
                if (senderProp != null)
                {
                    string orderInfo = senderProp.ValueT;
                    ParentBSOWCS.ApplicationQueue.Add(() => HandleOrderInfoPropChanged(orderInfo));
                }
            }
        }

        private void TempSensorActualValue_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<double> senderProp = sender as IACContainerTNet<double>;
                if (senderProp != null)
                {
                    PreProdTemperature = senderProp.ValueT;
                }
            }
        }

        protected void FermentationQuantityProp_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<double?> senderProp = sender as IACContainerTNet<double?>;
                if (senderProp != null)
                {
                    double? quantity = senderProp.ValueT;
                    ParentBSOWCS.ApplicationQueue.Add(() => HandleFermentationQuantity(quantity));
                }
            }
        }

        private void _PreProdScaleActValue_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<double> senderProp = sender as IACContainerTNet<double>;
                if (senderProp != null)
                {
                    PreProdScaleActualValue = senderProp.ValueT;
                }
            }
        }

        private void _VirtStoreOutEnabled_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<bool> senderProp = sender as IACContainerTNet<bool>;
                if (senderProp != null)
                {
                    VirutalStoreOutwardEnabled = senderProp.ValueT;
                }
            }
        }

        protected virtual void HandleDischargingACState(ACStateEnum acStateEnum)
        {
            DischargingState = (short)acStateEnum;
        }

        protected void HandleOrderInfoPropChanged(string orderInfo)
        {
            bool isOrderEmpty = string.IsNullOrEmpty(orderInfo);
            _IsOrderInfoEmpy = isOrderEmpty;

            if (isOrderEmpty)
            {
                Deactivate();
            }
            else
            {
                ACComponent currentProcessModule = CurrentProcessModule;

                string[] accessArr = (string[])currentProcessModule?.ACUrlCommand("!SemaphoreAccessedFrom");
                if (accessArr == null || !accessArr.Any())
                {
                    Deactivate();
                    return;
                }

                string pwGroupACUrl = accessArr[0];
                var pwGroup = Root.ACUrlCommand(pwGroupACUrl) as IACComponentPWNode;

                if (pwGroup == null)
                {
                    //Error50466: The user does not have access rights for class {0}.
                    Messages.Error(this, "Error50466", false, pwGroupACUrl);
                    return;
                }

                var pwGroupFermentation = new ACRef<IACComponentPWNode>(pwGroup, this);

                IEnumerable<ACChildInstanceInfo> pwNodes = pwGroup.GetChildInstanceInfo(1, new ChildInstanceInfoSearchParam() { OnlyWorkflows = true });

                if (pwNodes == null || !pwNodes.Any())
                    return;

                ACChildInstanceInfo fermentationStarter = pwNodes.FirstOrDefault(c => _PWFermentationStarterType.IsAssignableFrom(c.ACType.ValueT.ObjectType));
                IACContainerTNet<double?> fermentationQuantityProp = null;

                if (fermentationStarter != null)
                {
                    IACComponentPWNode pwNode = pwGroup.ACUrlCommand(fermentationStarter.ACUrlParent + "\\" + fermentationStarter.ACIdentifier) as IACComponentPWNode;
                    if (pwNode == null)
                    {
                        pwGroupFermentation.Detach();

                        //Error50466: The user does not have access rights for class {0}.
                        Messages.Error(this, "Error50466", false, fermentationStarter.ACUrlParent + "\\" + fermentationStarter.ACIdentifier);
                        return;
                    }

                    var fermentationStarterRef = new ACRef<IACComponentPWNode>(pwNode, this);

                    fermentationQuantityProp = fermentationStarterRef.ValueT.GetPropertyNet(PWBakeryFermentationStarter.PN_FSTargetQuantity) as IACContainerTNet<double?>;
                    if (fermentationQuantityProp == null)
                    {
                        pwGroupFermentation.Detach();
                        fermentationStarterRef.Detach();

                        //Error50464: Initialization is not complete. The property {0} can not be found!
                        Messages.Error(this, "Error50463", false, PWBakeryFermentationStarter.PN_FSTargetQuantity);
                        return;
                    }

                    using (ACMonitor.Lock(_70100_MembersLock))
                    {
                        PWGroupFermentation = pwGroupFermentation;
                        FermentationStarterRef = fermentationStarterRef;
                        FermentationQuantityProp = fermentationQuantityProp;
                    }
                }

                if (fermentationQuantityProp != null)
                {
                    HandleFermentationQuantity(fermentationQuantityProp.ValueT);
                    fermentationQuantityProp.PropertyChanged += FermentationQuantityProp_PropertyChanged;
                }

                OnHandleOrderInfoPropChanged(pwGroup);
            }
        }

        public virtual void OnHandleOrderInfoPropChanged(IACComponentPWNode pwGroup)
        {

        }

        protected void HandleFermentationQuantity(double? fermentationQuantity)
        {
            IACComponentPWNode fermentationStarter = null;

            using (ACMonitor.Lock(_70100_MembersLock))
            {
                fermentationStarter = FermentationStarterRef?.ValueT;
            }

            if (fermentationQuantity != null)
            {
                MessageItem msgItem = MessagesListSafe.FirstOrDefault(c => c.UserAckPWNode != null && c.UserAckPWNode.ValueT == fermentationStarter);

                if (msgItem == null)
                {
                    string message = Root.Environment.TranslateText(this, "msgFermentationStarter", Math.Round(fermentationQuantity.Value, 2));
                    msgItem = new MessageItem(fermentationStarter, this);
                    msgItem.Message = message;
                    AddToMessageList(msgItem);
                    RefreshMessageList();
                }
            }
            else
            {
                MessageItem msgItem = MessagesListSafe.FirstOrDefault(c => c.UserAckPWNode != null && c.UserAckPWNode.ValueT == fermentationStarter);
                if (msgItem != null)
                {
                    RemoveFromMessageList(msgItem);
                    RefreshMessageList();
                }
            }
        }

        protected void RefreshVirtualSourceStore()
        {
            DelegateToMainThread((object state) =>
            {
                if (VirtualSourceFacility == null)
                    return;

                VirtualSourceFacility.FacilityCharge_Facility.AutoLoad();
                VirtualSourceFacility.FacilityCharge_Facility.AutoRefresh();
                SourceFCList = VirtualSourceFacility.FacilityCharge_Facility.Where(c => !c.NotAvailable).ToList();
            });
        }

        #endregion

        #region Methods => Commands

        [ACMethodInfo("", "en{'Acknowledge - Start'}de{'Quittieren - Start'}", 800, true)]
        public virtual void Acknowledge()
        {
            IACComponentPWNode fermentationStarter = null;
            using (ACMonitor.Lock(_70100_MembersLock))
            {
                fermentationStarter = FermentationStarterRef?.ValueT;
            }

            if (fermentationStarter != null)
            {
                MessageItem msgItem = MessagesList.FirstOrDefault(c => c.UserAckPWNode != null && c.UserAckPWNode.ValueT == fermentationStarter);
                if (msgItem != null)
                {
                    bool? result = fermentationStarter.ExecuteMethod(PWBakeryFermentationStarter.MN_AckFermentationStarter, false) as bool?;
                    if (result.HasValue && !result.Value)
                    {
                        //The starter were not added to the container!Do you still want to start production?
                        if (Messages.Question(this, "Question50063") == Global.MsgResult.Yes)
                        {
                            fermentationStarter.ExecuteMethod(PWBakeryFermentationStarter.MN_AckFermentationStarter, true);
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
            RoutingResult rResult = ACRoutingService.FindSuccessors(RoutingService, DatabaseApp.ContextIPlus, true, CurrentProcessModule.ComponentClass,
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
            return _IsOrderInfoEmpy && PAFPreProducing != null;
        }

        [ACMethodInfo("", "en{'Start clean'}de{'Reinigen starten'}", 801, true)]
        public void StartClean()
        {
            gip.core.datamodel.ACClass pafClass = PAFPreProducing?.ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(DatabaseApp.ContextIPlus);
            if (pafClass == null)
            {
                //Error50467: Can not get the ACClass for a PAFPreProducing function.
                Messages.Error(this, "Error50467");
                return;
            }

            var config = pafClass.ConfigurationEntries.FirstOrDefault(c => c.ConfigACUrl == PAFBakeryYeastProducing.PN_CleaningMode);
            if (config == null)
            {
                //Error50468: Can not find the configuration for {0} on the PAFPreProducing function. 
                Messages.Error(this, "Error50468", false, PAFBakeryYeastProducing.PN_CleaningMode);
                return;
            }

            BakeryPreProdCleaningMode? mode = config.Value as BakeryPreProdCleaningMode?;
            if (mode == null)
            {
                //Error50469: The configuration for the pre production cleaning mode is null.
                Messages.Error(this, "Error50469");
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

                if (VirtualStore == null)
                    return;

                var outwardFacilityRef = VirtualStore.GetPropertyNet("Facility") as IACContainerTNet<ACRef<Facility>>;

                Facility outFacility = outwardFacilityRef?.ValueT?.ValueT;
                if (outFacility == null)
                {
                    //Error50462: The target virtual store can not be found!
                    Messages.Error(this, "Error50462");
                    return;
                }

                Facility outwardFacility = outFacility.FromAppContext<Facility>(DatabaseApp);

                bool outFacilityOutwardEnabled = outFacility.OutwardEnabled;

                outwardFacility.OutwardEnabled = true;

                CurrentBookParamRelocation.InwardFacility = outwardFacility;
                CurrentBookParamRelocation.OutwardFacility = outwardFacility;
                CurrentBookParamRelocation.InwardQuantity = 0.0001;
                CurrentBookParamRelocation.OutwardQuantity = 0.0001;

                config = pafClass.ConfigurationEntries.FirstOrDefault(c => c.ConfigACUrl == PAFBakeryYeastProducing.PN_CleaningProdACClassWF);
                if (config == null)
                {
                    //Error50468: Can not find the configuration for {0} on the PAFPreProducing function. 
                    Messages.Error(this, "Error50468", false, PAFBakeryYeastProducing.PN_CleaningProdACClassWF);
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

                BookNotAvailableFacility(outwardFacility);
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
                if (!_IsOrderInfoEmpy && DischargingState != (short)ACStateEnum.SMRunning && DischargingState != (short)ACStateEnum.SMPaused)
                {
                    //The product is not yet ready to be dosed.Are you sure that you want to release the container?
                    if (Messages.Question(this, "Question50069") == Global.MsgResult.No)
                        return;
                }

                PAFPreProducing?.ExecuteMethod(PAFBakeryYeastProducing.MN_SwitchVirtualStoreOutwardEnabled);

                if (_IsOrderInfoEmpy)//process module is not mapped
                {
                    //There is no order in the container. Do you want to reactivate the dosing process and temperature control ?
                    if (Messages.Question(this, "Question50066") == Global.MsgResult.Yes)
                    {
                        bool managers = CheckAndInitManagers();
                        if (!managers)
                            return;

                        ClearBookingData();

                        if (VirtualStore == null)
                            return;

                        var outwardFacilityRef = VirtualStore.GetPropertyNet("Facility") as IACContainerTNet<ACRef<Facility>>;

                        Facility outFacility = outwardFacilityRef?.ValueT?.ValueT;
                        if (outFacility == null)
                        {
                            //Error50462: The target virtual store can not be found!
                            Messages.Error(this, "Error50462");
                            return;
                        }

                        Facility sourceFacility = VirtualSourceFacility;

                        if (sourceFacility == null)
                        {
                            //Error50465: The virtual source facility can not be found!
                            Messages.Error(this, "Error50465");
                            return;
                        }

                        try
                        {
                            Facility outwardFacility = outFacility.FromAppContext<Facility>(DatabaseApp);
                            outFacility.AutoRefresh();

                            outwardFacility.OutwardEnabled = outFacility.OutwardEnabled;

                            CurrentBookParamRelocation.InwardMaterial = outwardFacility.Material;
                            CurrentBookParamRelocation.InwardFacility = outwardFacility;
                            CurrentBookParamRelocation.OutwardMaterial = outwardFacility.Material;
                            CurrentBookParamRelocation.OutwardFacility = sourceFacility;
                            CurrentBookParamRelocation.InwardQuantity = PreProdScaleActualValue;
                            CurrentBookParamRelocation.OutwardQuantity = PreProdScaleActualValue;

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
                else // process module is mapped
                {
                    if (DischargingState == (short)ACStateEnum.SMPaused)
                    {
                        //Question50068: The dosing readiness of the container is interrupted. Do you want to reactivate the dosing readiness?
                        if (Messages.Question(this, "Question50068") == Global.MsgResult.Yes)
                        {
                            DischargingACStateProp.ValueT = ACStateEnum.SMRunning;
                            DischargingState = (short)DischargingACStateProp.ValueT;
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
                if (DischargingState == (short)ACStateEnum.SMRunning)
                {
                    Global.ControlModes finishOrderRight = Global.ControlModes.Hidden;
                    string nMethodName;
                    gip.core.datamodel.ACClassMethod finishOrder = GetACClassMethod("FinishOrder", out nMethodName);
                    if (finishOrder != null)
                        finishOrderRight = ComponentClass.RightManager.GetControlMode(finishOrder);

                    if (finishOrderRight == Global.ControlModes.Enabled)
                    {
                        //The product is ready to be dosed. Do you want to finish the order? With button<No>, only the dosing readiness is taken away and the container is locked.
                        var questionResult = Messages.Question(this, "Question50067");
                        if (questionResult == Global.MsgResult.Yes)
                            FinishOrder();

                        else if (questionResult == Global.MsgResult.No && DischargingACStateProp != null)
                        {
                            DischargingACStateProp.ValueT = ACStateEnum.SMPaused;
                            DischargingState = (short)DischargingACStateProp.ValueT;
                        }

                        else if (questionResult == Global.MsgResult.Cancel)
                            return;
                    }
                    else
                    {
                        //The product is ready to be dosed. Do you want to take away the dosing readiness and lock the container?
                        var questionResult = Messages.Question(this, "Question50070");
                        if (questionResult == Global.MsgResult.Yes)
                        {
                            DischargingACStateProp.ValueT = ACStateEnum.SMPaused;
                            DischargingState = (short)DischargingACStateProp.ValueT;
                        }
                        else
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

        /// <summary>
        /// Method that used for rightmanagment on outward enabled/disabled operation at virutal taget store
        /// </summary>
        [ACMethodInfo("", "", 802, true)]
        public void FinishOrder()
        {
            if (DischargingACStateProp != null)
            {
                DischargingACStateProp.ValueT = ACStateEnum.SMCompleted;
                DischargingState = (short)DischargingACStateProp.ValueT;
            }
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
                //Error50470: The pumping targets not exist!
                Messages.Error(this, "Error50470");
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
            if (VirtualStore == null)
                return;

            var outwardFacilityRef = VirtualStore.GetPropertyNet("Facility") as IACContainerTNet<ACRef<Facility>>;

            Facility outFacility = outwardFacilityRef?.ValueT?.ValueT;
            if (outFacility == null)
            {
                //Error50462: The target virtual store can not be found!
                Messages.Error(this, "Error50462");
                return;
            }

            Facility outwardFacility = outFacility.FromAppContext<Facility>(DatabaseApp);

            double currentStock = outwardFacility.CurrentFacilityStock.AvailableQuantity;

            if (currentStock < PumpOverTargetQuantity)
            {
                //The current stock in facility is {0} kg, but you want pump over {0} kg. Do you still want to continue?
                if (Messages.Question(this, "The current stock in facility is {0} kg, but you want pump over {0} kg. Do you still want to continue?") != Global.MsgResult.Yes)
                {
                    return;
                }
            }

            Guid? inwardFacilityID = SelectedPumpTarget.Value as Guid?;

            if (!inwardFacilityID.HasValue)
            {
                //Error50471: The selected pumping target has not defined source virutal store!
                Messages.Error(this, "Error50471");
                return;
            }

            Facility inwardFacility = DatabaseApp.Facility.FirstOrDefault(c => c.FacilityID == inwardFacilityID.Value);
            if (inwardFacility == null)
            {
                //Error50465: The virtual source facility can not be found!
                Messages.Error(this, "Error50465");
                return;
            }

            CurrentBookParamRelocation.InwardFacility = inwardFacility;
            CurrentBookParamRelocation.OutwardFacility = outwardFacility;
            CurrentBookParamRelocation.InwardQuantity = PumpOverTargetQuantity;
            CurrentBookParamRelocation.OutwardQuantity = PumpOverTargetQuantity;

            var config = PAFPreProducing?.ComponentClass.ConfigurationEntries.FirstOrDefault(c => c.ConfigACUrl == "PumpOverACClassWF");
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

        #endregion

        #region Methods => Booking

        [ACMethodInfo("", "en{'Quant not available'}de{'Quant nicht verfügbar'}", 880, true)]
        public void BookNotAvailableFacilityCharge()
        {
            CleanNotAvailableBookData();

            CurrentBookParamNotAvailable.InwardFacilityCharge = SelectedSourceFC;
            CurrentBookParamNotAvailable.MDZeroStockState = MDZeroStockState.DefaultMDZeroStockState(DatabaseApp, MDZeroStockState.ZeroStockStates.SetNotAvailable);
            ACMethodEventArgs result = ACFacilityManager.BookFacility(CurrentBookParamNotAvailable, this.DatabaseApp) as ACMethodEventArgs;
            if (!CurrentBookParamNotAvailable.ValidMessage.IsSucceded() || CurrentBookParamNotAvailable.ValidMessage.HasWarnings())
                Messages.Msg(CurrentBookParamNotAvailable.ValidMessage);
            else if (result.ResultState == Global.ACMethodResultState.Failed || result.ResultState == Global.ACMethodResultState.Notpossible)
            {
                if (String.IsNullOrEmpty(result.ValidMessage.Message))
                    result.ValidMessage.Message = result.ResultState.ToString();
                Messages.Msg(result.ValidMessage);
            }
        }

        public bool IsEnabledBookNotAvailableFacilityCharge()
        {
            return SelectedSourceFC != null;
        }

        [ACMethodInfo("", "en{'Silo empty'}de{'Silo leer'}", 881, true)]
        public void BookNotAvailableFacility()
        {
            var outwardFacilityRef = VirtualStore.GetPropertyNet("Facility") as IACContainerTNet<ACRef<Facility>>;

            Facility outFacility = outwardFacilityRef?.ValueT?.ValueT;

            if (outFacility == null)
            {
                //Error50462: The target virtual store can not be found!
                Messages.Error(this, "Error50462");
                return;
            }

            Facility outwardFacility = outFacility.FromAppContext<Facility>(DatabaseApp);
            BookNotAvailableFacility(outwardFacility);
        }

        public bool IsEnabledBookNotAvailableFacility()
        {
            return VirtualStore != null;
        }

        private void BookNotAvailableFacility(Facility targetFacility)
        {
            CleanNotAvailableBookData();
            CurrentBookParamNotAvailableSilo.InwardFacility = targetFacility;
            CurrentBookParamNotAvailableSilo.MDZeroStockState = MDZeroStockState.DefaultMDZeroStockState(DatabaseApp, MDZeroStockState.ZeroStockStates.BookToZeroStock);
            ACMethodEventArgs result = ACFacilityManager.BookFacility(CurrentBookParamNotAvailableSilo, this.DatabaseApp) as ACMethodEventArgs;
            if (!CurrentBookParamNotAvailableSilo.ValidMessage.IsSucceded() || CurrentBookParamNotAvailableSilo.ValidMessage.HasWarnings())
                Messages.Msg(CurrentBookParamNotAvailableSilo.ValidMessage);
            else if (result.ResultState == Global.ACMethodResultState.Failed || result.ResultState == Global.ACMethodResultState.Notpossible)
            {
                if (String.IsNullOrEmpty(result.ValidMessage.Message))
                    result.ValidMessage.Message = result.ResultState.ToString();
                Messages.Msg(result.ValidMessage);
            }
        }

        private void CleanNotAvailableBookData()
        {
            CheckAndInitManagers();

            if (_BookParamNotAvailableClone == null)
                _BookParamNotAvailableClone = ACFacilityManager.ACUrlACTypeSignature("!" + GlobalApp.FBT_ZeroStock_FacilityCharge.ToString(), gip.core.datamodel.Database.GlobalDatabase) as ACMethodBooking; // Immer Globalen context um Deadlock zu vermeiden 

            if (_BookParamNotAvailableSiloClone == null)
                _BookParamNotAvailableSiloClone = ACFacilityManager.ACUrlACTypeSignature("!" + GlobalApp.FBT_ZeroStock_Facility_BulkMaterial.ToString(), gip.core.datamodel.Database.GlobalDatabase) as ACMethodBooking; // Immer Globalen context um Deadlock zu vermeiden 

            ACMethodBooking clone = _BookParamNotAvailableClone.Clone() as ACMethodBooking;
            CurrentBookParamNotAvailable = clone;
            clone = _BookParamNotAvailableSiloClone.Clone() as ACMethodBooking;
            CurrentBookParamNotAvailableSilo = clone;
        }

        #endregion

        #region Methods => Misc.

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
                return true;
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

        #endregion

        #endregion
    }
}

