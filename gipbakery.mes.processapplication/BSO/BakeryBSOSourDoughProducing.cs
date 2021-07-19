using gip.bso.manufacturing;
using gip.core.autocomponent;
using gip.core.datamodel;
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

        public BakeryBSOSourDoughProducing(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
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

            return base.ACDeInit(deleteACClassTask);
        }

        public const string ClassName = "BakeryBSOSourDoughProducing";

        #endregion

        #region Properties

        private Type _PWFermentationStarterType = typeof(PWBakeryFermentationStarter);

        private double _FlourActualQuantity;
        [ACPropertyInfo(800, "", "en{'Flour actual q.'}de{'Mehl Ist'}")]
        public double FlourActualQuantity
        {
            get => _FlourActualQuantity;
            set
            {
                _FlourActualQuantity = value;
                OnPropertyChanged("FlourActualQuantity");
            }
        }

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

        private double _WaterActualQuantity;
        [ACPropertyInfo(802, "", "en{'Water actual q.'}de{'Wasser Ist'}")]
        public double WaterActualQuantity
        {
            get => _WaterActualQuantity;
            set
            {
                _WaterActualQuantity = value;
                OnPropertyChanged("WaterActualQuantity");
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

        private DateTime _StartDateTime;
        [ACPropertyInfo(805, "", "en{'Start time'}de{'Start zeit'}")]
        public DateTime StartDateTime
        {
            get => _StartDateTime;
            set
            {
                _StartDateTime = value;
                OnPropertyChanged("StartDateTime");
            }
        }

        private DateTime _ReadyForDosing;
        [ACPropertyInfo(806, "", "en{'Ready for dosing'}de{'Dosierbereitschaft'}")]
        public DateTime ReadyForDosing
        {
            get => _ReadyForDosing;
            set
            {
                _ReadyForDosing = value;
                OnPropertyChanged("ReadyForDosing");
            }
        }

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

        public IACContainerTNet<string> ProcessModuleOrderInfo;

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

            if (ProcessModuleOrderInfo != null)
            {
                ProcessModuleOrderInfo.PropertyChanged -= ProcessModuleOrderInfo_PropertyChanged;
                ProcessModuleOrderInfo = null;
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
            ACChildInstanceInfo func = childInstances.FirstOrDefault(c => typeof(PAFBakerySourDoughProducing).IsAssignableFrom(c.ACType.ValueT.ObjectType));
            if (func != null)
            {
                ACComponent funcComp = processModule.ACUrlCommand(func.ACIdentifier) as ACComponent;
                string scaleACUrl = funcComp?.ExecuteMethod(PAFBakerySourDoughProducing.MN_GetFermentationStarterScaleACUrl) as string;

                ACComponent scale = funcComp.ACUrlCommand(scaleACUrl) as ACComponent;
                if (scale == null)
                {
                    //TODO: error
                    return;
                }
                SourDoughScale = scale;
            }

            ProcessModuleOrderInfo = processModule.GetPropertyNet("OrderInfo") as IACContainerTNet<string>;
            if (ProcessModuleOrderInfo == null)
            {
                //error
                return;
            }

            ProcessModuleOrderInfo.PropertyChanged += ProcessModuleOrderInfo_PropertyChanged;
            string orderInfo = ProcessModuleOrderInfo.ValueT;
            HandleOrderInfoPropChanged(orderInfo);
        }

        private void ProcessModuleOrderInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                string orderInfo = ProcessModuleOrderInfo.ValueT;
                Task.Run(() => HandleOrderInfoPropChanged(orderInfo));
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
                ACComponent pwGroup = Root.ACUrlCommand(pwGroupACUrl) as ACComponent;

                if (pwGroup == null)
                {
                    //todo: error
                    return;
                }

                IEnumerable<ACChildInstanceInfo> pwNodes = pwGroup.GetChildInstanceInfo(1, new ChildInstanceInfoSearchParam() { OnlyWorkflows = true });

                if (pwNodes == null || !pwNodes.Any())
                    return;

                ACChildInstanceInfo fermentationStarter = pwNodes.FirstOrDefault(c => _PWFermentationStarterType.IsAssignableFrom(c.ACType.ValueT.ObjectType));

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

                FermentationQuantityProp.PropertyChanged += FermentationQuantityProp_PropertyChanged;
                HandleFermentationQunatity();

            }
        }

        private void FermentationQuantityProp_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                Task.Run(() => HandleFermentationQunatity());
            }
        }

        private void HandleFermentationQunatity()
        {
            if (FermentationQuantityProp.ValueT != null)
            {
                MessageItem msgItem = MessagesList.FirstOrDefault(c => c.UserAckPWNode.ValueT == FermentationStarterRef.ValueT);

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

        }

        public bool IsEnabledClean()
        {
            return true;
        }

        [ACMethodInfo("", "en{'Switch availability'}de{'Schalterverfügbarkeit'}", 802, true)]
        public void SwitchAvailability()
        {

        }

        public bool IsEnabledSwitchAvailability()
        {
            return true;
        }

        #endregion
    }
}
