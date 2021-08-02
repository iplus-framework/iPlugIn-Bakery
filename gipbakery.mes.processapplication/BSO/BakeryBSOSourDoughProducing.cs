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
    public class BakeryBSOSourDoughProducing : BakeryBSOYeastProducing
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

        public new const string ClassName = "BakeryBSOSourDoughProducing";

        #endregion

        #region Properties

        private Type _PAFBakerySourDoughProdType = typeof(PAFBakerySourDoughProducing);
        private Type _PAFBakeryDosingFlourType = typeof(PAFBakeryDosingFlour);
        private Type _PAFBakeryDosingWaterType = typeof(PAFBakeryDosingWater);


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


        private ACRef<ACComponent> _PAFSourDoughProducing;

        public override ACComponent PAFPreProducingFunction
        {
            get => _PAFSourDoughProducing?.ValueT;
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

        private IACContainerTNet<short> NextFermentationStageProp
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

        #endregion

        #region Methods

        public override void Activate(ACComponent selectedProcessModule)
        {
            InitBSO(selectedProcessModule);
        }

        public override void DeActivate()
        {
            Deactivate();

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

        protected override void Deactivate()
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

        protected override void InitBSO(ACComponent processModule)
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
                    PreProdScale = scale;
                }
                else
                {
                    //error
                }

                string storeACUrl = _PAFSourDoughProducing.ValueT.ExecuteMethod(PAFBakerySourDoughProducing.MN_GetVirtualStoreACUrl) as string;
                ACComponent store = _PAFSourDoughProducing.ValueT?.ACUrlCommand(storeACUrl) as ACComponent;
                if (store != null)
                {
                    VirtualStore = store;
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
            ParentBSOWCS.ApplicationQueue.Add(() =>  HandleOrderInfoPropChanged(orderInfo));
        }

        public override void OnHandleOrderInfoPropChanged()
        {
            base.OnHandleOrderInfoPropChanged();

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

            NextFermentationStageProp.PropertyChanged += NextFermentationStageProp_PropertyChanged;

            StartTimeProp.PropertyChanged += StartTimeProp_PropertyChanged;

            ReadyForDosingProp.PropertyChanged += ReadyForDosingProp_PropertyChanged;

            SetInfoProperties();
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

        [ACMethodInfo("","en{'Acknowledge - Start'}de{'Quittieren - Start'}",800, true)]
        public override void Acknowledge()
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

        public override Global.ControlModes OnGetControlModes(IVBContent vbControl)
        {

            return base.OnGetControlModes(vbControl);
        }

        #endregion
    }
}
