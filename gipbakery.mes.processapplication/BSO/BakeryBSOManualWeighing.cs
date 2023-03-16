using gip.bso.manufacturing;
using gip.core.autocomponent;
using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using vd = gip.mes.datamodel;
using System.Text;
using System.Threading.Tasks;
using gip.mes.processapplication;
using gip.mes.facility;
using System.Data.Entity.Core.Mapping;
using gip.mes.datamodel;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioManufacturing, "en{'Manual weighing'}de{'Handverwiegung'}", Global.ACKinds.TACBSO, Global.ACStorableTypes.NotStorable, true, true, SortIndex = 100)]
    public class BakeryBSOManualWeighing : BSOManualWeighing
    {
        #region c'tors

        public BakeryBSOManualWeighing(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {

        }

        public override bool ACInit(Global.ACStartTypes startChildMode = Global.ACStartTypes.Automatic)
        {
            return base.ACInit(startChildMode);
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            _MatNoWarmWater = null;
            _MatNoColdWater = null;

            if (_FloorScaleDosingInfo != null)
            {
                _FloorScaleDosingInfo.PropertyChanged -= _FloorScaleDosingInfo_PropertyChanged;
                _FloorScaleDosingInfo = null;
            }

            return base.ACDeInit(deleteACClassTask);
        }

        #endregion

        #region Properties

        private Type _BakeryTempCalcType = typeof(PWBakeryTempCalc);
        private Type _BakeryRecvPointType = typeof(BakeryReceivingPoint);
        private ACMonitorObject _71100_TempCalcLock = new ACMonitorObject(71100);

        public ACRef<IACComponentPWNode> _BakeryTempCalculator;

        private bool _IsTempCalcNotNull;

        [ACPropertyInfo(9999)]
        public IACComponentPWNode CurrentBakeryTempCalc
        {
            get
            {
                using (ACMonitor.Lock(_71100_TempCalcLock))
                {
                    return _BakeryTempCalculator?.ValueT;
                }
            }
        }

        private IACContainerTNet<ACStateEnum> BakeryTempCalcACState
        {
            get;
            set;
        }

        private short _BakeryTempCalcACState;

        private IACContainerTNet<string> TempCalcResultMessage
        {
            get;
            set;
        }

        private IACContainerTNet<bool> IsCoverUpDown;

        private IACContainerTNet<RecvPointDosingInfoEnum> _FloorScaleDosingInfo;

        private CoverFlourButtonEnum _CoverFlourBtnMode;
        [ACPropertyInfo(9999)]
        public CoverFlourButtonEnum CoverFlourBtnMode
        {
            get => _CoverFlourBtnMode;
            set
            {
                _CoverFlourBtnMode = value;
                OnPropertyChanged("CoverFlourBtnMode");
            }
        }

        public bool _BtnFlourBlink;
        [ACPropertyInfo(9999)]
        public bool BtnFlourBlink
        {
            get => _BtnFlourBlink;
            set
            {
                //if (_BtnFlourBlink && !value)
                //{
                //    if (MessagesListSafe.Any(c => c.UserAckPWNode.ValueT.ACIdentifier.Contains(nameof(PWBakeryFlourDischargingAck))))
                //        return;
                //}

                _BtnFlourBlink = value;
                OnPropertyChanged();
            }
        }

        private RecvPointDosingInfoEnum _RecvPointDosingInfo;
        public RecvPointDosingInfoEnum RecvPointDosingInfo
        {
            get => _RecvPointDosingInfo;
            set
            {
                _RecvPointDosingInfo = value;
                OnPropertyChanged();
            }
        }

        private string _MatNoColdWater, _MatNoWarmWater;

        #region Properties => Temperature dialog

        private bool _ParamChanged = false;

        private bool _IsOnlyWaterTemperatureCalculation;
        [ACPropertyInfo(804)]
        public bool IsOnlyWaterTemperatureCalculation
        {
            get => _IsOnlyWaterTemperatureCalculation;
            set
            {
                _IsOnlyWaterTemperatureCalculation = value;

                if (WaterTargetTemperature < 0.0001 && WaterTargetTemperature > -0.0001)
                {
                    var bakeryTempCalc = CurrentBakeryTempCalc;
                    if (bakeryTempCalc != null)
                    {
                        var temp = bakeryTempCalc.ACUrlCommand(nameof(PWBakeryTempCalc.WaterCalcResult)) as double?;
                        if (temp != null && temp.Value > 0.001)
                        {
                            WaterTargetTemperature = Math.Round(temp.Value,1);
                        }
                    }
                }

                OnPropertyChanged();
            }
        }

        private bool _IsDoughTemperatureCalculation;
        [ACPropertyInfo(805)]
        public bool IsDoughTemperatureCalculation
        {
            get => _IsDoughTemperatureCalculation;
            set
            {
                _IsDoughTemperatureCalculation = value;
                _ParamChanged = true;
                OnPropertyChanged();
            }
        }

        private double _DoughTargetTemperature;
        [ACPropertyInfo(800)]
        public double DoughTargetTemperature
        {
            get => _DoughTargetTemperature;
            set
            {
                _DoughTargetTemperature = value;
                OnPropertyChanged();
            }
        }

        private double _DoughCorrTemperature;
        [ACPropertyInfo(801)]
        public double DoughCorrTemperature
        {
            get => _DoughCorrTemperature;
            set
            {
                _DoughCorrTemperature = value;
                _ParamChanged = true;
                OnPropertyChanged();
            }
        }

        private double _WaterTargetTemperature;
        [ACPropertyInfo(802)]
        public double WaterTargetTemperature
        {
            get => _WaterTargetTemperature;
            set
            {
                _WaterTargetTemperature = value;
                _ParamChanged = true;
                OnPropertyChanged();
            }
        }

        [ACPropertyInfo(803)]
        public double? CityWaterTemp
        {
            get;
            set;
        }

        [ACPropertyInfo(804)]
        public double? ColdWaterTemp
        {
            get;
            set;
        }

        [ACPropertyInfo(805)]
        public double? WarmWaterTemp
        {
            get;
            set;
        }

        [ACPropertyInfo(806)]
        public double? IceTemp
        {
            get;
            set;
        }

        [ACPropertyInfo(807)]
        public double? KneedingRiseTemp
        {
            get;
            set;
        }

        private double _NewWaterQuantity;
        [ACPropertyInfo(808)]
        public double NewWaterQuantity
        {
            get => _NewWaterQuantity;
            set
            {
                if (_NewWaterQuantity != value)
                {
                    double valueForCompare = _NewWaterQuantity;
                    if (WaterQuantityFromPartslist > 0.0001)
                        valueForCompare = WaterQuantityFromPartslist;

                    double diff = Math.Abs(valueForCompare - value);

                    if (diff <= WaterCorrectionDiffMax)
                    {
                        _NewWaterQuantity = value;
                        OnPropertyChanged();
                        _ParamChanged = true;
                    }
                }
            }
        }

        public double WaterQCorrectionStep
        {
            get;
            set;
        }

        public double WaterCorrectionDiffMax
        {
            get;
            set;
        }

        private double WaterQuantityFromPartslist
        {
            get;
            set;
        }


        private IACContainerTNet<double> _ContentWeightOfContainer = null;
        [ACPropertyInfo(608, "", "en{'Weight of content in bin'}de{'Gewicht des Inhaltes des Behälters'}", IsProxyProperty = true)]
        public IACContainerTNet<double> ContentWeightOfContainer
        {
            get
            {
                return _ContentWeightOfContainer;
            }
            set
            {
                _ContentWeightOfContainer = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Properties => SingleDosing dialog

        private double? _SingleDosTargetTemperature;
        [ACPropertyInfo(850, "", "en{'Temperature'}de{'Temperatur'}")]
        public double? SingleDosTargetTemperature
        {
            get => _SingleDosTargetTemperature;
            set
            {
                _SingleDosTargetTemperature = value;
                OnPropertyChanged();
            }
        }

        private bool _DischargeOverHose;
        [ACPropertyInfo(850, "", "en{'Over hose'}de{'Über Schlauch'}")]
        public bool DischargeOverHose
        {
            get => _DischargeOverHose;
            set
            {
                _DischargeOverHose = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #endregion

        #region Methods

        public override void Activate(ACComponent selectedProcessModule)
        {
            UnloadBakeryTempCalc();
            ResetTemperatureDialogParam();
            base.Activate(selectedProcessModule);

            CoverFlourBtnMode = CoverFlourButtonEnum.None;

            gip.core.datamodel.ACClass recvPointClass = null;

            //var contextIplus = DatabaseApp.ContextIPlus;
            using (Database db = new gip.core.datamodel.Database())
            {
                recvPointClass = selectedProcessModule?.ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(db);
                if (recvPointClass != null && _BakeryRecvPointType.IsAssignableFrom(recvPointClass.ObjectType))
                {
                    var isCoverUpDown = selectedProcessModule.GetPropertyNet(nameof(BakeryReceivingPoint.IsCoverDown)) as IACContainerTNet<bool>;
                    if (isCoverUpDown != null)
                    {
                        bool? isBounded = selectedProcessModule.ExecuteMethod(nameof(BakeryReceivingPoint.IsCoverDownPropertyBounded)) as bool?;
                        if (isBounded.HasValue && isBounded.Value)
                        {
                            bool cover = false;

                            var config = recvPointClass.ConfigurationEntries.FirstOrDefault(c => c.KeyACUrl == recvPointClass.ACConfigKeyACUrl 
                                                                                              && c.LocalConfigACUrl == nameof(BakeryReceivingPoint.WithCover));
                            if (config != null)
                            {
                                bool? val = config.Value as bool?;
                                if (val.HasValue)
                                    cover = val.Value;
                            }
                            else
                            {
                                Messages.Error(this, "Can not find the configuration property WithCover on a receiving point!");
                            }

                            //bool CanAckInAdvance = false;
                            //config = recvPointClass.ConfigurationEntries.FirstOrDefault(c => c.KeyACUrl == recvPointClass.ACConfigKeyACUrl && c.LocalConfigACUrl == "CanAckInAdvance");
                            //if (config != null)
                            //{
                            //    bool? val = config.Value as bool?;
                            //    if (val.HasValue)
                            //        CanAckInAdvance = val.Value;
                            //}

                            if (cover)
                                CoverFlourBtnMode = CoverFlourButtonEnum.CoverUpDownVisible;
                            else
                                CoverFlourBtnMode = CoverFlourButtonEnum.FlourDischargeVisible;

                            IsCoverUpDown = isCoverUpDown;
                        }
                    }

                    ContentWeightOfContainer = selectedProcessModule.GetPropertyNet(nameof(BakeryReceivingPoint.ContentWeightOfContainer)) as IACContainerTNet<double>;
                }
            }
        }

        public override void OnGetPWGroup(IACComponentPWNode pwGroup)
        {
            IEnumerable<ACChildInstanceInfo> pwNodes;

            using (Database db = new Database())
            {
                var pwClass = db.ACClass.FirstOrDefault(c => c.ACProject.ACProjectTypeIndex == (short)Global.ACProjectTypes.ClassLibrary &&
                                                                                                c.ACIdentifier == PWBakeryTempCalc.PWClassName);
                ACRef<gip.core.datamodel.ACClass> refClass = new ACRef<gip.core.datamodel.ACClass>(pwClass, true);
                pwNodes = pwGroup.GetChildInstanceInfo(1, new ChildInstanceInfoSearchParam() { OnlyWorkflows = true, TypeOfRoots = refClass });
                refClass.Detach();
            }

            if (pwNodes == null || !pwNodes.Any())
                return;

            ACChildInstanceInfo node = pwNodes.FirstOrDefault();

            IACComponentPWNode pwNode = pwGroup.ACUrlCommand(node.ACUrlParent + "\\" + node.ACIdentifier) as IACComponentPWNode;
            if (pwNode == null)
            {
                //Error50290: The user does not have access rights for class PWManualWeighing ({0}).
                // Der Benutzer hat keine Zugriffsrechte auf Klasse PWManualWeighing ({0}).
                Messages.Error(this, "Error50290", false, node.ACUrlParent + "\\" + node.ACIdentifier);
                return;
            }

            using (ACMonitor.Lock(_71100_TempCalcLock))
            {
                _BakeryTempCalculator = new ACRef<IACComponentPWNode>(pwNode, this);
                _IsTempCalcNotNull = true;
            }

            BakeryTempCalcACState = pwNode.GetPropertyNet(Const.ACState) as IACContainerTNet<ACStateEnum>;
            if (BakeryTempCalcACState != null)
            {
                _BakeryTempCalcACState = (short)BakeryTempCalcACState.ValueT;
            }

            TempCalcResultMessage = pwNode.GetPropertyNet(nameof(PWBakeryTempCalc.TemperatureCalculationResult)) as IACContainerTNet<string>;

            if (TempCalcResultMessage != null)
            {
                HandleTempCalcResultMsg(TempCalcResultMessage.ValueT);
                TempCalcResultMessage.PropertyChanged += TempCalcResultMessage_PropertyChanged;
            }

            if (BakeryTempCalcACState != null)
            {
                HandleTempCalcACState(BakeryTempCalcACState.ValueT);
                BakeryTempCalcACState.PropertyChanged += BakeryTempCalcACState_PropertyChanged;
            }

            GetTemperaturesFromPWBakeryTempCalc(pwNode);

            if (MessagesListSafe.Any(c => c.UserAckPWNode != null && c.UserAckPWNode.ValueT.ACIdentifier.Contains(nameof(PWBakeryFlourDischargingAck))))
            {
                BtnFlourBlink = true;
            }
        }

        public override void UnloadWFNode()
        {
            if (BakeryTempCalcACState != null)
            {
                BakeryTempCalcACState.PropertyChanged -= BakeryTempCalcACState_PropertyChanged;
                BakeryTempCalcACState = null;
            }

            if (TempCalcResultMessage != null)
            {
                TempCalcResultMessage.PropertyChanged -= TempCalcResultMessage_PropertyChanged;
                TempCalcResultMessage = null;
            }

            BtnFlourBlink = false;

            base.UnloadWFNode();
        }

        private void BakeryTempCalcACState_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                var senderProp = sender as IACContainerTNet<ACStateEnum>;
                ACStateEnum acState = senderProp.ValueT;
                if (senderProp != null)
                {
                    _BakeryTempCalcACState = (short)acState;
                    ParentBSOWCS.ApplicationQueue.Add(() => HandleTempCalcACState(acState));
                }
            }
        }

        private void TempCalcResultMessage_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                var prop = sender as IACContainerTNet<string>;
                if (prop != null)
                    ParentBSOWCS?.ApplicationQueue.Add(() => HandleTempCalcResultMsg(prop.ValueT));
            }
        }

        private void HandleTempCalcACState(ACStateEnum acState)
        {
            IACComponentPWNode tempCalc = CurrentBakeryTempCalc;

            if (tempCalc == null)
            {
                //Error50457: The workflow node Bakery temperature calculator is not available.
                Messages.LogError(this.GetACUrl(), "HandleTempCalcACState(10)", "The workflow node Bakery temperature calculator is not available.");
            }

            if (acState != ACStateEnum.SMRunning)
            {
                var existingMessageItems = MessagesListSafe.Where(c => c.UserAckPWNode != null && c.UserAckPWNode.ValueT == tempCalc).ToArray();
                if (existingMessageItems != null && existingMessageItems.Any())
                {
                    foreach (MessageItem mItem in existingMessageItems)
                    {
                        RemoveFromMessageList(mItem);
                    }
                    RefreshMessageList();
                }
                return;
            }

            ACMethod acMethod = tempCalc?.ACUrlCommand("MyConfiguration") as ACMethod;
            if (acMethod == null)
            {
                //Error50288: The configuration(ACMethod) for the workflow node cannot be found!
                // Die Konfiguration (ACMethod) für den Workflow-Knoten kann nicht gefunden werden!
                Messages.Error(this, "Error50288");
                return;
            }

            ACValue param = acMethod.ParameterValueList.GetACValue("AskUserIsWaterNeeded");
            if (param != null && param.ParamAsBoolean)
            {
                bool? userResponse = tempCalc.ExecuteMethod(nameof(PWBakeryTempCalc.GetUserResponse)) as bool?;
                if (userResponse.HasValue)
                {
                    return;
                }

                var existingMessageItems = MessagesListSafe.Where(c => c.UserAckPWNode != null && c.UserAckPWNode.ValueT == tempCalc).ToArray();
                if (existingMessageItems != null)
                {
                    foreach (MessageItem mItem in existingMessageItems)
                    {
                        RemoveFromMessageList(mItem);
                    }
                }

                //Question50071: Is water needed?
                MessageItem msgItem = new MessageItem(tempCalc, this, eMsgLevel.Question);
                msgItem.Message = Root.Environment.TranslateMessage(this, "Question50071");

                AddToMessageList(msgItem);
                RefreshMessageList();
            }
        }

        private void HandleTempCalcResultMsg(string msg)
        {
            if (_BakeryTempCalcACState == (short)ACStateEnum.SMRunning)
            {
                IACComponentPWNode tempCalc = CurrentBakeryTempCalc;
                if (tempCalc == null)
                {
                    //Error50457: The workflow node Bakery temperature calculator is not available.
                    Messages.LogError(this.GetACUrl(), "HandleTempCalcResultMsg(10)", "The workflow node Bakery temperature calculator is not available.");
                }

                var existingMessageItems = MessagesListSafe.Where(c => c.UserAckPWNode != null && c.UserAckPWNode.ValueT == tempCalc).ToArray();
                if (existingMessageItems != null)
                {
                    foreach (MessageItem mItem in existingMessageItems)
                    {
                        RemoveFromMessageList(mItem);
                    }
                }

                if (string.IsNullOrEmpty(msg))
                    return;

                MessageItem msgItem = new MessageItem(tempCalc, this);
                msgItem.Message = msg;

                AddToMessageList(msgItem);
                RefreshMessageList();
            }
        }

        private void GetTemperaturesFromPWBakeryTempCalc(IACComponentPWNode tempCalc)
        {
            ACMethod config = tempCalc.ACUrlCommand("MyConfiguration") as ACMethod;
            if (config != null)
            {
                ACValue dTemp = config.ParameterValueList.GetACValue("DoughTemp");
                if (dTemp != null)
                {
                    DoughTargetTemperature = dTemp.ParamAsDouble;
                }

                ACValue wTemp = config.ParameterValueList.GetACValue("WaterTemp");
                if (wTemp != null && wTemp.Value != null)
                {
                    WaterTargetTemperature = wTemp.ParamAsDouble;
                }

                ACValue onlyForWaterCalc = config.ParameterValueList.GetACValue("UseWaterTemp");
                if (onlyForWaterCalc != null)
                {
                    IsOnlyWaterTemperatureCalculation = onlyForWaterCalc.ParamAsBoolean;
                    if (IsOnlyWaterTemperatureCalculation)
                        IsDoughTemperatureCalculation = false;
                    else
                        IsDoughTemperatureCalculation = true;
                }

                ACValue corrStep = config.ParameterValueList.GetACValue("WaterQCorrectionStep");
                if (corrStep != null)
                    WaterQCorrectionStep = corrStep.ParamAsDouble;
                else
                    WaterQCorrectionStep = 0.5;

                //TODO: maximum water correction difference depends on original quantity
                ACValue corrMax = config.ParameterValueList.GetACValue("MaxWaterCorrectionDiff");
                if (corrMax != null)
                {
                    WaterCorrectionDiffMax = corrMax.ParamAsDouble;
                    if (WaterQCorrectionStep > WaterCorrectionDiffMax)
                    {
                        WaterQCorrectionStep = WaterCorrectionDiffMax;
                    }
                }
                else
                    WaterCorrectionDiffMax = 10;
                
            }
        }

        private void UnloadBakeryTempCalc()
        {
            if (BakeryTempCalcACState != null)
            {
                BakeryTempCalcACState.PropertyChanged -= BakeryTempCalcACState_PropertyChanged;
                BakeryTempCalcACState = null;
            }

            if (TempCalcResultMessage != null)
            {
                TempCalcResultMessage.PropertyChanged -= TempCalcResultMessage_PropertyChanged;
                TempCalcResultMessage = null;
            }

            using (ACMonitor.Lock(_71100_TempCalcLock))
            {
                if (_BakeryTempCalculator != null)
                {
                    _BakeryTempCalculator.Detach();
                    _BakeryTempCalculator = null;
                    _IsTempCalcNotNull = false;
                }
            }
        }

        private void ResetTemperatureDialogParam()
        {
            IsOnlyWaterTemperatureCalculation = false;
            IsDoughTemperatureCalculation = false;
            DoughTargetTemperature = 0;
            DoughCorrTemperature = 0;
            WaterTargetTemperature = 0;
        }

        [ACMethodInfo("", "en{'Cover up/down'}de{'Abdeckung oben/unten'}", 9999)]
        public void RecvPointCoverUpDown()
        {
            if (IsEnabledRecvPointCoverUpDown())
            {
                if (CoverFlourBtnMode == CoverFlourButtonEnum.CoverUpDownVisible)
                {
                    IsCoverUpDown.ValueT = !IsCoverUpDown.ValueT;
                }
                else if (CoverFlourBtnMode == CoverFlourButtonEnum.FlourDischargeVisible)
                {
                    IsCoverUpDown.ValueT = !IsCoverUpDown.ValueT;
                    //IsCoverUpDown.ValueT = true;
                }
            }
        }

        public bool IsEnabledRecvPointCoverUpDown()
        {
            return ParentBSOWCS != null && ParentBSOWCS.IsCurrentUserConfigured && IsCoverUpDown != null && CoverFlourBtnMode > CoverFlourButtonEnum.None;
        }

        #region Methods => Temperature dialog

        [ACMethodInfo("", "en{'Temperature Dialog'}de{'Temperatur-Dialog'}", 800)]
        public void ShowTemperaturesDialog()
        {
            ACComponent currentProcessModule = CurrentProcessModule;
            if (currentProcessModule == null)
            {
                //Error50283: The manual weighing module can not be initialized. The property CurrentProcessModule is null.
                // Die Handverwiegungsstation konnte nicht initialisiert werden. Die Eigenschaft CurrentProcessModule ist null.
                Messages.Error(this, "Error50283");
                return;
            }

            var corrTemp = currentProcessModule.ACUrlCommand("DoughCorrTemp") as double?;
            if (corrTemp.HasValue)
                DoughCorrTemperature = corrTemp.Value;

            IACComponentPWNode tempCalc = CurrentBakeryTempCalc;

            ACValueList watersTempInCalc = tempCalc?.ExecuteMethod(nameof(PWBakeryTempCalc.GetTemperaturesUsedInCalc)) as ACValueList;
            if (watersTempInCalc != null)
            {
                var cityWater = watersTempInCalc.GetACValue(WaterType.CityWater.ToString());
                if (cityWater != null)
                {
                    CityWaterTemp = cityWater.Value as double?;
                }

                var coldWater = watersTempInCalc.GetACValue(WaterType.ColdWater.ToString());
                if (coldWater != null)
                {
                    ColdWaterTemp = coldWater.Value as double?;
                }

                var warmWater = watersTempInCalc.GetACValue(WaterType.WarmWater.ToString());
                if (warmWater != null)
                {
                    WarmWaterTemp = warmWater.Value as double?;
                }

                var ice = watersTempInCalc.GetACValue(WaterType.DryIce.ToString());
                if (ice != null)
                {
                    IceTemp = ice.Value as double?;
                }

                var kneedingRiseTemp = watersTempInCalc.GetACValue(PWBakeryTempCalc.KneedingRiseTemp);
                if (kneedingRiseTemp != null)
                {
                    KneedingRiseTemp = kneedingRiseTemp.Value as double?;
                }

                var waterTargetQFromPL = watersTempInCalc.GetACValue(PWBakeryTempCalc.ParamPLWaterTQ);
                if (waterTargetQFromPL != null)
                {
                    WaterQuantityFromPartslist = waterTargetQFromPL.ParamAsDouble;
                }
            }

            double? waterTotalQ = tempCalc?.ACUrlCommand(nameof(PWBakeryTempCalc.WaterTotalQuantity)) as double?;
            if (waterTotalQ.HasValue)
            {
                _NewWaterQuantity = waterTotalQ.Value;
            }

            _ParamChanged = false;

            ShowDialog(this, "TemperaturesDialog");

            CityWaterTemp = null;
            ColdWaterTemp = null;
            WarmWaterTemp = null;
            IceTemp = null;
            KneedingRiseTemp = null;
        }

        public bool IsEnabledShowTemperaturesDialog()
        {
            return !IsCurrentProcessModuleNull;
        }

        [ACMethodInfo("", "en{'Increase Dough-temperature'}de{'Erhöhe Teigtemperatur'}", 880, true)]
        public void DoughTempCorrPlus()
        {
            DoughCorrTemperature++;
        }

        [ACMethodInfo("", "en{'Reduce Dough-temperature'}de{'Reduziere Teigtemperatur'}", 880, true)]
        public void DoughTempCorrMinus()
        {
            DoughCorrTemperature--;
        }

        [ACMethodInfo("", "en{'Increase Water-temperature'}de{'Erhöhe Wassertemperatur'}", 880, true)]
        public void WaterTempPlus()
        {
            WaterTargetTemperature++;
        }

        [ACMethodInfo("", "en{'Reduce Water-temperature'}de{'Reduziere Wassertemperatur'}", 880, true)]
        public void WaterTempMinus()
        {
            WaterTargetTemperature--;
        }

        [ACMethodInfo("", "en{'Apply'}de{'Anwenden'}", 800)]
        public void ApplyTemperatures()
        {
            if (_ParamChanged)
            {
                IACComponentPWNode tempCalc = CurrentBakeryTempCalc;
                if (tempCalc == null)
                {
                    //Error50457: The workflow node Bakery temperature calculator is not available.
                    Messages.Error(this, "Error50457");
                    return;
                }

                ACComponent currentProcessModule = CurrentProcessModule;
                if (currentProcessModule == null)
                {
                    //Error50283: The manual weighing module can not be initialized. The property CurrentProcessModule is null.
                    // Die Handverwiegungsstation konnte nicht initialisiert werden. Die Eigenschaft CurrentProcessModule ist null.
                    Messages.Error(this, "Error50283");
                    return;
                }

                currentProcessModule.ACUrlCommand("DoughCorrTemp", DoughCorrTemperature); //Save dough correct temperature on bakery recieving point
                tempCalc.ExecuteMethod(nameof(PWBakeryTempCalc.SaveWorkplaceTemperatureSettings), WaterTargetTemperature, IsOnlyWaterTemperatureCalculation, NewWaterQuantity);//TODO parameters
            }
            CloseTopDialog();
            _ParamChanged = false;
        }

        public bool IsEnabledApplyTemperatures()
        {
            return _IsTempCalcNotNull && !IsCurrentProcessModuleNull;
        }

        [ACMethodInfo("", "en{'Recalculate'}de{'Neu berechnen'}", 801)]
        public void RecalcTemperatures()
        {
            IACComponentPWNode tempCalc = CurrentBakeryTempCalc;
            if (tempCalc == null)
            {
                //Error50457: The workflow node Bakery temperature calculator is not available.
                Messages.Error(this, "Error50457");
                return;
            }

            ACComponent currentProcessModule = CurrentProcessModule;
            if (currentProcessModule == null)
            {
                //Error50283: The manual weighing module can not be initialized. The property CurrentProcessModule is null.
                // Die Handverwiegungsstation konnte nicht initialisiert werden. Die Eigenschaft CurrentProcessModule ist null.
                Messages.Error(this, "Error50283");
                return;
            }

            currentProcessModule.ACUrlCommand("DoughCorrTemp", DoughCorrTemperature); //Save dough correct temperature on bakery recieving point
            tempCalc.ExecuteMethod(nameof(PWBakeryTempCalc.SaveWorkplaceTemperatureSettings), WaterTargetTemperature, IsOnlyWaterTemperatureCalculation, NewWaterQuantity);//TODO parameters
            _ParamChanged = false;
        }

        public bool IsEnabledRecalcTemperatures()
        {
            return _IsTempCalcNotNull && !IsCurrentProcessModuleNull && _ParamChanged;
        }

        [ACMethodInfo("", "en{'Increase Water quantity'}de{'Erhöhe Wassermenge'}", 880, true)]
        public void WaterQuantityPlus()
        {
            NewWaterQuantity += WaterQCorrectionStep;
        }

        [ACMethodInfo("", "en{'Reduce Water quantity'}de{'Reduziere Wassermenge'}", 880, true)]
        public void WaterQuantityMinus()
        {
            NewWaterQuantity -= WaterQCorrectionStep;
        }

        #endregion

        #region Methods => SingleDosing

        public override bool OnPreStartWorkflow(vd.DatabaseApp dbApp, gip.mes.datamodel.Picking picking, List<SingleDosingConfigItem> configItems, Route validRoute, gip.core.datamodel.ACClassWF rootWF)
        {
            base.OnPreStartWorkflow(dbApp, picking, configItems, validRoute, rootWF);

            if (SingleDosTargetTemperature.HasValue)
            {
                SingleDosingConfigItem configItem = configItems.FirstOrDefault(c => c.PWGroup.ACClassWF_ParentACClassWF
                                                                                             .Any(x => _BakeryTempCalcType.IsAssignableFrom(x.PWACClass.ObjectType)));

                if (configItem != null)
                {
                    gip.core.datamodel.ACClassWF tempCalc = configItem.PWGroup.ACClassWF_ParentACClassWF.FirstOrDefault(x => _BakeryTempCalcType.IsAssignableFrom(x.PWACClass.ObjectType));
                    if (tempCalc == null)
                        return false;

                    string preConfigACUrl = configItem.PreConfigACUrl + "\\";
                    string propertyACUrl = string.Format("{0}\\{1}\\WaterTemp", tempCalc.ConfigACUrl, ACStateEnum.SMStarting);

                    // Water temp 
                    IACConfig waterTempConfig = picking.ConfigurationEntries.FirstOrDefault(c => c.PreConfigACUrl == preConfigACUrl && c.LocalConfigACUrl == propertyACUrl);
                    if (waterTempConfig == null)
                    {
                        waterTempConfig = InsertTemperatureConfiguration(propertyACUrl, preConfigACUrl, "WaterTemp", tempCalc, picking);
                    }

                    if (waterTempConfig == null)
                    {
                        //The insert process of water temperature configuration for single dosing is failed.
                        return false;
                    }
                    else
                        waterTempConfig.Value = SingleDosTargetTemperature;

                    // Water temp 
                    propertyACUrl = string.Format("{0}\\{1}\\UseWaterTemp", tempCalc.ConfigACUrl, ACStateEnum.SMStarting);
                    IACConfig useOnlyForWaterTempCalculation = picking.ConfigurationEntries.FirstOrDefault(c => c.PreConfigACUrl == preConfigACUrl
                                                                                                             && c.LocalConfigACUrl == propertyACUrl);
                    if (useOnlyForWaterTempCalculation == null)
                    {
                        useOnlyForWaterTempCalculation = InsertTemperatureConfiguration(propertyACUrl, preConfigACUrl, "UseWaterTemp", tempCalc, picking);
                    }

                    if (useOnlyForWaterTempCalculation == null)
                    {
                        //The insert process of use water temperature configuration for single dosing is failed.
                    }
                    else
                        useOnlyForWaterTempCalculation.Value = true;

                    var msg = dbApp.ACSaveChanges();
                    if (msg != null)
                        Messages.Msg(msg);

                }
            }

            bool result = AddDischargingConfig(dbApp, picking, configItems, validRoute, rootWF);

            return result;
        }

        private bool AddDischargingConfig(vd.DatabaseApp dbApp, vd.Picking picking, List<SingleDosingConfigItem> configItems, Route validRoute, gip.core.datamodel.ACClassWF rootWF)
        {
            if (!DischargeOverHose)
                return true;

            SingleDosingConfigItem configItem = configItems.FirstOrDefault(c => c.PWGroup.ACClassWF_ParentACClassWF
                                                                                 .Any(x => _BakeryTempCalcType.IsAssignableFrom(x.PWACClass.ObjectType)));
            if (configItem != null)
            {
                gip.core.datamodel.ACClassWF discharging = configItem.PWGroup.ACClassWF_ParentACClassWF
                                                                     .FirstOrDefault(c => c.PWACClass.ACIdentifier
                                                                                           .Contains(PWBakeryDischargingSingleDos.PWClassName));

                var dosings = configItem.PWGroup.ACClassWF_ParentACClassWF.Where(c => c.PWACClass.ACIdentifier.Contains(nameof(PWDosing)));

                if (discharging != null)
                {
                    ACComponent currentProcessModule = CurrentProcessModule;
                    if (currentProcessModule == null)
                    {
                        //Error50283: The manual weighing module can not be initialized. The property CurrentProcessModule is null.
                        // Die Handverwiegungsstation konnte nicht initialisiert werden. Die Eigenschaft CurrentProcessModule ist null.
                        Messages.Error(this, "Error50283");
                        return false;
                    }

                    gip.core.datamodel.ACClass compClass = currentProcessModule?.ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(dbApp.ContextIPlus);

                    var config = compClass?.ConfigurationEntries.FirstOrDefault(c => c.KeyACUrl == compClass.ACConfigKeyACUrl && c.LocalConfigACUrl == "HoseDestination");
                    if (config == null)
                    {
                        //Error50458: Can not find the configuration for hose destination on the receiving point. Please configure the hose destination.
                        Messages.Error(this, "Error50458");
                        return false;
                    }

                    InsertOverHoseConfiguration(discharging, rootWF, picking, config.Value);

                    if (dosings != null)
                    {
                        foreach (var dos in dosings)
                        {
                            InsertOverHoseConfiguration(dos, rootWF, picking, config.Value);
                        }
                    }

                    Msg msg = dbApp.ACSaveChanges();
                    if (msg != null)
                    {
                        Messages.Msg(msg);
                        return false;
                    }
                }
            }

            return true;
        }

        private bool InsertOverHoseConfiguration(gip.core.datamodel.ACClassWF pwNode, gip.core.datamodel.ACClassWF rootWF, gip.mes.datamodel.Picking picking, object configValue)
        {
            if (pwNode == null || rootWF == null)
                return false;

            ACMethod acMethod = pwNode.RefPAACClassMethod.ACMethod;

            if (acMethod == null)
                return false;

            string preConfigACUrl = rootWF.ConfigACUrl + "\\";
            string configACUrl = string.Format("{0}\\{1}\\Destination", pwNode.ConfigACUrl, acMethod.ACIdentifier);

            IACConfig targetConfig = picking.ConfigurationEntries.FirstOrDefault(c => c.PreConfigACUrl == preConfigACUrl && c.LocalConfigACUrl == configACUrl);

            using (Database db = new gip.core.datamodel.Database())
            {
                if (targetConfig == null)
                {
                    ACConfigParam param = new ACConfigParam()
                    {
                        ACIdentifier = "Destination",
                        ACCaption = acMethod.GetACCaptionForACIdentifier("Destination"),
                        ValueTypeACClassID = db.GetACType(nameof(Int16)).ACClassID,
                        ACClassWF = pwNode
                    };

                    targetConfig = ConfigManagerIPlus.ACConfigFactory(picking, param, preConfigACUrl, configACUrl, null);
                    param.ConfigurationList.Insert(0, targetConfig);

                    picking.ConfigurationEntries.Append(targetConfig);
                }
            }

            targetConfig.Value = configValue;

            return true;
        }

        private IACConfig InsertTemperatureConfiguration(string propertyACUrl, string preConfigACUrl, string paramACIdentifier, gip.core.datamodel.ACClassWF acClassWF, IACConfigStore configStore)
        {
            ACMethod acMethod = acClassWF.PWACClass.ACClassMethod_ACClass.FirstOrDefault(c => c.ACIdentifier == ACStateConst.SMStarting)?.ACMethod;
            if (acMethod != null)
            {
                ACValue valItem = acMethod.ParameterValueList.GetACValue(paramACIdentifier);

                ACConfigParam param = new ACConfigParam()
                {
                    ACIdentifier = valItem.ACIdentifier,
                    ACCaption = acMethod.GetACCaptionForACIdentifier(valItem.ACIdentifier),
                    ValueTypeACClassID = valItem.ValueTypeACClass.ACClassID,
                    ACClassWF = acClassWF
                };

                IACConfig configParam = ConfigManagerIPlus.ACConfigFactory(configStore, param, preConfigACUrl, propertyACUrl, null);
                param.ConfigurationList.Insert(0, configParam);

                configStore.ConfigurationEntries.Append(configParam);

                return configParam;
            }

            return null;
        }

        public override SingleDosingItems OnFilterSingleDosingItems(IEnumerable<SingleDosingItem> items)
        {
            if (string.IsNullOrEmpty(_MatNoColdWater) || string.IsNullOrEmpty(_MatNoWarmWater))
            {
                ACComponent currentProcessModule = CurrentProcessModule;

                if (currentProcessModule != null)
                {
                    gip.core.datamodel.ACClass recvPointClass = null;
                    using (Database db = new gip.core.datamodel.Database())
                    {
                        recvPointClass = currentProcessModule?.ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(db);

                        if (recvPointClass != null && _BakeryRecvPointType.IsAssignableFrom(recvPointClass.ObjectType))
                        {
                            MaterialTemperature warmWater, coldWater;

                            ACValueList waters = currentProcessModule.ExecuteMethod(nameof(BakeryReceivingPoint.GetWaterComponentsFromTempService)) as ACValueList;
                            if (waters != null && waters.Any())
                            {
                                IEnumerable<MaterialTemperature> waterTemps = waters.Select(c => c.Value as MaterialTemperature);
                                warmWater = waterTemps.FirstOrDefault(c => c.Water == WaterType.WarmWater);
                                coldWater = waterTemps.FirstOrDefault(c => c.Water == WaterType.ColdWater);

                                _MatNoWarmWater = warmWater?.MaterialNo;
                                _MatNoColdWater = coldWater?.MaterialNo;
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(_MatNoColdWater) && !string.IsNullOrEmpty(_MatNoWarmWater))
            {
                return base.OnFilterSingleDosingItems(items.Where(c => c.MaterialNo != _MatNoColdWater && c.MaterialNo != _MatNoWarmWater));
            }

            return base.OnFilterSingleDosingItems(items);
        }

        public override MsgWithDetails ValidateSingleDosingStart(ACComponent currentProcessModule)
        {
            MsgWithDetails msg = null; // base.ValidateSingleDosingStart(currentProcessModule);

            if (currentProcessModule != null)
            {
                using (Database db = new gip.core.datamodel.Database())
                {
                    gip.core.datamodel.ACClass recvPointClass = currentProcessModule?.ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(db);

                    if (recvPointClass != null && _BakeryRecvPointType.IsAssignableFrom(recvPointClass.ObjectType))
                    {
                        MaterialTemperature cityWater = null;

                        ACValueList waters = currentProcessModule.ExecuteMethod(nameof(BakeryReceivingPoint.GetWaterComponentsFromTempService)) as ACValueList;
                        if (waters != null && waters.Any())
                        {
                            IEnumerable<MaterialTemperature> waterTemps = waters.Select(c => c.Value as MaterialTemperature);
                            //warmWater = waterTemps.FirstOrDefault(c => c.Water == WaterType.WarmWater);
                            //coldWater = waterTemps.FirstOrDefault(c => c.Water == WaterType.ColdWater);
                            cityWater = waterTemps.FirstOrDefault(c => c.Water == WaterType.CityWater);
                        }

                        if (cityWater != null && SelectedSingleDosingItem.MaterialNo != cityWater.MaterialNo)
                        {
                            return base.ValidateSingleDosingStart(currentProcessModule);
                        }

                        gip.core.datamodel.ACClass componentClass = currentProcessModule.ComponentClass?.FromIPlusContext<gip.core.datamodel.ACClass>(db);
                        if (componentClass == null)
                            return null;

                        gip.core.datamodel.ACClassProperty waterTankACUrlProp = componentClass.GetProperty(nameof(BakeryReceivingPoint.WaterTankACUrl));
                        if (waterTankACUrlProp != null && waterTankACUrlProp.Value != null && waterTankACUrlProp.Value is string)
                        {
                            string acUrl = waterTankACUrlProp.Value as string;
                            gip.core.datamodel.ACClass waterTank = db.ACClass.FirstOrDefault(c => c.ACURLComponentCached == acUrl);

                            double maxWeight = 0;
                            gip.core.datamodel.ACClassProperty maxWeightProp = waterTank.GetProperty(nameof(PAProcessModule.MaxWeightCapacity));
                            if (maxWeightProp != null && maxWeightProp.Value != null && maxWeightProp.Value is string)
                                maxWeight = (double)ACConvert.ChangeType(maxWeightProp.Value as string, typeof(double), true, db);

                            maxWeight = maxWeight - (maxWeight * 0.2);

                            if (SingleDosTargetQuantity > maxWeight)
                            {
                                //Error50487:The dosing quantity is {0} kg but the maximum dosing qunatity is {1} kg.
                                var msg1 = new Msg(this, eMsgLevel.Error, ClassName, "ValidateStart", 2469, "Error50487", SingleDosTargetQuantity, Math.Round(maxWeight, 2));
                                return new MsgWithDetails(new Msg[] { msg1 });
                            }
                        }
                        else
                        {
                            msg = base.ValidateSingleDosingStart(currentProcessModule);
                        }

                        if (!SingleDosTargetTemperature.HasValue)
                        {
                            //Error50489: The water target temperature is missing. Please enter the water target temperature and continue with process.
                            Msg msg1 = new Msg(this, eMsgLevel.Error, ClassName, "ValidateStart", 943, "Error50489");
                            if (msg != null)
                            {
                                msg.AddDetailMessage(msg1);
                                return msg;
                            }
                            return new MsgWithDetails(new Msg[] { msg1 });
                        }

                        double minTemp = 0;
                        double maxTemp = 80;

                        if (SingleDosTargetTemperature < minTemp || SingleDosTargetTemperature > maxTemp)
                        {
                            //Error50488 :The target water temperature is {0}°C, but minimum is {1}°C and maximum is {2}°C.
                            Msg msg1 = new Msg(this, eMsgLevel.Error, ClassName, "ValidateStart", 958, "Error50488", SingleDosTargetTemperature, minTemp, maxTemp);
                            if (msg != null)
                            {
                                msg.AddDetailMessage(msg1);
                                return msg;
                            }
                            return new MsgWithDetails(new Msg[] { msg1 });
                        }
                    }
                }
            }

            return msg;
        }

        [ACMethodInfo("", "en{'Single dosing'}de{'Einzeldosierung'}", 660)]
        public override void ShowSingleDosingDialog()
        {
            DischargeOverHose = false;
            base.ShowSingleDosingDialog();
        }

        #endregion

        public override Global.ControlModes OnGetControlModes(IVBContent vbControl)
        {
            return base.OnGetControlModes(vbControl);
        }

        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            switch (acMethodName)
            {
                case nameof(RecvPointCoverUpDown):
                    RecvPointCoverUpDown();
                    return true;

                case nameof(IsEnabledRecvPointCoverUpDown):
                    result = IsEnabledRecvPointCoverUpDown();
                    return true;

                case nameof(ShowTemperaturesDialog):
                    ShowTemperaturesDialog();
                    return true;

                case nameof(IsEnabledShowTemperaturesDialog):
                    result = IsEnabledShowTemperaturesDialog();
                    return true;

                case nameof(DoughTempCorrPlus):
                    DoughTempCorrPlus();
                    return true;

                case nameof(DoughTempCorrMinus):
                    DoughTempCorrMinus();
                    return true;

                case nameof(WaterTempPlus):
                    WaterTempPlus();
                    return true;

                case nameof(WaterTempMinus):
                    WaterTempMinus();
                    return true;

                case nameof(IsEnabledApplyTemperatures):
                    result = IsEnabledApplyTemperatures();
                    return true;

                case nameof(RecalcTemperatures):
                    RecalcTemperatures();
                    return true;

                case nameof(IsEnabledRecalcTemperatures):
                    result = IsEnabledRecalcTemperatures();
                    return true;
            }

            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        protected override List<MessageItem> OnHandleWFNodesRemoveMessageItems(List<MessageItem> messageItems)
        {
            if (messageItems.Any(c => c.UserAckPWNode != null && c.UserAckPWNode.ValueT.ACIdentifier.Contains(nameof(PWBakeryFlourDischargingAck))))
            {
                BtnFlourBlink = false;
            }

            IACComponentPWNode bakeryTempCalc = CurrentBakeryTempCalc;
            if (bakeryTempCalc != null)
            {
                return messageItems.Where(c => c.UserAckPWNode != null && c.UserAckPWNode.ValueT != bakeryTempCalc).ToList();
            }

            return base.OnHandleWFNodesRemoveMessageItems(messageItems);
        }

        public override bool AddToMessageList(MessageItem messageItem)
        {
            if (messageItem != null && messageItem.UserAckPWNode != null)
            {
                if (messageItem.UserAckPWNode.ValueT.ACIdentifier.Contains(PWBakeryFlourDischargingAck.PWClassName))
                {
                    if (   CoverFlourBtnMode == CoverFlourButtonEnum.FlourDischargeVisible
                        || (CoverFlourBtnMode == CoverFlourButtonEnum.CoverUpDownVisible && !IsCoverUpDown.ValueT))
                    {
                        BtnFlourBlink = true;
                    }
                    messageItem.HandleByAcknowledgeButton = false;
                }
            }

            return base.AddToMessageList(messageItem);
        }

        public override void Abort()
        {
            if (!IsEnabledAbort())
                return;

            IACComponentPWNode componentPWNode = ComponentPWNodeLocked;
            if (componentPWNode != null)
            {
                if (_FloorScaleDosingInfo == null)
                {
                    var dosingOnFloorScaleProp = CurrentProcessModule?.GetPropertyNet(nameof(BakeryReceivingPoint.IsDosingOnFloorScale));
                    if (dosingOnFloorScaleProp != null)
                    {
                        _FloorScaleDosingInfo = dosingOnFloorScaleProp as IACContainerTNet<RecvPointDosingInfoEnum>;
                    }
                }

                if (_FloorScaleDosingInfo != null)
                {
                    _FloorScaleDosingInfo.PropertyChanged -= _FloorScaleDosingInfo_PropertyChanged;
                    _FloorScaleDosingInfo.PropertyChanged += _FloorScaleDosingInfo_PropertyChanged;

                    _FloorScaleDosingInfo_PropertyChanged(_FloorScaleDosingInfo, new System.ComponentModel.PropertyChangedEventArgs(Const.ValueT));
                }
            }

            base.Abort();

            if (_FloorScaleDosingInfo != null)
            {
                _FloorScaleDosingInfo.PropertyChanged -= _FloorScaleDosingInfo_PropertyChanged;
            }
        }


        private void _FloorScaleDosingInfo_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<RecvPointDosingInfoEnum> temp = sender as IACContainerTNet<RecvPointDosingInfoEnum>;
                if (temp != null)
                {
                    RecvPointDosingInfo = temp.ValueT;
                }
            }
        }

        public override void Interdischarge()
        {
            if (!IsEnabledInterdischarge())
                return;

            base.Interdischarge();
        }

        public override bool IsEnabledInterdischarge()
        {
            if (RecvPointDosingInfo != RecvPointDosingInfoEnum.DosingActive)
            {
                return InInterdischargingQ.HasValue ? false : true;
            }

            return false;
        }

        #endregion
    }

    public enum CoverFlourButtonEnum : short
    {
        None = 0,
        CoverUpDownVisible = 10,
        //CoverUpDownVisibleAckInAdvance = 11,
        FlourDischargeVisible = 20,
        //FlourDischargeVisibleAckInAdvance = 21
    }
}
