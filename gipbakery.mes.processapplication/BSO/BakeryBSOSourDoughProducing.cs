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

        public override ACComponent PAFPreProducing
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
            private set
            {
                _FlourScale = new ACRef<ACComponent>(value, this);
                OnPropertyChanged("FlourScale");
            }
        }

        private double _FlourScaleActualValue;
        [ACPropertyInfo(808, "", "en{'Flour actual q.'}de{'Mehl Ist'}")]
        public double FlourScaleActualValue
        {
            get => _FlourScaleActualValue;
            set 
            {
                _FlourScaleActualValue = value;
                OnPropertyChanged("FlourScaleActualValue");
            }
        }

        private ACRef<ACComponent> _WaterScale
        {
            get;
            set;
        }

        [ACPropertyInfo(809)]
        public ACComponent WaterScale
        {
            get => _WaterScale?.ValueT;
            private set
            {
                _WaterScale = new ACRef<ACComponent>(value, this);
                OnPropertyChanged("WaterScale");
            }
        }

        private double _WaterScaleActualValue;
        [ACPropertyInfo(810, "", "en{'Water actual q.'}de{'Wasser Ist'}")]
        public double WaterScaleActualValue
        {
            get => _WaterScaleActualValue;
            set
            {
                _WaterScaleActualValue = value;
                OnPropertyChanged("WaterScaleActualValue");
            }
        }

        private IACContainerTNet<double> _WaterScaleActValue;
        private IACContainerTNet<double> _FlourScaleActValue;

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
            SubscribeToWFNodes(selectedProcessModule);
            InitBSO(selectedProcessModule);
        }

        public override void DeActivate()
        {
            base.DeActivate();

            if (_PAFSourDoughProducing != null)
            {
                _PAFSourDoughProducing.Detach();
                _PAFSourDoughProducing = null;
            }

            if (_FlourScaleActValue != null)
            {
                _FlourScaleActValue.PropertyChanged -= _FlourScaleActValue_PropertyChanged;
                _FlourScaleActValue = null;
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
        }

        protected override void Deactivate()
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

            MessagesListSafe.Clear();
            RefreshMessageList();
        }

        protected override void InitBSO(ACComponent processModule)
        {
            StartDateTime = null;
            ReadyForDosing = null;
            NextStage = 0;

            base.InitBSO(processModule);

            var childInstances = processModule.GetChildInstanceInfo(1, false);
            if (childInstances == null || !childInstances.Any())
                return;

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
                    _FlourScaleActValue = scale.GetPropertyNet("ActualWeight") as IACContainerTNet<double>;
                    if (_FlourScaleActValue != null)
                    {
                        FlourScaleActualValue = _FlourScaleActValue.ValueT;
                        _FlourScaleActValue.PropertyChanged += _FlourScaleActValue_PropertyChanged;
                    }
                }
                else
                {
                    Messages.LogError(this.GetACUrl(), "InitBSO(10)", "Can't find the flour scale.");
                }

                FlourDiffQuantityProp = tempRef.ValueT.GetPropertyNet("FlourDiffQuantity") as IACContainerTNet<double>;

                if (FlourDiffQuantityProp != null)
                {
                    FlourDiffQuantityProp.PropertyChanged += FlourTargetQuantityProp_PropertyChanged;
                }
                else
                {
                    Messages.LogError(this.GetACUrl(), "InitBSO(15)", "Can't find the property FlourDiffQuantity.");
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
                    _WaterScaleActValue = scale.GetPropertyNet("ActualWeight") as IACContainerTNet<double>;
                    if (_WaterScaleActValue != null)
                    {
                        WaterScaleActualValue = _WaterScaleActValue.ValueT;
                        _WaterScaleActValue.PropertyChanged += _WaterScaleActValue_PropertyChanged;
                    }
                }
                else
                {
                    Messages.LogError(this.GetACUrl(), "InitBSO(20)", "Can't find the water scale.");
                }

                WaterDiffQuantityProp = tempRef.ValueT.GetPropertyNet("WaterDiffQuantity") as IACContainerTNet<double>;

                if (WaterDiffQuantityProp != null)
                {
                    WaterDiffQuantityProp.PropertyChanged += WaterTargetQuantityProp_PropertyChanged;
                }
                else
                {
                    Messages.LogError(this.GetACUrl(), "InitBSO(25)", "Can't find the property WaterDiffQuantity.");
                }

                tempRef.Detach();
                tempRef = null;
            }
        }



        public override void InitPreProdFunction(ACComponent processModule, IEnumerable<ACChildInstanceInfo> childInstances)
        {
            ACChildInstanceInfo func = childInstances.FirstOrDefault(c => _PAFBakerySourDoughProdType.IsAssignableFrom(c.ACType.ValueT.ObjectType));
            if (func != null)
            {
                ACComponent funcComp = processModule.ACUrlCommand(func.ACIdentifier) as ACComponent;
                if (funcComp == null)
                    return;

                _PAFSourDoughProducing = new ACRef<ACComponent>(funcComp, this);
            }
        }

        public override void OnHandleOrderInfoPropChanged(IACComponentPWNode pwGroup)
        {
            NextFermentationStageProp = pwGroup.GetPropertyNet(PWBakeryGroupFermentation.PN_NextFermentationStage) as IACContainerTNet<short>;
            if (NextFermentationStageProp == null)
            {
                return;
            }

            StartTimeProp = pwGroup.GetPropertyNet(PWBakeryGroupFermentation.PN_StartNextFermentationStageTime) as IACContainerTNet<DateTime>;
            if (StartTimeProp == null)
            {
                NextFermentationStageProp = null;
                return;
            }

            ReadyForDosingProp = pwGroup.GetPropertyNet(PWBakeryGroupFermentation.PN_ReadyForDosingTime) as IACContainerTNet<DateTime>;
            if (ReadyForDosingProp == null)
            {
                NextFermentationStageProp = null;
                StartTimeProp = null;
                return;
            }

            SetInfoProperties(ReadyForDosingProp.ValueT.ToString(), StartTimeProp.ValueT.ToString(), NextFermentationStageProp.ValueT);

            NextFermentationStageProp.PropertyChanged += NextFermentationStageProp_PropertyChanged;

            StartTimeProp.PropertyChanged += StartTimeProp_PropertyChanged;

            ReadyForDosingProp.PropertyChanged += ReadyForDosingProp_PropertyChanged;
        }

        private void SetInfoProperties(string readyForDosing, string startDateTime, short nextStage)
        {
            ReadyForDosing = readyForDosing;
            if (DischargingState != (short)ACStateEnum.SMRunning)
                StartDateTime = startDateTime;
            NextStage = nextStage;
        }

        private void ReadyForDosingProp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<DateTime> senderProp = sender as IACContainerTNet<DateTime>;
                if (senderProp != null)
                {
                    ReadyForDosing = senderProp.ValueT.ToString();
                }
            }
        }

        private void StartTimeProp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<DateTime> senderProp = sender as IACContainerTNet<DateTime>;
                if (senderProp != null)
                {
                    StartDateTime = senderProp.ValueT.ToString();
                }
            }
        }

        private void NextFermentationStageProp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<short> senderProp = sender as IACContainerTNet<short>;
                if (senderProp != null)
                {
                    NextStage = senderProp.ValueT;
                }
            }
        }

        private void WaterTargetQuantityProp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<double> senderProp = sender as IACContainerTNet<double>;
                if (senderProp != null)
                {
                    WaterDiffQuantity = senderProp.ValueT;
                }
            }
        }

        private void FlourTargetQuantityProp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<double> senderProp = sender as IACContainerTNet<double>;
                if (senderProp != null)
                {
                    FlourDiffQuantity = senderProp.ValueT;
                }
            }
        }

        private void _WaterScaleActValue_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<double> senderProp = sender as IACContainerTNet<double>;
                if (senderProp != null)
                {
                    WaterScaleActualValue = senderProp.ValueT;
                }
            }
        }

        private void _FlourScaleActValue_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<double> senderProp = sender as IACContainerTNet<double>;
                if (senderProp != null)
                {
                    FlourScaleActualValue = senderProp.ValueT;
                }
            }
        }

        protected override void HandleDischargingACState(ACStateEnum acState)
        {
            string text = Root.Environment.TranslateText(this, "tbDosingReady");
            DischargingState = (short)acState;

            if (acState == ACStateEnum.SMRunning)
            {
                StartDateTime = text;
            }
            else
            {
                if (StartDateTime == text)
                    StartDateTime = null;
            }
        }

        [ACMethodInfo("","en{'Acknowledge - Start'}de{'Quittieren - Start'}",800, true)]
        public override void Acknowledge()
        {
            IACComponentPWNode fermentationStarter = null;

            using (ACMonitor.Lock(_70100_MembersLock))
            {
                fermentationStarter = FermentationStarterRef?.ValueT;
            }

            if (fermentationStarter != null )
            {
                MessageItem msgItem = MessagesList.FirstOrDefault(c => c.UserAckPWNode != null && c.UserAckPWNode.ValueT == fermentationStarter);
                if (msgItem != null)
                {
                    bool? result = fermentationStarter.ExecuteMethod(PWBakeryFermentationStarter.MN_AckFermentationStarter, false) as bool?;
                    if (result.HasValue && !result.Value)
                    {
                        if (Messages.Question(this, "Question50063") == Global.MsgResult.Yes)
                        {
                            fermentationStarter.ExecuteMethod(PWBakeryFermentationStarter.MN_AckFermentationStarter, true);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
