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

        [ACPropertyInfo(807)]
        public ACComponent VirtualStore
        {
            get => _VirtualStore?.ValueT;
            set
            {
                _VirtualStore = new ACRef<ACComponent>(value, this);
                OnPropertyChanged("VirtualStore");
            }
        }

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

            if (MessagesList.Any())
            {
                MessagesList.Clear();
                MessagesList = MessagesList.ToList();
            }
        }

        protected virtual void InitBSO(ACComponent processModule)
        {
            if (ProcessModuleOrderInfo != null)
                ProcessModuleOrderInfo.PropertyChanged -= ProcessModuleOrderInfo_PropertyChanged;

            ProcessModuleOrderInfo = null;

            var childInstances = processModule.GetChildInstanceInfo(1, false);

            ACChildInstanceInfo func = childInstances.FirstOrDefault(c => _PAFBakeryYeastProdType.IsAssignableFrom(c.ACType.ValueT.ObjectType));
            if (func != null)
            {
                ACComponent funcComp = processModule.ACUrlCommand(func.ACIdentifier) as ACComponent;
                if (funcComp == null)
                    return;

                _PAFYeastProducing = new ACRef<ACComponent>(funcComp, this);

                string scaleACUrl = _PAFYeastProducing.ValueT.ExecuteMethod(PAFBakeryYeastProducing.MN_GetFermentationStarterScaleACUrl) as string;

                ACComponent scale = _PAFYeastProducing.ValueT?.ACUrlCommand(scaleACUrl) as ACComponent;
                if (scale != null)
                {
                    PreProdScale = scale;
                }
                else
                {
                    //error
                }

                string storeACUrl = _PAFYeastProducing.ValueT.ExecuteMethod(PAFBakeryYeastProducing.MN_GetVirtualStoreACUrl) as string;
                ACComponent store = _PAFYeastProducing.ValueT?.ACUrlCommand(storeACUrl) as ACComponent;
                if (store != null)
                {
                    VirtualStore = store;
                }
                else
                {
                    //error
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
            string text = Root.Environment.TranslateText(this, "tbDosingReady");

            if (DischargingACStateProp.ValueT == ACStateEnum.SMRunning)
            {
                IsDischargingActive = true;
                //StartDateTime = text;
            }
            else
            {
                IsDischargingActive = false;
                //if (StartDateTime == text)
                //    StartDateTime = null;
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
                MessageItem msgItem = MessagesList.FirstOrDefault(c => c.UserAckPWNode != null && c.UserAckPWNode.ValueT == FermentationStarterRef.ValueT);

                if (msgItem == null)
                {
                    string message = Root.Environment.TranslateText(this, "msgFermentationStarter", FermentationQuantityProp.ValueT);
                    msgItem = new MessageItem(FermentationStarterRef.ValueT, this);
                    msgItem.Message = message;
                    AddToMessageList(msgItem);
                }
            }
            else
            {
                MessageItem msgItem = MessagesList.FirstOrDefault(c => c.UserAckPWNode != null && c.UserAckPWNode.ValueT == FermentationStarterRef.ValueT);
                if (msgItem != null)
                    RemoveFromMessageList(msgItem);
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

                outwardFacility.OutwardEnabled = outFacility.OutwardEnabled;

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
            }
        }

        public bool IsEnabledClean()
        {
            return ProcessModuleOrderInfo?.ValueT == null && PAFPreProducing != null;
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

                        Facility outwardFacility = outFacility.FromAppContext<Facility>(DatabaseApp);

                        outwardFacility.OutwardEnabled = outFacility.OutwardEnabled;

                        CurrentBookParamRelocation.InwardFacility = outwardFacility;
                        CurrentBookParamRelocation.OutwardFacility = outwardFacility;
                        CurrentBookParamRelocation.InwardQuantity = 0.0001;
                        CurrentBookParamRelocation.OutwardQuantity = 0.0001;

                        //todo: lock or from another context
                        var config = PAFPreProducing?.ComponentClass.ACClassConfig_ACClass.FirstOrDefault(c => c.ConfigACUrl == "ContinueProdACClassWF");
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
            return PAFPreProducing != null;
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

        public override Global.ControlModes OnGetControlModes(IVBContent vbControl)
        {

            return base.OnGetControlModes(vbControl);
        }

        #endregion
    }
}

