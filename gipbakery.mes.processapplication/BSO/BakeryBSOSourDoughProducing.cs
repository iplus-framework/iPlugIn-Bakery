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
    [ACClassInfo(Const.PackName_VarioManufacturing, "en{'Sour dough'}de{'Sauerteig'}", Global.ACKinds.TACBSO, Global.ACStorableTypes.NotStorable, true, true, SortIndex = 50)]
    public class BakeryBSOSourDoughProducing : BSOWorkCenterMessages
    {
        #region c'tors

        public BakeryBSOSourDoughProducing(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
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
            _PAFBakerySourDoughProdType = null;
            _PAFBakeryDosingWaterType = null;
            _PAFBakeryDosingFlourType = null;
            _PAFDischargingType = null;

            return base.ACDeInit(deleteACClassTask);
        }

        public const string ClassName = "BakeryBSOSourDoughProducing";

        #endregion

        #region Properties

        private Type _PWFermentationStarterType = typeof(PWBakeryFermentationStarter);
        private Type _PAFBakerySourDoughProdType = typeof(PAFBakerySourDoughProducing);
        private Type _PAFBakeryDosingFlourType = typeof(PAFBakeryDosingFlour);
        private Type _PAFBakeryDosingWaterType = typeof(PAFBakeryDosingWater);
        private Type _PAFDischargingType = typeof(PAFDischarging);


        private double _FlourDiffQuantity;
        [ACPropertyInfo(801, "", "en{'Flour difference q.'}de{'Mehl Rest'}")]
        public double FlourDiffQuantity
        {
            get => _FlourDiffQuantity;
            set
            {
                _FlourDiffQuantity = value;
                OnPropertyChanged("FlourDiffQuantity");
            }
        }

        private double _WaterDiffQuantity;
        [ACPropertyInfo(803, "", "en{'Water difference q.'}de{'Wasser Rest'}")]
        public double WaterDiffQuantity
        {
            get => _WaterDiffQuantity;
            set
            {
                _WaterDiffQuantity = value;
                OnPropertyChanged("WaterDiffQuantity");
            }
        }

        private short _NextStage;
        [ACPropertyInfo(804, "", "en{'Next stage'}de{'Nach. Stufe'}")]
        public short NextStage
        {
            get => _NextStage;
            set
            {
                _NextStage = value;
                OnPropertyChanged("NextStage");
            }
        }

        private string _StartDateTime;
        [ACPropertyInfo(805, "", "en{'Start time'}de{'Start zeit'}")]
        public string StartDateTime
        {
            get => _StartDateTime;
            set
            {
                _StartDateTime = value;
                OnPropertyChanged("StartDateTime");
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

        private ACRef<ACComponent> _PAFSourDoughProducing;

        private ACRef<ACComponent> _SourDoughScale
        {
            get;
            set;
        }

        [ACPropertyInfo(807)]
        public ACComponent SourDoughScale
        {
            get => _SourDoughScale?.ValueT;
            set
            {
                _SourDoughScale = new ACRef<ACComponent>(value, this);
                OnPropertyChanged("SourDoughScale");
            }
        }

        private ACRef<ACComponent> _SourDoughStore
        {
            get;
            set;
        }

        [ACPropertyInfo(807)]
        public ACComponent SourDoughStore
        {
            get => _SourDoughStore?.ValueT;
            set
            {
                _SourDoughStore = new ACRef<ACComponent>(value, this);
                OnPropertyChanged("SourDoughStore");
            }
        }

        private ACRef<ACComponent> _FlourScale
        {
            get;
            set;
        }

        [ACPropertyInfo(807)]
        public ACComponent FlourScale
        {
            get => _FlourScale?.ValueT;
            set
            {
                _FlourScale = new ACRef<ACComponent>(value, this);
                OnPropertyChanged("FlourScale");
            }
        }

        private ACRef<ACComponent> _WaterScale
        {
            get;
            set;
        }

        [ACPropertyInfo(807)]
        public ACComponent WaterScale
        {
            get => _WaterScale?.ValueT;
            set
            {
                _WaterScale = new ACRef<ACComponent>(value, this);
                OnPropertyChanged("WaterScale");
            }
        }

        public IACContainerTNet<string> ProcessModuleOrderInfo;

        private ACRef<IACComponentPWGroup> PWGroupFermentation
        {
            get;
            set;
        }

        private ACRef<IACComponentPWNode> FermentationStarterRef
        {
            get;
            set;
        }

        private IACContainerTNet<double?> FermentationQuantityProp
        {
            get;
            set;
        }

        private IACContainerTNet<short> NextFermentationStageProp
        {
            get;
            set;
        }

        private IACContainerTNet<ACStateEnum> DischargingACStateProp
        {
            get;
            set;
        }

        private IACContainerTNet<DateTime> StartTimeProp
        {
            get;
            set;
        }

        private IACContainerTNet<DateTime> ReadyForDosingProp
        {
            get;
            set;
        }

        private IACContainerTNet<double> FlourDiffQuantityProp
        {
            get;
            set;
        }

        private IACContainerTNet<double> WaterDiffQuantityProp
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

            if (_SourDoughScale != null)
            {
                _SourDoughScale.Detach();
                _SourDoughScale = null;
            }

            if (_SourDoughStore != null)
            {
                _SourDoughStore.Detach();
                _SourDoughStore = null;
            }

            if (_PAFSourDoughProducing != null)
            {
                _PAFSourDoughProducing.Detach();
                _PAFSourDoughProducing = null;
            }

            if (_FlourScale != null)
            {
                _FlourScale.Detach();
                _FlourScale = null;
            }

            if (_WaterScale != null)
            {
                _WaterScale.Detach();
                _WaterScale = null;
            }

            if (FlourDiffQuantityProp != null)
            {
                FlourDiffQuantityProp.PropertyChanged -= FlourTargetQuantityProp_PropertyChanged;
                FlourDiffQuantityProp = null;
            }

            if (WaterDiffQuantityProp != null)
            {
                WaterDiffQuantityProp.PropertyChanged -= WaterTargetQuantityProp_PropertyChanged;
                WaterDiffQuantityProp = null;
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

        private void Deactivate()
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

            if (NextFermentationStageProp != null)
            {
                NextFermentationStageProp.PropertyChanged -= NextFermentationStageProp_PropertyChanged;
                NextFermentationStageProp = null;
            }

            if (StartTimeProp != null)
            {
                StartTimeProp.PropertyChanged -= StartTimeProp_PropertyChanged;
                StartTimeProp = null;
            }

            if (ReadyForDosingProp != null)
            {
                ReadyForDosingProp.PropertyChanged -= ReadyForDosingProp_PropertyChanged;
                ReadyForDosingProp = null;
            }

            if (MessagesList.Any())
            {
                MessagesList.Clear();
                MessagesList = MessagesList.ToList();
            }
        }

        private void InitBSO(ACComponent processModule)
        {
            if (ProcessModuleOrderInfo != null)
                ProcessModuleOrderInfo.PropertyChanged -= ProcessModuleOrderInfo_PropertyChanged;

            ProcessModuleOrderInfo = null;

            var childInstances = processModule.GetChildInstanceInfo(1, false);

            ACChildInstanceInfo func = childInstances.FirstOrDefault(c => _PAFBakerySourDoughProdType.IsAssignableFrom(c.ACType.ValueT.ObjectType));
            if (func != null)
            {
                ACComponent funcComp = processModule.ACUrlCommand(func.ACIdentifier) as ACComponent;
                if (funcComp == null)
                    return;

                _PAFSourDoughProducing = new ACRef<ACComponent>(funcComp, this);

                string scaleACUrl = _PAFSourDoughProducing.ValueT.ExecuteMethod(PAFBakerySourDoughProducing.MN_GetFermentationStarterScaleACUrl) as string;

                ACComponent scale = _PAFSourDoughProducing.ValueT?.ACUrlCommand(scaleACUrl) as ACComponent;
                if (scale != null)
                {
                    SourDoughScale = scale;
                }
                else
                {
                    //error
                }

                string storeACUrl = _PAFSourDoughProducing.ValueT.ExecuteMethod("GetSourDoughStoreACUrl") as string;
                ACComponent store = _PAFSourDoughProducing.ValueT?.ACUrlCommand(storeACUrl) as ACComponent;
                if (store != null)
                {
                    SourDoughStore = store;
                }
                else
                {
                    //error
                }
            }

            ACChildInstanceInfo flourFunc = childInstances.FirstOrDefault(c => _PAFBakeryDosingFlourType.IsAssignableFrom(c.ACType.ValueT.ObjectFullType));
            if (flourFunc != null)
            {
                ACComponent funcComp = processModule.ACUrlCommand(flourFunc.ACIdentifier) as ACComponent;
                if (funcComp == null)
                    return;

                ACRef<ACComponent> tempRef = new ACRef<ACComponent>(funcComp, this);

                string scaleACUrl = tempRef.ValueT.ExecuteMethod("GetFlourDosingScale") as string;
                ACComponent scale = tempRef.ValueT.ACUrlCommand(scaleACUrl) as ACComponent;

                if (scale != null)
                {
                    FlourScale = scale;
                }
                else
                {
                    //Error
                }

                FlourDiffQuantityProp = tempRef.ValueT.GetPropertyNet("FlourDiffQuantity") as IACContainerTNet<double>;

                if (FlourDiffQuantityProp != null)
                {
                    FlourDiffQuantityProp.PropertyChanged += FlourTargetQuantityProp_PropertyChanged;
                }
                else
                {
                    //TODO: error
                }    

                tempRef.Detach();
                tempRef = null;
            }

            ACChildInstanceInfo waterFunc = childInstances.FirstOrDefault(c => _PAFBakeryDosingWaterType.IsAssignableFrom(c.ACType.ValueT.ObjectType));
            if (waterFunc != null)
            {
                ACComponent funcComp = processModule.ACUrlCommand(waterFunc.ACIdentifier) as ACComponent;
                if (funcComp == null)
                    return;

                ACRef<ACComponent> tempRef = new ACRef<ACComponent>(funcComp, this);

                string scaleACUrl = tempRef.ValueT.ExecuteMethod("GetWaterDosingScale") as string;
                ACComponent scale = tempRef.ValueT.ACUrlCommand(scaleACUrl) as ACComponent;

                if (scale != null)
                {
                    WaterScale = scale;
                }
                else
                {
                    //Error
                }

                WaterDiffQuantityProp = tempRef.ValueT.GetPropertyNet("WaterDiffQuantity") as IACContainerTNet<double>;

                if (WaterDiffQuantityProp != null)
                {
                    WaterDiffQuantityProp.PropertyChanged += WaterTargetQuantityProp_PropertyChanged;
                }
                else
                {
                    //TODO: error
                }

                tempRef.Detach();
                tempRef = null;
            }

            ACChildInstanceInfo dischFunc = childInstances.FirstOrDefault(c => _PAFDischargingType.IsAssignableFrom(c.ACType.ValueT.ObjectType));
            if (dischFunc != null)
            {
                ACComponent funcComp = processModule.ACUrlCommand(dischFunc.ACIdentifier) as ACComponent;
                if (funcComp == null)
                    return;

                DischargingACStateProp = funcComp?.GetPropertyNet("ACState") as IACContainerTNet<ACStateEnum>;
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
            ParentBSOWCS.ApplicationQueue.Add(() =>  HandleOrderInfoPropChanged(orderInfo));
        }

        private void DischargingACStateProp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                HandleDischargingACState();
            }
        }

        private void HandleDischargingACState()
        {
            string text = Root.Environment.TranslateText(this, "tbDosingReady");

            if (DischargingACStateProp.ValueT == ACStateEnum.SMRunning)
            {
                IsDischargingActive = true;
                StartDateTime = text;
            }
            else
            {
                IsDischargingActive = false;
                if (StartDateTime == text)
                    StartDateTime = null;
            }
        }

        private void FlourActualQuantityProp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ProcessModuleOrderInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                string orderInfo = ProcessModuleOrderInfo.ValueT;
                ParentBSOWCS.ApplicationQueue.Add(() => HandleOrderInfoPropChanged(orderInfo));
            }
        }

        private void HandleOrderInfoPropChanged(string orderInfo)
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

                NextFermentationStageProp = PWGroupFermentation.ValueT.GetPropertyNet(PWBakeryGroupFermentation.PN_NextFermentationStage) as IACContainerTNet<short>;
                if (NextFermentationStageProp == null)
                {
                    //TODO:
                    return;
                }

                StartTimeProp = PWGroupFermentation.ValueT.GetPropertyNet(PWBakeryGroupFermentation.PN_StartNextFermentationStageTime) as IACContainerTNet<DateTime>;
                if (StartTimeProp == null)
                {
                    //TODO:
                    return;
                }

                ReadyForDosingProp = PWGroupFermentation.ValueT.GetPropertyNet(PWBakeryGroupFermentation.PN_ReadyForDosingTime) as IACContainerTNet<DateTime>;
                if (ReadyForDosingProp == null)
                {
                    //TODO:
                    return;
                }

                if (FermentationQuantityProp != null)
                {
                    FermentationQuantityProp.PropertyChanged += FermentationQuantityProp_PropertyChanged;
                    HandleFermentationQunatity();
                }
                NextFermentationStageProp.PropertyChanged += NextFermentationStageProp_PropertyChanged;

                StartTimeProp.PropertyChanged += StartTimeProp_PropertyChanged;

                ReadyForDosingProp.PropertyChanged += ReadyForDosingProp_PropertyChanged;

                SetInfoProperties();
            }
        }

        private void SetInfoProperties()
        {
            ReadyForDosing = ReadyForDosingProp.ValueT.ToString();
            if (!IsDischargingActive)
                StartDateTime = StartTimeProp.ValueT.ToString();
            NextStage = NextFermentationStageProp.ValueT;
        }

        private void ReadyForDosingProp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                ReadyForDosing = ReadyForDosingProp.ValueT.ToString();
            }
        }

        private void StartTimeProp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                StartDateTime = StartTimeProp.ValueT.ToString();
            }
        }

        private void NextFermentationStageProp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                NextStage = NextFermentationStageProp.ValueT;
            }
        }

        private void WaterTargetQuantityProp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                WaterDiffQuantity = WaterDiffQuantityProp.ValueT;
            }
        }

        private void FlourTargetQuantityProp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                FlourDiffQuantity = FlourDiffQuantityProp.ValueT;
            }
        }

        private void FermentationQuantityProp_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                ParentBSOWCS.ApplicationQueue.Add(() => HandleFermentationQunatity());
            }
        }

        private void HandleFermentationQunatity()
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

        [ACMethodInfo("","en{'Acknowledge - Start'}de{'Quittieren - Start'}",800, true)]
        public void Acknowledge()
        {
            if (FermentationStarterRef != null )
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

        public bool IsEnabledAcknowledge()
        {
            return true;
        }

        [ACMethodInfo("", "en{'Clean'}de{'Reinigen'}", 801, true)]
        public void Clean()
        {
            Msg resultMsg = _PAFSourDoughProducing.ValueT.ExecuteMethod("Clean", (short)11) as Msg;
            if (resultMsg != null)
            {
                Messages.Msg(resultMsg);
            }
        }

        public bool IsEnabledClean()
        {
            return ProcessModuleOrderInfo?.ValueT == null && _PAFSourDoughProducing != null && _PAFSourDoughProducing.ValueT != null;
        }

        [ACMethodInfo("", "", 802, true)]
        public void StoreOutwardEnabledOn()
        {
            if (IsEnabledStoreOutwardEnabledOn())
            {
                _PAFSourDoughProducing.ValueT.ExecuteMethod("SwitchSourDoughStoreOutwardEnabled");

                if (!IsDischargingActive)
                {
                    if (Messages.Question(this, "Question50065") == Global.MsgResult.Yes)
                    {
                        if (_ACFacilityManager == null)
                        {
                            _ACFacilityManager = FacilityManager.ACRefToServiceInstance(this);
                            if (_ACFacilityManager == null)
                            {
                                //Error50432: The facility manager is null.
                                Messages.Error(this, "Error50432");
                                return;
                            }
                        }

                        if (_ACPickingManager == null)
                        {
                            _ACPickingManager = ACRefToPickingManager();
                        }

                        ClearBookingData();

                        if (_SourDoughStore == null || _SourDoughStore.ValueT == null)
                            return;

                        var outwardFacilityRef = _SourDoughStore.ValueT.GetPropertyNet("Facility") as IACContainerTNet<ACRef<Facility>>;

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
                        CurrentBookParamRelocation.InwardQuantity = 0.001;
                        CurrentBookParamRelocation.OutwardQuantity = 0.001;

                        //todo: lock or from another context
                        var config = _PAFSourDoughProducing?.ValueT?.ComponentClass.ACClassConfig_ACClass.FirstOrDefault(c => c.ConfigACUrl == "SourDoughContinueProdACClassWF");
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
            return _PAFSourDoughProducing != null && _PAFSourDoughProducing.ValueT != null;
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

                    var questionResult = Messages.Question(this, "Question50064");
                    if (questionResult == Global.MsgResult.Yes && DischargingACStateProp != null)
                    {
                        DischargingACStateProp.ValueT = ACStateEnum.SMCompleted;
                    }
                    else if (questionResult == Global.MsgResult.Cancel)
                    {
                        return;
                    }
                }

                _PAFSourDoughProducing.ValueT.ExecuteMethod("SwitchSourDoughStoreOutwardEnabled");
            }
        }

        public bool IsEnabledStoreOutwardEnabledOff()
        {
            return _PAFSourDoughProducing != null && _PAFSourDoughProducing.ValueT != null;
        }

        [ACMethodInfo("", "en{'Pump over'}de{'Umpumpen'}", 802, true)]
        public void PumpOver()
        {
            ACValueList targets = _PAFSourDoughProducing.ValueT.ExecuteMethod("GetPumpOverTargets") as ACValueList;
            PumpTargets = targets;

            if (PumpTargets == null || !PumpTargets.Any())
            {
                //TODO: error
                return;
            }

            if (_ACFacilityManager == null)
            {
                _ACFacilityManager = FacilityManager.ACRefToServiceInstance(this);
                if (_ACFacilityManager == null)
                {
                    //Error50432: The facility manager is null.
                    Messages.Error(this, "Error50432");
                    return;
                }
            }

            if (_ACPickingManager == null)
            {
                _ACPickingManager = ACRefToPickingManager();
            }

            ClearBookingData();
            ShowDialog(this, "PumpOverDialog");
        }

        public bool IsEnabledPumpOver()
        {
            return _PAFSourDoughProducing != null && _PAFSourDoughProducing.ValueT != null;
        }

        [ACMethodInfo("", "en{'Pump over'}de{'Umpumpen'}", 803, true)]
        public void PumpOverStart()
        {
            if (_SourDoughStore == null || _SourDoughStore.ValueT == null)
                return;

            var outwardFacilityRef = _SourDoughStore.ValueT.GetPropertyNet("Facility") as IACContainerTNet<ACRef<Facility>>;

            if (outwardFacilityRef == null)
            {
                //TODO: error
            }

            Facility outFacility = outwardFacilityRef.ValueT?.ValueT;

            if ( outFacility == null)
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
            var config = _PAFSourDoughProducing?.ValueT?.ComponentClass.ACClassConfig_ACClass.FirstOrDefault(c => c.ConfigACUrl == "PumpOverACClassWF");
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
            return _PAFSourDoughProducing != null && _PAFSourDoughProducing.ValueT != null;
        }

        public override Global.ControlModes OnGetControlModes(IVBContent vbControl)
        {

            return base.OnGetControlModes(vbControl);
        }

        #endregion
    }
}
