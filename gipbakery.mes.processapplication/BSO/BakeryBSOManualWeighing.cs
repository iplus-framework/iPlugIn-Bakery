using gip.bso.manufacturing;
using gip.core.autocomponent;
using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using vd = gip.mes.datamodel;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioManufacturing, "en{'Manual weighing'}de{'Handverwiegung'}", Global.ACKinds.TACBSO, Global.ACStorableTypes.NotStorable, true, true, SortIndex = 100)]
    public class BakeryBSOManualWeighing : BSOManualWeighing
    {
        #region c'tors

        public BakeryBSOManualWeighing(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
            
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
                OnPropertyChanged("IsOnlyWaterTemperatureCalculation");
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
                OnPropertyChanged("IsDoughTemperatureCalculation");
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
                OnPropertyChanged("DoughTargetTemperature");
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
                OnPropertyChanged("DoughCorrTemperature");
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
                OnPropertyChanged("WaterTargetTemperature");
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
                OnPropertyChanged("SingleDosTargetTemperature");
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
                OnPropertyChanged("DischargeOverHose");
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

            ACClass recvPointClass = null;

            var contextIplus = Database.ContextIPlus;
            using (ACMonitor.Lock(contextIplus.QueryLock_1X000))
            {
                recvPointClass = selectedProcessModule?.ComponentClass.FromIPlusContext<ACClass>(contextIplus);
            }

            if (recvPointClass != null && _BakeryRecvPointType.IsAssignableFrom(recvPointClass.ObjectType))
            {
                var isCoverUpDown = selectedProcessModule.GetPropertyNet("IsCoverDown") as IACContainerTNet<bool>;
                if (isCoverUpDown != null)
                {
                    bool? isBounded = selectedProcessModule.ExecuteMethod("IsCoverDownPropertyBounded") as bool?;
                    if (isBounded.HasValue && isBounded.Value)
                    {
                        bool cover = false;

                        var config = recvPointClass.ACClassConfig_ACClass.FirstOrDefault(c => c.ConfigACUrl == "WithCover");
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

                        if (cover)
                            CoverFlourBtnMode = CoverFlourButtonEnum.CoverUpDownVisible;
                        else
                            CoverFlourBtnMode = CoverFlourButtonEnum.FlourDischargeVisible;

                        IsCoverUpDown = isCoverUpDown;
                    }
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
                ACRef<ACClass> refClass = new ACRef<ACClass>(pwClass, true);
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

            TempCalcResultMessage = pwNode.GetPropertyNet("TemperatureCalculationResult") as IACContainerTNet<string>;

            if (TempCalcResultMessage != null)
            {
                HandleTempCalcResultMsg(TempCalcResultMessage.ValueT);
                TempCalcResultMessage.PropertyChanged += TempCalcResultMessage_PropertyChanged;
            }


            if (BakeryTempCalcACState != null)
            {
                HandleTempCalcACState(BakeryTempCalcACState.ValueT);
                _BakeryTempCalcACState = (short)BakeryTempCalcACState.ValueT;
                BakeryTempCalcACState.PropertyChanged += BakeryTempCalcACState_PropertyChanged;
            }

            GetTemperaturesFromPWBakeryTempCalc(pwNode);
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
            if (acState != ACStateEnum.SMRunning)
                return; //TODO: check if list contains this message

            IACComponentPWNode tempCalc = CurrentBakeryTempCalc;
            if (tempCalc == null)
            {
                //TODO: error
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
                var existingMessageItems = _MessagesListSafe.Where(c => c.UserAckPWNode.ValueT == tempCalc).ToArray();
                if (existingMessageItems != null)
                {
                    foreach (MessageItem mItem in existingMessageItems)
                    {
                        RemoveFromMessageList(mItem);
                    }
                }

                MessageItem msgItem = new MessageItem(tempCalc, this, eMsgLevel.Question);
                msgItem.Message = "Is water needed?";

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
                    //TODO: error
                }

                var existingMessageItems = _MessagesListSafe.Where(c => c.UserAckPWNode.ValueT == tempCalc).ToArray();
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
                if (wTemp != null)
                {
                    WaterTargetTemperature = wTemp.ParamAsDouble;
                }

                ACValue onlyForWaterCalc = config.ParameterValueList.GetACValue("UseWaterTemp");
                if (onlyForWaterCalc != null)
                {
                    IsOnlyWaterTemperatureCalculation = onlyForWaterCalc.ParamAsBoolean;
                    if (IsOnlyWaterTemperatureCalculation)
                    {
                        IsDoughTemperatureCalculation = false;
                    }
                    else
                    {
                        IsDoughTemperatureCalculation = true;
                    }
                }
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
                    IsCoverUpDown.ValueT = true;
                }
            }
        }

        public bool IsEnabledRecvPointCoverUpDown()
        {
            return ParentBSOWCS != null && ParentBSOWCS.IsCurrentUserConfigured && IsCoverUpDown != null && CoverFlourBtnMode > CoverFlourButtonEnum.None;
        }

        #region Methods => Temperature dialog

        [ACMethodInfo("", "", 800)]
        public void ShowTemperaturesDialog()
        {
            var corrTemp = CurrentProcessModule.ACUrlCommand("DoughCorrTemp") as double?;
            if (corrTemp.HasValue)
                DoughCorrTemperature = corrTemp.Value;

            IACComponentPWNode tempCalc = CurrentBakeryTempCalc;

            ACValueList watersTempInCalc = tempCalc?.ExecuteMethod("GetTemperaturesUsedInCalc") as ACValueList;
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
            }

            _ParamChanged = false;

            ShowDialog(this, "TemperaturesDialog");
        }

        public bool IsEnabledShowTemperaturesDialog()
        {
            return CurrentProcessModule != null;
        }

        [ACMethodInfo("", "", 880, true)]
        public void DoughTempCorrPlus()
        {
            DoughCorrTemperature++;
        }

        [ACMethodInfo("", "", 880, true)]
        public void DoughTempCorrMinus()
        {
            DoughCorrTemperature--;
        }

        [ACMethodInfo("", "", 880, true)]
        public void WaterTempPlus()
        {
            WaterTargetTemperature++;
        }

        [ACMethodInfo("", "", 880, true)]
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
                    //TODO: error
                }

                CurrentProcessModule.ACUrlCommand("DoughCorrTemp", DoughCorrTemperature); //Save dough correct temperature on bakery recieving point
                tempCalc.ExecuteMethod("SaveWorkplaceTemperatureSettings", WaterTargetTemperature, IsOnlyWaterTemperatureCalculation);//TODO parameters
            }
            CloseTopDialog();
            _ParamChanged = false;
        }

        public bool IsEnabledApplyTemperatures()
        {
            return _IsTempCalcNotNull && CurrentProcessModule != null;
        }

        [ACMethodInfo("", "en{'Recalculate'}de{'Neu berechnen'}", 801)]
        public void RecalcTemperatures()
        {
            IACComponentPWNode tempCalc = CurrentBakeryTempCalc;
            if (tempCalc == null)
            {
                //TODO: error
            }

            CurrentProcessModule.ACUrlCommand("DoughCorrTemp", DoughCorrTemperature); //Save dough correct temperature on bakery recieving point
            tempCalc.ExecuteMethod("SaveWorkplaceTemperatureSettings", WaterTargetTemperature, IsOnlyWaterTemperatureCalculation);//TODO parameters
            _ParamChanged = false;
        }

        public bool IsEnabledRecalcTemperatures()
        {
            return _IsTempCalcNotNull && CurrentProcessModule != null && _ParamChanged;
        }

        #endregion

        #region Methods => SingleDosing

        public override bool OnPreStartWorkflow(gip.mes.datamodel.Picking picking, List<SingleDosingConfigItem> configItems, Route validRoute, ACClassWF rootWF)
        {
            base.OnPreStartWorkflow(picking, configItems, validRoute, rootWF);

            if (SingleDosTargetTemperature.HasValue)
            {
                SingleDosingConfigItem configItem = configItems.FirstOrDefault(c => c.PWGroup.ACClassWF_ParentACClassWF
                                                                                             .Any(x => _BakeryTempCalcType.IsAssignableFrom(x.PWACClass.ObjectType)));

                if (configItem != null)
                {
                    ACClassWF tempCalc = configItem.PWGroup.ACClassWF_ParentACClassWF.FirstOrDefault(x => _BakeryTempCalcType.IsAssignableFrom(x.PWACClass.ObjectType));
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

                    var msg = DatabaseApp.ACSaveChanges();
                    if (msg != null)
                        Messages.Msg(msg);

                }
            }

            bool result = AddDischargingConfig(picking, configItems, validRoute, rootWF);

            return result;
        }

        private bool AddDischargingConfig(gip.mes.datamodel.Picking picking, List<SingleDosingConfigItem> configItems, Route validRoute, ACClassWF rootWF)
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

                if (discharging != null)
                {
                    ACMethod acMethod = discharging.RefPAACClassMethod.ACMethod;

                    string preConfigACUrl = rootWF.ConfigACUrl + "\\";
                    string configACUrl = string.Format("{0}\\{1}\\Destination", discharging.ConfigACUrl, acMethod.ACIdentifier);

                    IACConfig targetConfig = picking.ConfigurationEntries.FirstOrDefault(c => c.PreConfigACUrl == preConfigACUrl && c.LocalConfigACUrl == configACUrl);

                    if (targetConfig == null)
                    {
                        ACConfigParam param = new ACConfigParam()
                        {
                            ACIdentifier = "Destination",
                            ACCaption = acMethod.GetACCaptionForACIdentifier("Destination"),
                            ValueTypeACClassID = DatabaseApp.ContextIPlus.GetACType("Int16").ACClassID,
                            ACClassWF = discharging
                        };

                        targetConfig = ConfigManagerIPlus.ACConfigFactory(picking, param, preConfigACUrl, configACUrl, null);
                        param.ConfigurationList.Insert(0, targetConfig);

                        picking.ConfigurationEntries.Append(targetConfig);
                    }

                    ACClass compClass = CurrentProcessModule?.ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(DatabaseApp.ContextIPlus);
                    if (compClass == null)
                    {
                        //TODO:error
                        return false;
                    }

                    var config = compClass.ACClassConfig_ACClass.FirstOrDefault(c => c.ConfigACUrl == "HoseDestination");
                    if (config == null)
                    {
                        //TODO: error
                        return false;
                    }

                    targetConfig.Value = config.Value;

                    Msg msg = DatabaseApp.ACSaveChanges();
                    if (msg != null)
                    {
                        Messages.Msg(msg);
                        return false;
                    }
                }
            }

            return true;
        }

        private IACConfig InsertTemperatureConfiguration(string propertyACUrl, string preConfigACUrl, string paramACIdentifier, ACClassWF acClassWF, IACConfigStore configStore)
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

        #endregion

        public override Global.ControlModes OnGetControlModes(IVBContent vbControl)
        {
            return base.OnGetControlModes(vbControl);
        }

        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            switch(acMethodName)
            {
                case "RecvPointCoverUpDown":
                    RecvPointCoverUpDown();
                    return true;

                case "IsEnabledRecvPointCoverUpDown":
                    result = IsEnabledRecvPointCoverUpDown();
                    return true;

                case "ShowTemperaturesDialog":
                    ShowTemperaturesDialog();
                    return true;

                case "IsEnabledShowTemperaturesDialog":
                    result = IsEnabledShowTemperaturesDialog();
                    return true;

                case "DoughTempCorrPlus":
                    DoughTempCorrPlus();
                    return true;

                case "DoughTempCorrMinus":
                    DoughTempCorrMinus();
                    return true;

                case "WaterTempPlus":
                    WaterTempPlus();
                    return true;

                case "WaterTempMinus":
                    WaterTempMinus();
                    return true;

                case "IsEnabledApplyTemperatures":
                    result = IsEnabledApplyTemperatures();
                    return true;

                case "RecalcTemperatures":
                    RecalcTemperatures();
                    return true;

                case "IsEnabledRecalcTemperatures":
                    result = IsEnabledRecalcTemperatures();
                    return true;
            }

            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        public override bool AddToMessageList(MessageItem messageItem)
        {
            if (messageItem != null && messageItem.UserAckPWNode != null)
            {
                if (messageItem.UserAckPWNode.ValueT.ACIdentifier.Contains(PWBakeryFlourDischargingAck.PWClassName))
                {
                    messageItem.HandleByAcknowledgeButton = false;
                }
            }

            return base.AddToMessageList(messageItem);
        }

        #endregion
    }

    public enum CoverFlourButtonEnum : short
    {
        None = 0,
        CoverUpDownVisible = 10,
        FlourDischargeVisible = 20
    }
}
