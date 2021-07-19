﻿using gip.bso.manufacturing;
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

        [ACPropertyInfo(9999)]
        public ACRef<IACComponentPWNode> BakeryTempCalculator
        {
            get;
            set;
        }

        [ACPropertyInfo(9999)]
        public IACComponentPWNode CurrentBakeryTempCalc
        {
            get
            {
                return BakeryTempCalculator?.ValueT;
            }
        }

        private IACContainerTNet<ACStateEnum> BakeryTempCalcACState
        {
            get;
            set;
        }

        private IACContainerTNet<string> TempCalcResultMessage
        {
            get;
            set;
        }

        private IACContainerTNet<bool> IsCoverUpDown;

        private bool _IsCoverUpDownVisible;
        [ACPropertyInfo(9999)]
        public bool IsCoverUpDownVisible
        {
            get => _IsCoverUpDownVisible;
            set
            {
                _IsCoverUpDownVisible = value;
                OnPropertyChanged("IsCoverUpDownVisible");
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

        #endregion

        #endregion

        #region Methods

        public override void Activate(ACComponent selectedProcessModule)
        {
            UnloadBakeryTempCalc();
            ResetTemperatureDialogParam();
            base.Activate(selectedProcessModule);

            IsCoverUpDownVisible = false;

            if (_BakeryRecvPointType.IsAssignableFrom(selectedProcessModule.ComponentClass.ObjectType))
            {
                var isCoverUpDown = selectedProcessModule.GetPropertyNet("IsCoverDown") as IACContainerTNet<bool>;
                if (isCoverUpDown != null)
                {
                    bool? isBounded = selectedProcessModule.ExecuteMethod("IsCoverDownPropertyBounded") as bool?;
                    if (isBounded.HasValue && isBounded.Value)
                    {
                        IsCoverUpDown = isCoverUpDown;
                        IsCoverUpDownVisible = true;
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

            BakeryTempCalculator = new ACRef<IACComponentPWNode>(pwNode, this);

            BakeryTempCalcACState = BakeryTempCalculator.ValueT.GetPropertyNet(Const.ACState) as IACContainerTNet<ACStateEnum>;

            TempCalcResultMessage = BakeryTempCalculator.ValueT.GetPropertyNet("TemperatureCalculationResult") as IACContainerTNet<string>;
            if (TempCalcResultMessage != null)
            {
                TempCalcResultMessage.PropertyChanged += TempCalcResultMessage_PropertyChanged;
            }
            HandleTempCalcResultMsg(TempCalcResultMessage.ValueT);

            GetTemperaturesFromPWBakeryTempCalc();
        }

        public override void UnloadWFNode()
        {
            if (BakeryTempCalcACState != null)
                BakeryTempCalcACState = null;

            if (TempCalcResultMessage != null)
            {
                TempCalcResultMessage.PropertyChanged -= TempCalcResultMessage_PropertyChanged;
                TempCalcResultMessage = null;
            }

            base.UnloadWFNode();
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

        private void HandleTempCalcResultMsg(string msg)
        {
            if (BakeryTempCalcACState != null && BakeryTempCalcACState.ValueT == ACStateEnum.SMRunning)
            {
                var existingMessageItems = MessagesList.Where(c => c.UserAckPWNode.ValueT == CurrentBakeryTempCalc).ToArray();
                if (existingMessageItems != null)
                {
                    foreach (MessageItem mItem in existingMessageItems)
                    {
                        RemoveFromMessageList(mItem);
                    }
                }

                if (string.IsNullOrEmpty(msg))
                    return;

                MessageItem msgItem = new MessageItem(BakeryTempCalculator.ValueT, this);
                msgItem.Message = msg;

                AddToMessageList(msgItem);
            }
        }

        private void GetTemperaturesFromPWBakeryTempCalc()
        {
            ACMethod config = BakeryTempCalculator.ValueT.ACUrlCommand("MyConfiguration") as ACMethod;
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
                BakeryTempCalcACState = null;

            if (TempCalcResultMessage != null)
            {
                TempCalcResultMessage.PropertyChanged -= TempCalcResultMessage_PropertyChanged;
                TempCalcResultMessage = null;
            }

            if (BakeryTempCalculator != null)
            {
                BakeryTempCalculator.Detach();
                BakeryTempCalculator = null;
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
                IsCoverUpDown.ValueT = !IsCoverUpDown.ValueT;
            }
        }

        public bool IsEnabledRecvPointCoverUpDown()
        {
            return IsCoverUpDown != null && IsCoverUpDownVisible;
        }

        #region Methods => Temperature dialog

        [ACMethodInfo("", "", 800)]
        public void ShowTemperaturesDialog()
        {
            var corrTemp = CurrentProcessModule.ACUrlCommand("DoughCorrTemp") as double?;
            if (corrTemp.HasValue)
                DoughCorrTemperature = corrTemp.Value;

            ACValueList watersTempInCalc = BakeryTempCalculator?.ValueT?.ExecuteMethod("GetTemperaturesUsedInCalc") as ACValueList;
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
                CurrentProcessModule.ACUrlCommand("DoughCorrTemp", DoughCorrTemperature); //Save dough correct temperature on bakery recieving point
                BakeryTempCalculator.ValueT.ExecuteMethod("SaveWorkplaceTemperatureSettings", WaterTargetTemperature, IsOnlyWaterTemperatureCalculation);//TODO parameters
            }
            CloseTopDialog();
            _ParamChanged = false;
        }

        public bool IsEnabledApplyTemperatures()
        {
            return BakeryTempCalculator != null && BakeryTempCalculator.ValueT != null && CurrentProcessModule != null;
        }

        [ACMethodInfo("", "en{'Recalculate'}de{'Neu berechnen'}", 801)]
        public void RecalcTemperatures()
        {
            CurrentProcessModule.ACUrlCommand("DoughCorrTemp", DoughCorrTemperature); //Save dough correct temperature on bakery recieving point
            BakeryTempCalculator.ValueT.ExecuteMethod("SaveWorkplaceTemperatureSettings", WaterTargetTemperature, IsOnlyWaterTemperatureCalculation);//TODO parameters
            _ParamChanged = false;
        }

        public bool IsEnabledRecalcTemperatures()
        {
            return BakeryTempCalculator != null && BakeryTempCalculator.ValueT != null && CurrentProcessModule != null && _ParamChanged;
        }

        #endregion

        #region Methods => SingleDosing

        public override void OnPreStartWorkflow(gip.mes.datamodel.Picking picking, List<SingleDosingConfigItem> configItems, Route validRoute, ACClassWF rootWF)
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
                        return;

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
                        return;
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

        #endregion
    }
}
