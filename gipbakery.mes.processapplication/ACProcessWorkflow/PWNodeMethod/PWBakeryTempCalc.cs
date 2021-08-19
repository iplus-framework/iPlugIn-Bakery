using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using gip.mes.datamodel;
using System.Runtime.Serialization;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Dough temperature calculation'}de{'Teigtemperaturberechnung'}", Global.ACKinds.TPWNodeStatic, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryTempCalc : PWNodeUserAck
    {
        public new const string PWClassName = "PWBakeryTempCalc";

        public const string KneedingRiseTemp = "KneedingRiseTemp";

        #region Constructors

        static PWBakeryTempCalc()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("DoughTemp", typeof(double?), false, Global.ParamOption.Required));
            paramTranslation.Add("DoughTemp", "en{'Doughtemperature °C'}de{'Teigtemperatur °C'}");

            method.ParameterValueList.Add(new ACValue("WaterTemp", typeof(double?), false, Global.ParamOption.Required));
            paramTranslation.Add("WaterTemp", "en{'Watertemperature °C'}de{'Wassertemperatur °C'}");

            method.ParameterValueList.Add(new ACValue("DryIceMatNo", typeof(string), "", Global.ParamOption.Required));
            paramTranslation.Add("DryIceMatNo", "en{'Dry ice MaterialNo'}de{'Eis MaterialNo'}");

            method.ParameterValueList.Add(new ACValue("UseWaterTemp", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("UseWaterTemp", "en{'Use Watertemperature'}de{'Wassertemperatur verwenden'}");

            method.ParameterValueList.Add(new ACValue("UseWaterMixer", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("UseWaterMixer", "en{'Calculate for water mixer'}de{'Berechnen für Wassermischer'}");

            method.ParameterValueList.Add(new ACValue("IncludeMeltingHeat", typeof(MeltingHeatOptionEnum), MeltingHeatOptionEnum.Off, Global.ParamOption.Required));
            paramTranslation.Add("IncludeMeltingHeat", "en{'Include melting heat'}de{'Schmelzwärme einrechnen'}");

            method.ParameterValueList.Add(new ACValue("WaterMeltingHeat", typeof(double), 332500, Global.ParamOption.Required));
            paramTranslation.Add("MeltingHeatWater", "en{'Water melting heat[J/kg]'}de{'SchmelzwaermeWasser[J/kg]'}");

            method.ParameterValueList.Add(new ACValue("MeltingHeatInfluence", typeof(double), 100, Global.ParamOption.Required));
            paramTranslation.Add("MeltingHeatInfluence", "en{'Melting heat influence[%]'}de{'SchmelzwaermeEinfluss[%]'}");

            var wrapper = new ACMethodWrapper(method, "en{'User Acknowledge'}de{'Benutzerbestätigung'}", typeof(PWBakeryTempCalc), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryTempCalc), ACStateConst.SMStarting, wrapper);

            RegisterExecuteHandler(typeof(PWBakeryTempCalc), HandleExecuteACMethod_PWBakeryTempCalc);
        }

        public PWBakeryTempCalc(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            ResetMembers();
            return base.ACDeInit(deleteACClassTask);
        }

        public override void Recycle(IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
        {
            ResetMembers();
            base.Recycle(content, parentACObject, parameter, acIdentifier);
        }

        #endregion

        #region Properties

        [ACPropertyBindingSource]
        public IACContainerTNet<string> TemperatureCalculationResult
        {
            get;
            set;
        }

        [ACPropertyBindingSource]
        public IACContainerTNet<double> WarmWaterQuantity
        {
            get;
            set;
        }

        [ACPropertyBindingSource]
        public IACContainerTNet<double> CityWaterQuantity
        {
            get;
            set;
        }

        [ACPropertyBindingSource]
        public IACContainerTNet<double> ColdWaterQuantity
        {
            get;
            set;
        }

        [ACPropertyBindingSource]
        public IACContainerTNet<double> DryIceQuantity
        {
            get;
            set;
        }

        [ACPropertyBindingSource(500, "", "en{'Result'}de{'Ergebnis'}")]
        public IACContainerTNet<double> WaterTotalQuantity
        {
            get;
            set;
        }


        /// <summary>
        /// Represents the calculated water temperature
        /// </summary>
        [ACPropertyBindingSource(9999, "", "en{'Water calculation temperature result'}de{'Water calculation temperature result'}", "", false, true)]
        public IACContainerTNet<double> WaterCalcResult
        {
            get;
            set;
        }

        [ACPropertyInfo(9999)]
        public ACValueList WaterTemperaturesUsedInCalc
        {
            get;
            set;
        }
            

        private Type _PWManualWeighingType = typeof(PWManualWeighing);
        private Type _PWDosingType = typeof(PWDosing);

        public T ParentPWMethod<T>() where T : PWMethodVBBase
        {
            if (ParentRootWFNode == null)
                return null;
            return ParentRootWFNode as T;
        }

        public bool IsProduction
        {
            get
            {
                return ParentPWMethod<PWMethodProduction>() != null;
            }
        }

        public bool IsRelocation
        {
            get
            {
                return ParentPWMethod<PWMethodRelocation>() != null;
            }
        }

        private TempCalcMode _CalculatorMode = TempCalcMode.Calcuate;

        private bool _RecalculateTemperatures = true;

        private string _CityWaterMaterialNo;
        private string _ColdWaterMaterialNo;
        private string _WarmWaterMaterialNo;

        #region Properties => Configuration

        protected double? DoughTemp
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("DoughTemp");
                    if (acValue != null)
                    {
                        return (double?)acValue.Value;
                    }
                }
                return null;
            }
        }

        protected double? WaterTemp
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("WaterTemp");
                    if (acValue != null)
                    {
                        return (double?)acValue.Value;
                    }
                }
                return null;
            }
        }

        protected string DryIceMaterialNo
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("DryIceMatNo");
                    if (acValue != null)
                    {
                        return acValue.ParamAsString;
                    }
                }
                return null;
            }
        }

        protected bool UseWaterTemp
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("UseWaterTemp");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }

        internal bool UseWaterMixer
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("UseWaterMixer");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }

        protected MeltingHeatOptionEnum IncludeMeltingHeat
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("IncludeMeltingHeat");
                    if (acValue != null)
                    {
                        return acValue.ValueT<MeltingHeatOptionEnum>();
                    }
                }
                return MeltingHeatOptionEnum.Off;
            }
        }

        protected double? WaterMeltingHeat
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("WaterMeltingHeat");
                    if (acValue != null)
                    {
                        return (double?)acValue.Value;
                    }
                }
                return null;
            }
        }

        protected double? MeltingHeatInfluence
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("MeltingHeatInfluence");
                    if (acValue != null)
                    {
                        return (double?)acValue.Value;
                    }
                }
                return null;
            }
        }

        #endregion

        #endregion

        #region Methods

        #region Methods => ACState

        public override void Start()
        {
            base.Start();
        }

        public override void Reset()
        {
            ResetMembers();
            base.Reset();
        }

        public override void SMIdle()
        {
            ResetMembers();
            base.SMIdle();
        }

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        public override void SMRunning()
        {
            RefreshNodeInfoOnModule();
            if (Root.Initialized)
            {
                BakeryReceivingPoint recvPoint = ParentPWGroup?.AccessedProcessModule as BakeryReceivingPoint;
                bool? isTempServiceInitialized = recvPoint?.ExecuteMethod("IsTemperatureServiceInitialized") as bool?;
                if (isTempServiceInitialized.HasValue || isTempServiceInitialized.Value)
                {
                    TempCalcMode calcMode = TempCalcMode.Calcuate;
                    using (ACMonitor.Lock(_20015_LockValue))
                        calcMode = _CalculatorMode;

                    if (calcMode == TempCalcMode.Calcuate)
                        CalculateTargetTemperature();
                    else if (calcMode == TempCalcMode.AdjustOrder)
                    {
                        AdjustOrder();
                        CurrentACState = ACStateEnum.SMCompleted;
                    }
                }
                else
                {
                    SubscribeToProjectWorkCycle();
                }
            }
            else
                SubscribeToProjectWorkCycle();

            //base.SMRunning();
        }

        [ACMethodState("en{'Completed'}de{'Beendet'}", 40, true)]
        public override void SMCompleted()
        {
            base.SMCompleted();
        }

        #endregion

        public override void AckStart()
        {
            AcknowledgeAllAlarms();
            if (CurrentACState == ACStateEnum.SMStarting)
                CurrentACState = ACStateEnum.SMRunning;

            else if (CurrentACState == ACStateEnum.SMRunning)
            {
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    _CalculatorMode = TempCalcMode.AdjustOrder;
                }
                SubscribeToProjectWorkCycle();
            }
        }

        #region Methods => Temperature calculation

        private void CalculateTargetTemperature()
        {
            bool recalc = false;

            using (ACMonitor.Lock(_20015_LockValue))
            {
                recalc = _RecalculateTemperatures;
            }

            if (!recalc)
                return;

            using (ACMonitor.Lock(_20015_LockValue))
            {
                WarmWaterQuantity.ValueT = 0;
                CityWaterQuantity.ValueT = 0;
                ColdWaterQuantity.ValueT = 0;
                DryIceQuantity.ValueT = 0;
                WaterTotalQuantity.ValueT = 0;
            }

            BakeryReceivingPoint recvPoint = ParentPWGroup.AccessedProcessModule as BakeryReceivingPoint;
            if (recvPoint == null)
            {
                //Error50407: Accessed process module on the PWGroup is null or is not BakeryReceivingPoint!
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CalculateTargetTemperature(10)", 429, "Error50407");
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                {
                    OnNewAlarmOccurred(ProcessAlarm, msg);
                    Root.Messages.LogMessageMsg(msg);
                }
                return;
            }

            if (string.IsNullOrEmpty(DryIceMaterialNo))
            {
                //Error50408 The ice MaterialNo is not configured.Please configure the ice material number.
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CalculateTargetTemperature(20)", 441, "Error50408");
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                {
                    OnNewAlarmOccurred(ProcessAlarm, msg);
                    Root.Messages.LogMessageMsg(msg);
                }
                return;
            }

            PWMethodProduction pwMethodProduction = ParentPWMethod<PWMethodProduction>();
            // If dosing is not for production, then do nothing
            if (pwMethodProduction != null)
            {
                CalculateProdOrderTargetTemperature(recvPoint, pwMethodProduction);
            }
            else
            {
                PWMethodRelocation pwMethodRelocation = ParentPWMethod<PWMethodRelocation>();
                if (pwMethodRelocation != null)
                {
                    CalculateRelocationTargetTempreature(recvPoint, pwMethodRelocation);
                }
            }

            using (ACMonitor.Lock(_20015_LockValue))
            {
                _RecalculateTemperatures = false;
            }
            UnSubscribeToProjectWorkCycle();
        }

        private void CalculateProdOrderTargetTemperature(BakeryReceivingPoint recvPoint, PWMethodProduction pwMethodProduction)
        {
            using (Database db = new gip.core.datamodel.Database())
            using (DatabaseApp dbApp = new DatabaseApp())
            {
                if (!UseWaterTemp && DoughTemp == null)
                {
                    //Error50409: The dough target temperature calculation is selected, but the dough temperature is not configured. Please configure the dough target temperature.
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CalculateProdOrderTargetTemperature(10)", 478, "Error50409");
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ProcessAlarm, msg);
                        Root.Messages.LogMessageMsg(msg);
                    }
                    return;
                }

                double kneedingRiseTemperature = 0;
                double recvPointCorrTemp = 0;
                double doughTargetTempBeforeKneeding = 0;

                ProdOrderPartslistPos endBatchPos = pwMethodProduction.CurrentProdOrderPartslistPos.FromAppContext<ProdOrderPartslistPos>(dbApp);

                if (!UseWaterTemp)
                {
                    bool kneedingTempResult = GetKneedingRiseTemperature(dbApp, endBatchPos.TargetQuantityUOM, out kneedingRiseTemperature);
                    if (!kneedingTempResult)
                        return;

                    recvPointCorrTemp = recvPoint.DoughCorrTemp.ValueT;

                    doughTargetTempBeforeKneeding = DoughTemp.Value - kneedingRiseTemperature + recvPointCorrTemp;
                }

                ACValueList componentTemperaturesService = recvPoint.GetWaterComponentsFromTempService();
                if (componentTemperaturesService == null)
                    return;

                List<MaterialTemperature> tempFromService = componentTemperaturesService.Select(c => c.Value as MaterialTemperature).ToList();

                _ColdWaterMaterialNo = tempFromService.FirstOrDefault(c => c.Water == WaterType.ColdWater)?.MaterialNo;
                _CityWaterMaterialNo = tempFromService.FirstOrDefault(c => c.Water == WaterType.CityWater)?.MaterialNo;
                _WarmWaterMaterialNo = tempFromService.FirstOrDefault(c => c.Water == WaterType.WarmWater)?.MaterialNo;
                string dryIce = DryIceMaterialNo;

                if (string.IsNullOrEmpty(_ColdWaterMaterialNo))
                {
                    //Error50410: Can not get the MaterialNo for {0} from the temperature service.
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CalculateProdOrderTargetTemperature(20)", 515, "Error50410", "ColdWater");
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ProcessAlarm, msg);
                        Root.Messages.LogMessageMsg(msg);
                    }
                    return;
                }

                if (string.IsNullOrEmpty(_CityWaterMaterialNo))
                {
                    //Error50410: Can not get the MaterialNo for {0} from the temperature service.
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CalculateProdOrderTargetTemperature(20)", 526, "Error50410", "ColdWater");
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ProcessAlarm, msg);
                        Root.Messages.LogMessageMsg(msg);
                    }
                    return;
                }

                if (string.IsNullOrEmpty(_WarmWaterMaterialNo))
                {
                    //Error50410: Can not get the MaterialNo for {0} from the temperature service.
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CalculateProdOrderTargetTemperature(20)", 537, "Error50410", "ColdWater");
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ProcessAlarm, msg);
                        Root.Messages.LogMessageMsg(msg);
                    }
                    return;
                }


                if (pwMethodProduction.CurrentProdOrderBatch == null)
                {
                    // Error50411: No batch assigned to last intermediate material of this workflow
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CalculateProdOrderTargetTemperature(30)", 554, "Error50411");

                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                        Messages.LogError(this.GetACUrl(), msg.ACIdentifier, msg.InnerMessage);
                    OnNewAlarmOccurred(ProcessAlarm, msg, false);
                    return;
                }

                var contentACClassWFVB = ContentACClassWF.FromAppContext<gip.mes.datamodel.ACClassWF>(dbApp);
                ProdOrderBatch batch = pwMethodProduction.CurrentProdOrderBatch.FromAppContext<ProdOrderBatch>(dbApp);
                ProdOrderBatchPlan batchPlan = batch.ProdOrderBatchPlan;

                PartslistACClassMethod plMethod = endBatchPos.ProdOrderPartslist.Partslist.PartslistACClassMethod_Partslist.FirstOrDefault();
                ProdOrderPartslist currentProdOrderPartslist = endBatchPos.ProdOrderPartslist.FromAppContext<ProdOrderPartslist>(dbApp);

                if (currentProdOrderPartslist == null)
                {
                    //TODO Error
                    return;
                }

                IEnumerable<ProdOrderPartslistPos> intermediates = currentProdOrderPartslist.ProdOrderPartslistPos_ProdOrderPartslist
                                                                                            .Where(c => c.MaterialID.HasValue
                                                                                                     && c.MaterialPosType == GlobalApp.MaterialPosTypes.InwardIntern
                                                                                                    && !c.ParentProdOrderPartslistPosID.HasValue)
                                                                                            .SelectMany(p => p.ProdOrderPartslistPos_ParentProdOrderPartslistPos)
                                                                                            .ToArray();


                var relations = intermediates.Select(c => new Tuple<bool?, ProdOrderPartslistPos>(c.Material.ACProperties
                                                         .GetOrCreateACPropertyExtByName("UseInTemperatureCalculation", false)?.Value as bool?, c))
                                             .Where(c => c.Item1.HasValue && c.Item1.Value && c.Item2.ProdOrderBatchID == batch.ProdOrderBatchID)
                                             .SelectMany(x => x.Item2.ProdOrderPartslistPosRelation_TargetProdOrderPartslistPos)
                                             .Where(c => c.SourceProdOrderPartslistPos.IsOutwardRoot).ToArray();

                ProdOrderPartslistPosRelation cityWaterComp = relations.FirstOrDefault(c => c.SourceProdOrderPartslistPos.Material.MaterialNo == _CityWaterMaterialNo);
                if (cityWaterComp == null)
                {
                    // If partslist not contains city water, skip node
                    CurrentACState = ACStateEnum.SMCompleted;
                    return;
                }

                ProdOrderPartslistPosRelation coldWaterComp = relations.FirstOrDefault(c => c.SourceProdOrderPartslistPos.Material.MaterialNo == _ColdWaterMaterialNo);
                ProdOrderPartslistPosRelation warmWaterComp = relations.FirstOrDefault(c => c.SourceProdOrderPartslistPos.Material.MaterialNo == _WarmWaterMaterialNo);
                ProdOrderPartslistPosRelation iceComp = relations.FirstOrDefault(c => c.SourceProdOrderPartslistPos.Material.MaterialNo == DryIceMaterialNo);

                List<MaterialTemperature> compTemps = DetermineComponentsTemperature(currentProdOrderPartslist, intermediates, recvPoint, plMethod, dbApp, tempFromService, _ColdWaterMaterialNo,
                                                                                     _CityWaterMaterialNo, _WarmWaterMaterialNo, dryIce);

                double componentsQ = 0;

                if (!UseWaterTemp)
                    componentsQ = CalculateComponents_Q_(recvPoint, kneedingRiseTemperature, relations, _ColdWaterMaterialNo, _CityWaterMaterialNo, _WarmWaterMaterialNo, dryIce, compTemps);

                bool isOnlyWaterCompsInPartslist = relations.Count() == 1 && relations.FirstOrDefault(c => c.SourceProdOrderPartslistPos.Material.MaterialNo == _CityWaterMaterialNo) != null;
                double? waterTargetQuantity = cityWaterComp.TargetQuantity;

                if (coldWaterComp != null && coldWaterComp.TargetQuantity > 0)
                    waterTargetQuantity += coldWaterComp.TargetQuantity;

                if (warmWaterComp != null && warmWaterComp.TargetQuantity > 0)
                    waterTargetQuantity += warmWaterComp.TargetQuantity;

                if (iceComp != null && iceComp.TargetQuantity > 0)
                    waterTargetQuantity += iceComp.TargetQuantity;

                double defaultWaterTemp = 0;
                double suggestedWaterTemp = CalculateWaterTemperatureSuggestion(UseWaterTemp, isOnlyWaterCompsInPartslist, cityWaterComp.SourceProdOrderPartslistPos.Material, DoughTemp.Value,
                                                                                doughTargetTempBeforeKneeding, componentsQ, waterTargetQuantity, out defaultWaterTemp);

                CalculateWaterTypes(compTemps, suggestedWaterTemp, waterTargetQuantity.Value, defaultWaterTemp, componentsQ, isOnlyWaterCompsInPartslist, doughTargetTempBeforeKneeding,
                                    kneedingRiseTemperature);
            }
        }

        private void CalculateRelocationTargetTempreature(BakeryReceivingPoint recvPoint, PWMethodRelocation pwMethodRelocation)
        {
            using (Database db = new gip.core.datamodel.Database())
            using (DatabaseApp dbApp = new DatabaseApp())
            {
                ACValueList componentTemperaturesService = recvPoint.GetComponentTemperatures();
                if (componentTemperaturesService == null)
                    return;

                List<MaterialTemperature> tempFromService = componentTemperaturesService.Select(c => c.Value as MaterialTemperature).ToList();

                _ColdWaterMaterialNo = tempFromService.FirstOrDefault(c => c.Water == WaterType.ColdWater)?.MaterialNo;
                _CityWaterMaterialNo = tempFromService.FirstOrDefault(c => c.Water == WaterType.CityWater)?.MaterialNo;
                _WarmWaterMaterialNo = tempFromService.FirstOrDefault(c => c.Water == WaterType.WarmWater)?.MaterialNo;
                string dryIce = DryIceMaterialNo;

                if (string.IsNullOrEmpty(_ColdWaterMaterialNo) || string.IsNullOrEmpty(_CityWaterMaterialNo) || string.IsNullOrEmpty(_WarmWaterMaterialNo) || string.IsNullOrEmpty(dryIce))
                {
                    //TODO error
                }


                Picking picking = pwMethodRelocation.CurrentPicking.FromAppContext<Picking>(dbApp);
                PickingPos pickingPos = pwMethodRelocation.CurrentPickingPos.FromAppContext<PickingPos>(dbApp);

                Material cityWaterComp = pickingPos.Material.MaterialNo == _CityWaterMaterialNo ? pickingPos.Material : null;
                if (cityWaterComp == null)
                {
                    CurrentACState = ACStateEnum.SMCompleted;
                    return;
                }

                PickingPos coldWaterComp = picking.PickingPos_Picking.FirstOrDefault(c => c.Material.MaterialNo == _ColdWaterMaterialNo);
                PickingPos warmWaterComp = picking.PickingPos_Picking.FirstOrDefault(c => c.Material.MaterialNo == _WarmWaterMaterialNo);
                PickingPos iceComp = picking.PickingPos_Picking.FirstOrDefault(c => c.Material.MaterialNo == DryIceMaterialNo);

                List<MaterialTemperature> compTemps = DetermineComponentsTemperature(picking, recvPoint, dbApp, tempFromService, _ColdWaterMaterialNo, _CityWaterMaterialNo, _WarmWaterMaterialNo,
                                                                                    dryIce);


                bool isOnlyWaterCompsInPartslist = true;
                double? waterTargetQuantity = pickingPos.TargetQuantity;

                if (coldWaterComp != null && coldWaterComp.TargetQuantity > 0)
                    waterTargetQuantity += coldWaterComp.TargetQuantity;

                if (warmWaterComp != null && warmWaterComp.TargetQuantity > 0)
                    waterTargetQuantity += warmWaterComp.TargetQuantity;

                if (iceComp != null && iceComp.TargetQuantity > 0)
                    waterTargetQuantity += iceComp.TargetQuantity;

                double defaultWaterTemp = 0;
                double suggestedWaterTemp = CalculateWaterTemperatureSuggestion(UseWaterTemp, isOnlyWaterCompsInPartslist, cityWaterComp, WaterTemp.Value, 0,
                                                                                0, waterTargetQuantity, out defaultWaterTemp);

                CalculateWaterTypes(compTemps, suggestedWaterTemp, waterTargetQuantity.Value, defaultWaterTemp, 0, isOnlyWaterCompsInPartslist, 0, null);
            }
        }

        private bool GetKneedingRiseTemperature(DatabaseApp dbApp, double batchSize, out double kneedingTemperature)
        {
            kneedingTemperature = 0;

            var kneedingNodes = RootPW.FindChildComponents<PWBakeryKneading>();

            if (kneedingNodes != null && kneedingNodes.Any())
            {
                if (kneedingNodes.Count > 1)
                {
                    //TODO: alarm
                    return false;
                }

                PWBakeryKneading kneedingNode = kneedingNodes.FirstOrDefault();

                ACMethod kneedingNodeConfiguration = kneedingNode.MyConfiguration;
                if (kneedingNodeConfiguration == null)
                {
                    //TOOD alarm
                    return false;
                }

                double temperatureRiseSlow = 0, temperatureRiseFast = 0;
                TimeSpan kneedingSlow = TimeSpan.Zero, kneedingFast = TimeSpan.Zero;

                bool fullQuantity = true;
                PWNodeProcessWorkflowVB planningNode = RootPW.InvokingWorkflow as PWNodeProcessWorkflowVB;
                if (planningNode != null)
                {
                    ACMethod config = planningNode.MyConfiguration;
                    ACValue standardBatchSize = config?.ParameterValueList.GetACValue("BatchSizeStandard");
                    if (standardBatchSize != null)
                    {
                        double halfQuantity = standardBatchSize.ParamAsDouble / 2;
                        if (batchSize <= halfQuantity)
                            fullQuantity = false;
                    }
                }

                // Full quantity
                if (fullQuantity)
                {
                    ACValue tempRiseSlow = kneedingNodeConfiguration.ParameterValueList.GetACValue("TempRiseSlow");
                    if (tempRiseSlow != null)
                        temperatureRiseSlow = tempRiseSlow.ParamAsDouble;

                    ACValue tempRiseFast = kneedingNodeConfiguration.ParameterValueList.GetACValue("TempRiseFast");
                    if (tempRiseFast != null)
                        temperatureRiseFast = tempRiseFast.ParamAsDouble;

                    ACValue kTimeSlow = kneedingNodeConfiguration.ParameterValueList.GetACValue("KneadingTimeSlow");
                    if (kTimeSlow != null)
                        kneedingSlow = kTimeSlow.ParamAsTimeSpan;

                    ACValue kTimeFast = kneedingNodeConfiguration.ParameterValueList.GetACValue("KneadingTimeFast");
                    if (kTimeFast != null)
                        kneedingFast = kTimeFast.ParamAsTimeSpan;

                    kneedingTemperature = (kneedingSlow.TotalMinutes * temperatureRiseSlow) + (kneedingFast.TotalMinutes * temperatureRiseFast);
                }
                // Half quantity
                else
                {
                    ACValue tempRiseSlow = kneedingNodeConfiguration.ParameterValueList.GetACValue("TempRiseSlowHalf");
                    if (tempRiseSlow != null)
                        temperatureRiseSlow = tempRiseSlow.ParamAsDouble;

                    ACValue tempRiseFast = kneedingNodeConfiguration.ParameterValueList.GetACValue("TempRiseFastHalf");
                    if (tempRiseFast != null)
                        temperatureRiseFast = tempRiseFast.ParamAsDouble;

                    ACValue kTimeSlow = kneedingNodeConfiguration.ParameterValueList.GetACValue("KneadingTimeSlowHalf");
                    if (kTimeSlow != null)
                        kneedingSlow = kTimeSlow.ParamAsTimeSpan;

                    ACValue kTimeFast = kneedingNodeConfiguration.ParameterValueList.GetACValue("KneadingTimeFastHalf");
                    if (kTimeFast != null)
                        kneedingFast = kTimeFast.ParamAsTimeSpan;

                    kneedingTemperature = (kneedingSlow.TotalMinutes * temperatureRiseSlow) + (kneedingFast.TotalMinutes * temperatureRiseFast);
                }
            }
            return true;
        }

        private double CalculateComponents_Q_(BakeryReceivingPoint recvPoint, double kneedingTemperature, IEnumerable<ProdOrderPartslistPosRelation> relations,
                                              string coldWaterMatNo, string cityWaterMatNo, string warmWaterMatNo, string dryIceMatNo, List<MaterialTemperature> compTemps)
        {
            var relationsWithoutWater = relations.Where(c => c.SourceProdOrderPartslistPos.Material.MaterialNo != coldWaterMatNo
                                                          && c.SourceProdOrderPartslistPos.Material.MaterialNo != cityWaterMatNo
                                                          && c.SourceProdOrderPartslistPos.Material.MaterialNo != warmWaterMatNo
                                                          && c.SourceProdOrderPartslistPos.Material.MaterialNo != dryIceMatNo);

            double totalQ = 0;

            foreach (ProdOrderPartslistPosRelation rel in relationsWithoutWater)
            {
                double componentTemperature = recvPoint.RoomTemperature.ValueT;
                MaterialTemperature compTemp = compTemps.FirstOrDefault(c => c.Material.MaterialNo == rel.SourceProdOrderPartslistPos.Material.MaterialNo);
                if (compTemp != null && compTemp.AverageTemperature.HasValue)
                    componentTemperature = compTemp.AverageTemperature.Value;

                totalQ += rel.SourceProdOrderPartslistPos.Material.SpecHeatCapacity * rel.TargetQuantity * (kneedingTemperature - componentTemperature);
            }

            return totalQ;
        }



        private double CalculateWaterTemperatureSuggestion(bool calculateWaterTemp, bool isOnlyWater, Material waterComp, double doughTargetTempAfterKneeding,
                                                           double doughTargetTempBeforeKneeding, double componentsQ, double? waterTargetQuantity, out double defaultWaterTemp)
        {
            double suggestedWaterTemperature = 20;
            defaultWaterTemp = 0; //TODO

            double? waterSpecHeatCapacity = waterComp.SpecHeatCapacity;

            if (calculateWaterTemp)
            {
                if (isOnlyWater)
                {
                    suggestedWaterTemperature = doughTargetTempAfterKneeding;
                    defaultWaterTemp = doughTargetTempAfterKneeding;
                }
                else
                {
                    double waterTemp = WaterTemp.Value;

                    if (waterTemp > 0.00001 || waterTemp < -0.0001)
                    {
                        suggestedWaterTemperature = WaterTemp.Value;
                    }
                }
            }
            else
            {
                if (isOnlyWater)
                {
                    suggestedWaterTemperature = doughTargetTempAfterKneeding;
                    defaultWaterTemp = doughTargetTempAfterKneeding;
                }
                else if (waterTargetQuantity.HasValue && waterSpecHeatCapacity.HasValue && waterTargetQuantity > 0.00001 && waterSpecHeatCapacity > 0.00001)
                {
                    suggestedWaterTemperature = (componentsQ / (waterTargetQuantity.Value * waterSpecHeatCapacity.Value)) + doughTargetTempBeforeKneeding;
                }
            }

            return suggestedWaterTemperature;
        }



        private void CalculateWaterTypes(IEnumerable<MaterialTemperature> componentTemperatures, double targetWaterTemperature, double totalWaterQuantity, double defaultWaterTemp,
                                         double componentsQ, bool isOnlyWaterCompInPartslist, double doughTempBeforeKneeding, double? kneedingRiseTemp)
        {
            MaterialTemperature coldWater = componentTemperatures.FirstOrDefault(c => c.Water == WaterType.ColdWater);
            MaterialTemperature cityWater = componentTemperatures.FirstOrDefault(c => c.Water == WaterType.CityWater);
            MaterialTemperature warmWater = componentTemperatures.FirstOrDefault(c => c.Water == WaterType.WarmWater);
            MaterialTemperature dryIce = componentTemperatures.FirstOrDefault(c => c.Water == WaterType.DryIce);

            if (coldWater == null)
            {
                //The component/material {0} can not be found!
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CalculateWaterTypes(10)", 800, "Error50406", "ColdWater");
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                {
                    OnNewAlarmOccurred(ProcessAlarm, msg);
                    Root.Messages.LogMessageMsg(msg);
                }
                return;
            }

            if (cityWater == null)
            {
                //The component/material {0} can not be found!
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CalculateWaterTypes(20)", 800, "Error50406", "CityWater");
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                {
                    OnNewAlarmOccurred(ProcessAlarm, msg);
                    Root.Messages.LogMessageMsg(msg);
                }
                return;
            }

            if (warmWater == null)
            {
                //The component/material {0} can not be found!
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CalculateWaterTypes(30)", 800, "Error50406", "WarmWater");
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                {
                    OnNewAlarmOccurred(ProcessAlarm, msg);
                    Root.Messages.LogMessageMsg(msg);
                }
                return;
            }

            if (dryIce == null)
            {
                //The component/material {0} can not be found!
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CalculateWaterTypes(40)", 800, "Error50406", "Ice");
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                {
                    OnNewAlarmOccurred(ProcessAlarm, msg);
                    Root.Messages.LogMessageMsg(msg);
                }
                return;
            }

            ACValueList temperaturesUsedInCalc = new ACValueList(componentTemperatures.Where(c => c.Water != WaterType.NotWater).Select(x => new ACValue(x.Water.ToString(), x.AverageTemperature)).ToArray());
            if (kneedingRiseTemp.HasValue)
            {
                ACValue acValue = new ACValue(KneedingRiseTemp, kneedingRiseTemp.Value);
                temperaturesUsedInCalc.Add(acValue);
            }

            WaterTemperaturesUsedInCalc = temperaturesUsedInCalc;
            WaterTotalQuantity.ValueT = totalWaterQuantity;

            //check warm water 
            if (targetWaterTemperature > warmWater.AverageTemperature)
            {
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    WarmWaterQuantity.ValueT = totalWaterQuantity;
                }

                // The calculated water temperature of {0} °C can not be reached, the maximum water temperature is {1} °C and the target quantity is {2} {3}. 
                TemperatureCalculationResult.ValueT = Root.Environment.TranslateText(this, "TempCalcResultMax", targetWaterTemperature.ToString("F2"), warmWater.AverageTemperature,
                                                                                                                totalWaterQuantity.ToString("F2"), "kg");
                WaterCalcResult.ValueT = targetWaterTemperature;

                return;
            }

            if (CombineWarmCityWater(warmWater, cityWater, targetWaterTemperature, totalWaterQuantity))
            {
                //The calculated water temperature is {0} °C and the target quantity is {1} {2}.
                TemperatureCalculationResult.ValueT = Root.Environment.TranslateText(this, "TempCalcResult", targetWaterTemperature.ToString("F2"), 
                                                                                                             totalWaterQuantity.ToString("F2"), "kg");
                WaterCalcResult.ValueT = targetWaterTemperature;
                return;
            }

            if (CombineColdCityWater(coldWater, cityWater, targetWaterTemperature, totalWaterQuantity))
            {
                //The calculated water temperature is {0} °C and the target quantity is {1} {2}.
                TemperatureCalculationResult.ValueT = Root.Environment.TranslateText(this, "TempCalcResult", targetWaterTemperature.ToString("F2"), 
                                                                                                             totalWaterQuantity.ToString("F2"), "kg");
                WaterCalcResult.ValueT = targetWaterTemperature;
                return;
            }

            if (!CombineWatersWithDryIce(coldWater, dryIce, targetWaterTemperature, totalWaterQuantity, defaultWaterTemp, isOnlyWaterCompInPartslist, componentsQ, doughTempBeforeKneeding))
            {
                // The calculated water temperature of {0} °C can not be reached, the ice is {1} °C and the target quantity is {2} {3}. 
                TemperatureCalculationResult.ValueT = Root.Environment.TranslateText(this, "TempCalcResultMin", targetWaterTemperature.ToString("F2"), dryIce.AverageTemperature,
                                                                                                                totalWaterQuantity.ToString("F2"), "kg");
                WaterCalcResult.ValueT = targetWaterTemperature;
                return;
            }

            //The calculated water temperature is {0} °C and the target quantity is {1} {2}.
            TemperatureCalculationResult.ValueT = Root.Environment.TranslateText(this, "TempCalcResult", targetWaterTemperature.ToString("F2"), 
                                                                                                         totalWaterQuantity.ToString("F2"), "kg");
            WaterCalcResult.ValueT = targetWaterTemperature;
        }

        private bool CombineWarmCityWater(MaterialTemperature warmWater, MaterialTemperature cityWater, double targetWaterTemperature, double totalWaterQuantity)
        {
            if (targetWaterTemperature <= warmWater.AverageTemperature && targetWaterTemperature > cityWater.AverageTemperature)
            {
                double warmWaterSHC = warmWater.Material.SpecHeatCapacity;
                double cityWaterSHC = cityWater.Material.SpecHeatCapacity;

                double warmWaterQuantity = (totalWaterQuantity * (cityWaterSHC * (cityWater.AverageTemperature.Value - targetWaterTemperature)))
                                           / ((cityWaterSHC * (cityWater.AverageTemperature.Value - targetWaterTemperature))
                                           + (warmWaterSHC * (targetWaterTemperature - warmWater.AverageTemperature.Value)));

                double cityWaterQuantity = totalWaterQuantity - warmWaterQuantity;

                if (cityWaterQuantity < cityWater.WaterMinDosingQuantity)
                {
                    warmWaterQuantity = totalWaterQuantity;
                    cityWaterQuantity = 0;
                }
                else if (warmWaterQuantity < warmWater.WaterMinDosingQuantity)
                {
                    warmWaterQuantity = 0;
                    cityWaterQuantity = totalWaterQuantity;
                }

                using (ACMonitor.Lock(_20015_LockValue))
                {
                    WarmWaterQuantity.ValueT = warmWaterQuantity;
                    CityWaterQuantity.ValueT = cityWaterQuantity;
                }

                return true;
            }

            return false;
        }

        private bool CombineColdCityWater(MaterialTemperature coldWater, MaterialTemperature cityWater, double targetWaterTemperature, double totalWaterQuantity)
        {
            if (targetWaterTemperature <= cityWater.AverageTemperature && targetWaterTemperature > coldWater.AverageTemperature)
            {
                double coldWaterSHC = coldWater.Material.SpecHeatCapacity;
                double cityWaterSHC = cityWater.Material.SpecHeatCapacity;

                double cityWaterQuantity = (totalWaterQuantity * (coldWaterSHC * (coldWater.AverageTemperature.Value - targetWaterTemperature)))
                                           / ((coldWaterSHC * (coldWater.AverageTemperature.Value - targetWaterTemperature))
                                           + (cityWaterSHC * (targetWaterTemperature - cityWater.AverageTemperature.Value)));

                double coldWaterQuantity = totalWaterQuantity - cityWaterQuantity;

                if (coldWaterQuantity < coldWater.WaterMinDosingQuantity)
                {
                    cityWaterQuantity = totalWaterQuantity;
                    coldWaterQuantity = 0;
                }
                else if (cityWaterQuantity < cityWater.WaterMinDosingQuantity)
                {
                    coldWaterQuantity = totalWaterQuantity;
                    cityWaterQuantity = 0;
                }

                using (ACMonitor.Lock(_20015_LockValue))
                {
                    ColdWaterQuantity.ValueT = coldWaterQuantity;
                    CityWaterQuantity.ValueT = cityWaterQuantity;
                }

                return true;
            }

            return false;
        }

        private bool CombineWatersWithDryIce(MaterialTemperature coldWater, MaterialTemperature dryIce, double targetWaterTemperature, double totalWaterQuantity,
                                             double defaultWaterTemp, bool isOnlyWaterCompInPartslist, double componentsQ, double doughTempBeforeKneeding)
        {
            if (IncludeMeltingHeat == MeltingHeatOptionEnum.Off
                || (IncludeMeltingHeat == MeltingHeatOptionEnum.OnlyForDoughTempCalc && CalculateWaterTypesWithComponentsQ(defaultWaterTemp, isOnlyWaterCompInPartslist)))
            {
                if (targetWaterTemperature <= coldWater.AverageTemperature && targetWaterTemperature > dryIce.AverageTemperature)
                {
                    double waterSHC = coldWater.Material.SpecHeatCapacity;

                    double coldWaterQuantity = (totalWaterQuantity * (waterSHC * (dryIce.AverageTemperature.Value - targetWaterTemperature))) /
                                               ((waterSHC * (dryIce.AverageTemperature.Value - targetWaterTemperature)) + (waterSHC * (targetWaterTemperature - coldWater.AverageTemperature.Value)));
                    double dryIceQuantity = totalWaterQuantity - coldWaterQuantity;

                    if (coldWaterQuantity < coldWater.WaterMinDosingQuantity)
                    {
                        coldWaterQuantity = 0 ;
                        dryIceQuantity = totalWaterQuantity;
                    }
                    else if (dryIceQuantity < coldWater.WaterMinDosingQuantity) // TODO: find manual scale on receiving point and get min weighing quantity
                    {
                        coldWaterQuantity = totalWaterQuantity;
                        dryIceQuantity = 0;
                    }

                    using (ACMonitor.Lock(_20015_LockValue))
                    {
                        ColdWaterQuantity.ValueT = coldWaterQuantity;
                        DryIceQuantity.ValueT = dryIceQuantity;
                    }

                    return true;
                }
                else
                {
                    using (ACMonitor.Lock(_20015_LockValue))
                    {
                        ColdWaterQuantity.ValueT = 0;
                        DryIceQuantity.ValueT = totalWaterQuantity;
                    }
                    return false;
                }
            }
            else
            {
                if (CalculateWaterTypesWithComponentsQ(defaultWaterTemp, isOnlyWaterCompInPartslist))
                {
                    double deltaTemp = defaultWaterTemp - targetWaterTemperature;
                    double deltaTempCold = coldWater.AverageTemperature.Value + deltaTemp;

                    double waterSHC = coldWater.Material.SpecHeatCapacity;

                    double iceQuantity = (componentsQ + totalWaterQuantity * waterSHC * (doughTempBeforeKneeding - deltaTempCold))
                                        / ((WaterMeltingHeat.Value * MeltingHeatInfluence.Value) - waterSHC * (doughTempBeforeKneeding - deltaTempCold) + waterSHC
                                        * (doughTempBeforeKneeding - dryIce.AverageTemperature.Value));

                    double coldWaterQuantity = 0;

                    if (iceQuantity < 0)
                        iceQuantity *= -1;

                    if (iceQuantity <= totalWaterQuantity)
                    {
                        coldWaterQuantity = totalWaterQuantity - iceQuantity;
                        if (coldWaterQuantity < coldWater.WaterMinDosingQuantity)
                        {
                            iceQuantity = totalWaterQuantity;
                            coldWaterQuantity = 0;
                        }
                    }
                    else
                    {
                        coldWaterQuantity = (totalWaterQuantity * (waterSHC * (dryIce.AverageTemperature.Value - targetWaterTemperature)))
                                           / ((waterSHC * (dryIce.AverageTemperature.Value - targetWaterTemperature))
                                           + (waterSHC * (targetWaterTemperature - coldWater.AverageTemperature.Value)));

                        iceQuantity = totalWaterQuantity - coldWaterQuantity;

                        if (iceQuantity < dryIce.WaterMinDosingQuantity) //TODO find min dosing quantity
                        {
                            coldWaterQuantity = totalWaterQuantity;
                            iceQuantity = 0;
                        }
                        else if (coldWaterQuantity < coldWater.WaterMinDosingQuantity)
                        {
                            coldWaterQuantity = 0;
                            iceQuantity = totalWaterQuantity;
                        }
                    }

                    using (ACMonitor.Lock(_20015_LockValue))
                    {
                        ColdWaterQuantity.ValueT = coldWaterQuantity;
                        DryIceQuantity.ValueT = iceQuantity;
                    }
                }
                else
                {
                    double waterSHC = coldWater.Material.SpecHeatCapacity;
                    double iceSCH = dryIce.Material.SpecHeatCapacity;

                    if (Math.Abs(waterSHC) <= Double.Epsilon)
                    {
                        Msg msg = new Msg("Please configure specific heat capacity for {0}.", this, eMsgLevel.Error, PWClassName, "CombineWatersWithDryIce()", 1133);
                        if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                        {
                            Messages.LogMessageMsg(msg);
                        }
                        OnNewAlarmOccurred(ProcessAlarm, msg, true);
                    }

                    if (Math.Abs(iceSCH) <= Double.Epsilon)
                    {
                        Msg msg = new Msg("Please configure specific heat capacity for {0}.", this, eMsgLevel.Error, PWClassName, "CombineWatersWithDryIce()", 1143);
                        if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                        {
                            Messages.LogMessageMsg(msg);
                        }
                        OnNewAlarmOccurred(ProcessAlarm, msg, true);
                    }


                    if (targetWaterTemperature <= coldWater.AverageTemperature && targetWaterTemperature > dryIce.AverageTemperature)
                    {
                        bool ice = true; //TODO: ask Damir

                        if (ice)
                        {
                            double iceQuantity = (waterSHC * totalWaterQuantity * (coldWater.AverageTemperature.Value - targetWaterTemperature))
                                                 / ((iceSCH * (0 - dryIce.AverageTemperature.Value)) + (WaterMeltingHeat.Value * MeltingHeatInfluence.Value)
                                                 + (waterSHC * (targetWaterTemperature - 0)));

                            double coldWaterQuantity = 0;

                            if (iceQuantity <= totalWaterQuantity)
                            {
                                coldWaterQuantity = totalWaterQuantity - iceQuantity;
                                if (coldWaterQuantity < coldWater.WaterMinDosingQuantity)
                                {
                                    iceQuantity = totalWaterQuantity;
                                    coldWaterQuantity = 0;
                                }
                            }

                            using (ACMonitor.Lock(_20015_LockValue))
                            {
                                DryIceQuantity.ValueT = iceQuantity;
                                ColdWaterQuantity.ValueT = coldWaterQuantity;
                            }
                        }
                        else
                        {
                            double iceQuantity = (totalWaterQuantity * (coldWater.AverageTemperature.Value - targetWaterTemperature)) /
                                                 (coldWater.AverageTemperature.Value - dryIce.AverageTemperature.Value);
                            double coldWaterQuantity = totalWaterQuantity - iceQuantity;

                            if (iceQuantity < dryIce.WaterMinDosingQuantity)
                            {
                                coldWaterQuantity = totalWaterQuantity;
                                iceQuantity = 0;
                            }
                            else if (coldWaterQuantity < coldWater.WaterMinDosingQuantity)
                            {
                                coldWaterQuantity = 0;
                                iceQuantity = totalWaterQuantity;
                            }

                            using (ACMonitor.Lock(_20015_LockValue))
                            {
                                DryIceQuantity.ValueT = iceQuantity;
                                ColdWaterQuantity.ValueT = coldWaterQuantity;
                            }
                        }
                    }
                    else
                    {
                        // The water temperature calculation can not be performed.
                        Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CombineWatersWithDryIce(10)", 1173, "Error50422");
                        if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                        {
                            OnNewAlarmOccurred(ProcessAlarm, msg);
                            Root.Messages.LogMessageMsg(msg);
                        }
                    }
                }
            }
            return true;
        }

        private bool CalculateWaterTypesWithComponentsQ(double defaultWaterTemp, bool isOnlyWaterCompInPartslist)
        {
            if (defaultWaterTemp < 0.00001 && !isOnlyWaterCompInPartslist)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Methods => ModifyProdOrderPartslist/Picking

        public void AdjustOrder()
        {
            if (IsProduction)
            {
                AdjustWatersInProdOrderPartslist();
            }
            else if (IsRelocation)
            {
                AdjustWatersInPicking();
            }
        }

        public void AdjustWatersInProdOrderPartslist()
        {
            double cityWaterQ = 0, coldWaterQ = 0, warmWaterQ = 0, dryIceQ = 0;

            using (ACMonitor.Lock(_20015_LockValue))
            {
                cityWaterQ = Math.Round(CityWaterQuantity.ValueT, 2);
                coldWaterQ = Math.Round(ColdWaterQuantity.ValueT, 2);
                warmWaterQ = Math.Round(WarmWaterQuantity.ValueT, 2);
                dryIceQ = Math.Round(DryIceQuantity.ValueT, 2);
            }

            if (cityWaterQ < 0.0001 && coldWaterQ < 0.0001 && warmWaterQ < 0.0001 && dryIceQ < 0.0001)
            {
                // Error50412: The production order can not be adjusted by temperature calculator. Waters and/or ice do not have sufficient target quantity.
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "AdjustWatersInProdOrderPartslist(10)", 1225, "Error50412");
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                {
                    OnNewAlarmOccurred(ProcessAlarm, msg);
                    Root.Messages.LogMessageMsg(msg);
                }
                return;
            }

            if (string.IsNullOrEmpty(_CityWaterMaterialNo) || string.IsNullOrEmpty(_ColdWaterMaterialNo) || string.IsNullOrEmpty(_WarmWaterMaterialNo) || string.IsNullOrEmpty(DryIceMaterialNo))
            {
                // Error50413: The production order can not be adjusted by temperature calculator. Waters and/or ice material numbers are missing.
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "AdjustWatersInProdOrderPartslist(20)", 1237, "Error50413");
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                {
                    OnNewAlarmOccurred(ProcessAlarm, msg);
                    Root.Messages.LogMessageMsg(msg);
                }
                return;
            }

            PWMethodProduction pwMethodProduction = ParentPWMethod<PWMethodProduction>();
            // If dosing is not for production, then do nothing
            if (pwMethodProduction == null)
                return;

            using (Database db = new gip.core.datamodel.Database())
            using (DatabaseApp dbApp = new DatabaseApp(db))
            {
                ProdOrderPartslistPos endBatchPos = pwMethodProduction.CurrentProdOrderPartslistPos.FromAppContext<ProdOrderPartslistPos>(dbApp);

                if (pwMethodProduction.CurrentProdOrderBatch == null)
                {
                    // Error50411: No batch assigned to last intermediate material of this workflow
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "AdjustWatersInProdOrderPartslist(30)", 1259, "Error50411");

                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                        Messages.LogError(this.GetACUrl(), msg.ACIdentifier, msg.InnerMessage);
                    OnNewAlarmOccurred(ProcessAlarm, msg, false);
                    return;
                }

                var contentACClassWFVB = ContentACClassWF.FromAppContext<gip.mes.datamodel.ACClassWF>(dbApp);
                ProdOrderBatch batch = pwMethodProduction.CurrentProdOrderBatch.FromAppContext<ProdOrderBatch>(dbApp);
                ProdOrderBatchPlan batchPlan = batch.ProdOrderBatchPlan;

                PartslistACClassMethod plMethod = endBatchPos.ProdOrderPartslist.Partslist.PartslistACClassMethod_Partslist.FirstOrDefault();
                ProdOrderPartslist currentProdOrderPartslist = endBatchPos.ProdOrderPartslist.FromAppContext<ProdOrderPartslist>(dbApp);

                IEnumerable<MaterialWFConnection> matWFConnections = null;
                if (batchPlan != null && batchPlan.MaterialWFACClassMethodID.HasValue)
                {
                    matWFConnections = dbApp.MaterialWFConnection
                                            .Where(c => c.MaterialWFACClassMethod.MaterialWFACClassMethodID == batchPlan.MaterialWFACClassMethodID.Value
                                                    && c.ACClassWFID == contentACClassWFVB.ACClassWFID).ToArray();
                }

                if (matWFConnections == null || !matWFConnections.Any())
                {
                    // Error50415: No relation defined between Workflownode and intermediate material in Materialworkflow
                    Msg msg1 = new Msg(this, eMsgLevel.Error, PWClassName, "AdjustWatersInProdOrderPartslist(40)", 1285, "Error50415");

                    if (IsAlarmActive(ProcessAlarm, msg1.Message) == null)
                        Messages.LogError(this.GetACUrl(), msg1.ACIdentifier, msg1.InnerMessage);
                    OnNewAlarmOccurred(ProcessAlarm, msg1, false);
                    return;
                }

                var components = currentProdOrderPartslist.ProdOrderPartslistPos_ProdOrderPartslist.Where(c => c.MaterialID.HasValue && c.MaterialPosType == GlobalApp.MaterialPosTypes.OutwardRoot);

                Material intermediateAutomatic = matWFConnections.FirstOrDefault(c => c.Material.MaterialWFConnection_Material
                                                                                       .FirstOrDefault(x => _PWDosingType.IsAssignableFrom(x.ACClassWF.PWACClass.FromIPlusContext<gip.core.datamodel.ACClass>(db).ObjectType)) != null)?.Material;

                Material intermediateManual = matWFConnections.FirstOrDefault(c => c.Material.MaterialWFConnection_Material
                                                                                       .FirstOrDefault(x => _PWManualWeighingType.IsAssignableFrom(x.ACClassWF.PWACClass.FromIPlusContext<gip.core.datamodel.ACClass>(db).ObjectType)) != null)?.Material; ;

                if (intermediateManual == null)
                {
                    // Error50414: The material workflow is not properly connected with a process workflow or process workflow is not correcty designed. The process workflow at least must have the one
                    // PWManualWeighing node and must be connected with a intermediate material.
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "AdjustWatersInProdOrderPartslist(50)", 1305, "Error50414");

                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                        Messages.LogError(this.GetACUrl(), msg.ACIdentifier, msg.InnerMessage);
                    OnNewAlarmOccurred(ProcessAlarm, msg, false);
                    return;
                }

                if (UseWaterMixer)
                    cityWaterQ = cityWaterQ + coldWaterQ + warmWaterQ;

                double totalWatersQuantity = cityWaterQ + coldWaterQ + warmWaterQ + dryIceQ;

                ProdOrderPartslistPos posCity = components.FirstOrDefault(c => c.Material.MaterialNo == _CityWaterMaterialNo);
                if (posCity == null)
                {
                    posCity = AddProdOrderPartslistPos(dbApp, currentProdOrderPartslist, _CityWaterMaterialNo, components);
                    if (posCity == null)
                        return;
                }

                Material intermediateCity = intermediateAutomatic != null ? intermediateAutomatic : intermediateManual;

                AdjustBatchPosInProdOrderPartslist(dbApp, currentProdOrderPartslist, intermediateCity, posCity, batch, cityWaterQ, totalWatersQuantity);


                if (!UseWaterMixer)
                {
                    if (coldWaterQ > 0.00001)
                    {
                        ProdOrderPartslistPos pos = components.FirstOrDefault(c => c.Material.MaterialNo == _ColdWaterMaterialNo);
                        if (pos == null)
                        {
                            pos = AddProdOrderPartslistPos(dbApp, currentProdOrderPartslist, _ColdWaterMaterialNo, components);
                            if (pos == null)
                                return;
                        }

                        Material intermediate = intermediateAutomatic != null ? intermediateAutomatic : intermediateManual;

                        AdjustBatchPosInProdOrderPartslist(dbApp, currentProdOrderPartslist, intermediate, pos, batch, coldWaterQ, totalWatersQuantity);
                    }

                    if (warmWaterQ > 0.00001)
                    {
                        ProdOrderPartslistPos pos = components.FirstOrDefault(c => c.Material.MaterialNo == _WarmWaterMaterialNo);
                        if (pos == null)
                        {
                            pos = AddProdOrderPartslistPos(dbApp, currentProdOrderPartslist, _WarmWaterMaterialNo, components);
                            if (pos == null)
                                return;
                        }

                        Material intermediate = intermediateAutomatic != null ? intermediateAutomatic : intermediateManual;

                        AdjustBatchPosInProdOrderPartslist(dbApp, currentProdOrderPartslist, intermediate, pos, batch, warmWaterQ, totalWatersQuantity);
                    }
                }

                if (dryIceQ > 0.00001 && !string.IsNullOrEmpty(DryIceMaterialNo))
                {
                    ProdOrderPartslistPos pos = components.FirstOrDefault(c => c.Material.MaterialNo == DryIceMaterialNo);
                    if (pos == null)
                    {
                        pos = AddProdOrderPartslistPos(dbApp, currentProdOrderPartslist, DryIceMaterialNo, components);
                        if (pos == null)
                            return;
                    }

                    if (intermediateManual != null)
                    {
                        AdjustBatchPosInProdOrderPartslist(dbApp, currentProdOrderPartslist, intermediateManual, pos, batch, dryIceQ, totalWatersQuantity);
                    }
                }

                Msg result = dbApp.ACSaveChanges();
                if (result != null)
                {
                    if (IsAlarmActive(ProcessAlarm, result.Message) == null)
                        Messages.LogError(this.GetACUrl(), result.ACIdentifier, result.InnerMessage);
                    OnNewAlarmOccurred(ProcessAlarm, result, false);
                }
            }
        }

        private ProdOrderPartslistPos AddProdOrderPartslistPos(DatabaseApp dbApp, ProdOrderPartslist poPartslist, string waterMaterialNo,
                                                               IEnumerable<ProdOrderPartslistPos> components)
        {
            Material water = dbApp.Material.FirstOrDefault(c => c.MaterialNo == waterMaterialNo);
            if (water == null)
            {
                // Error50416: The production order can not be adjusted by temperature calculator. The material with material No {0} can not be found in the database.
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "AddProdOrderPartslistPos(10)", 1397, "Error50416", waterMaterialNo);
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                {
                    OnNewAlarmOccurred(ProcessAlarm, msg);
                    Root.Messages.LogMessageMsg(msg);
                }

                return null;
            }

            ProdOrderPartslistPos pos = ProdOrderPartslistPos.NewACObject(dbApp, poPartslist);
            pos.Material = water;
            pos.MaterialPosTypeIndex = (short)GlobalApp.MaterialPosTypes.OutwardRoot;
            pos.Sequence = 1;
            if (components.Any())
            {
                pos.Sequence = components.Max(x => x.Sequence) + 1;
            }
            pos.MDUnit = water.BaseMDUnit;

            dbApp.ProdOrderPartslistPos.AddObject(pos);

            return pos;
        }

        private void AdjustBatchPosInProdOrderPartslist(DatabaseApp dbApp, ProdOrderPartslist poPartslist, Material intermediateMaterial, ProdOrderPartslistPos sourcePos, ProdOrderBatch batch,
                                                         double waterQuantity, double totalWatersQuantity)
        {
            ProdOrderPartslistPos targetPos = poPartslist.ProdOrderPartslistPos_ProdOrderPartslist
                                                             .FirstOrDefault(c => c.MaterialID == intermediateMaterial.MaterialID
                                                                               && c.MaterialPosType == GlobalApp.MaterialPosTypes.InwardIntern);

            if (targetPos == null)
            {
                targetPos = ProdOrderPartslistPos.NewACObject(dbApp, null);
                targetPos.Sequence = 1;
                targetPos.MaterialPosTypeIndex = (short)GlobalApp.MaterialPosTypes.InwardIntern;
                dbApp.ProdOrderPartslistPos.AddObject(targetPos);
            }

            ProdOrderPartslistPosRelation topRelation = sourcePos.ProdOrderPartslistPosRelation_SourceProdOrderPartslistPos
                                                           .FirstOrDefault(c => c.TargetProdOrderPartslistPos.MaterialID == targetPos.MaterialID
                                                                             && c.TargetProdOrderPartslistPos.MaterialPosType == GlobalApp.MaterialPosTypes.InwardIntern);

            if (topRelation == null)
            {
                topRelation = ProdOrderPartslistPosRelation.NewACObject(dbApp, null);
                topRelation.SourceProdOrderPartslistPos = sourcePos;
                topRelation.TargetProdOrderPartslistPos = targetPos;
                topRelation.Sequence = targetPos.ProdOrderPartslistPosRelation_TargetProdOrderPartslistPos
                                                .Where(c => c.TargetProdOrderPartslistPos.MaterialID == targetPos.MaterialID
                                                         && c.TargetProdOrderPartslistPos.MaterialPosType == GlobalApp.MaterialPosTypes.InwardIntern)
                                                .Max(x => x.Sequence) + 1;

                dbApp.ProdOrderPartslistPosRelation.AddObject(topRelation);
            }

            ProdOrderPartslistPosRelation batchRelation = batch.ProdOrderPartslistPosRelation_ProdOrderBatch
                                                               .FirstOrDefault(c => c.ParentProdOrderPartslistPosRelationID == topRelation.ProdOrderPartslistPosRelationID);

            ProdOrderPartslistPos batchPos = batch.ProdOrderPartslistPos_ProdOrderBatch.FirstOrDefault(c => c.MaterialID == intermediateMaterial.MaterialID
                                                                                     && c.MaterialPosType == GlobalApp.MaterialPosTypes.InwardPartIntern);

            if (batchPos == null)
            {
                batchPos = ProdOrderPartslistPos.NewACObject(dbApp, targetPos);
                batchPos.Sequence = 1;
                var existingBatchPos = batch.ProdOrderPartslistPos_ProdOrderBatch.Where(c => c.MaterialID == intermediateMaterial.MaterialID
                                                                                     && c.MaterialPosType == GlobalApp.MaterialPosTypes.InwardPartIntern);

                if (existingBatchPos != null && existingBatchPos.Any())
                    batchPos.Sequence = existingBatchPos.Max(c => c.Sequence) + 1;

                batchPos.TargetQuantityUOM = totalWatersQuantity;
                batchPos.ProdOrderBatch = batch;
                batchPos.MDUnit = targetPos.MDUnit;
                targetPos.ProdOrderPartslistPos_ParentProdOrderPartslistPos.Add(batchPos);
            }

            targetPos.CalledUpQuantityUOM += waterQuantity;

            if (batchRelation == null)
            {
                batchRelation = ProdOrderPartslistPosRelation.NewACObject(dbApp, topRelation);
                batchRelation.Sequence = topRelation.Sequence;
                batchRelation.TargetProdOrderPartslistPos = batchPos;
                batchRelation.SourceProdOrderPartslistPos = topRelation.SourceProdOrderPartslistPos;
                batchRelation.ProdOrderBatch = batch;
            }

            batchRelation.TargetQuantityUOM = waterQuantity;
        }

        private void AdjustWatersInPicking()
        {
            double cityWaterQ = 0, coldWaterQ = 0, warmWaterQ = 0, dryIceQ = 0;

            using (ACMonitor.Lock(_20015_LockValue))
            {
                cityWaterQ = Math.Round(CityWaterQuantity.ValueT, 2);
                coldWaterQ = Math.Round(ColdWaterQuantity.ValueT, 2);
                warmWaterQ = Math.Round(WarmWaterQuantity.ValueT, 2);
                dryIceQ = Math.Round(DryIceQuantity.ValueT, 2);
            }

            if (cityWaterQ < 0.0001 && coldWaterQ < 0.0001 && warmWaterQ < 0.0001 && dryIceQ < 0.0001)
            {
                // Error50417: The picking order can not be adjusted by temperature calculator. Waters and/or ice does not have sufficient target quantity.
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "AdjustWatersInPicking(10)", 1505, "Error50417");
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                {
                    OnNewAlarmOccurred(ProcessAlarm, msg);
                    Root.Messages.LogMessageMsg(msg);
                }
                return;
            }

            if (string.IsNullOrEmpty(_CityWaterMaterialNo) || string.IsNullOrEmpty(_ColdWaterMaterialNo) || string.IsNullOrEmpty(_WarmWaterMaterialNo) || string.IsNullOrEmpty(DryIceMaterialNo))
            {
                // Error50418: The picking order can not be adjusted by temperature calculator. Waters and/or ice material numbers are missing.
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "AdjustWatersInPicking(20)", 1517, "Error50418");
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                {
                    OnNewAlarmOccurred(ProcessAlarm, msg);
                    Root.Messages.LogMessageMsg(msg);
                }
                return;
            }

            PWMethodRelocation pwMethodRelocation = ParentPWMethod<PWMethodRelocation>();
            if (pwMethodRelocation == null)
                return;

            if (!UseWaterMixer)
            {
                using (DatabaseApp dbApp = new DatabaseApp())
                {
                    Picking picking = pwMethodRelocation.CurrentPicking.FromAppContext<Picking>(dbApp);
                    if (picking == null)
                        return;


                    PickingPos cityWaterPos = picking.PickingPos_Picking.FirstOrDefault(c => c.Material.MaterialNo == _CityWaterMaterialNo);
                    if (cityWaterPos == null)
                        return;

                    Facility targetFacility = cityWaterPos.ToFacility;

                    cityWaterPos.PickingQuantityUOM = cityWaterQ;

                    if (coldWaterQ > 0.00001)
                    {
                        PickingPos coldWaterPos = picking.PickingPos_Picking.FirstOrDefault(c => c.Material.MaterialNo == _ColdWaterMaterialNo);
                        if (coldWaterPos == null)
                        {
                            coldWaterPos = AddPickingPos(dbApp, picking, _ColdWaterMaterialNo, targetFacility);
                            if (coldWaterPos == null)
                                return;
                        }
                        coldWaterPos.PickingQuantityUOM = coldWaterQ;
                    }

                    if (warmWaterQ > 0.00001)
                    {
                        PickingPos warmWaterPos = picking.PickingPos_Picking.FirstOrDefault(c => c.Material.MaterialNo == _WarmWaterMaterialNo);
                        if (warmWaterPos == null)
                        {
                            warmWaterPos = AddPickingPos(dbApp, picking, _WarmWaterMaterialNo, targetFacility);
                            if (warmWaterPos == null)
                                return;
                        }
                        warmWaterPos.PickingQuantityUOM = warmWaterQ;
                    }

                    //TODO: ice, picking in manual weighing 

                    Msg result = dbApp.ACSaveChanges();
                    if (result != null)
                    {
                        if (IsAlarmActive(ProcessAlarm, result.Message) == null)
                            Messages.LogError(this.GetACUrl(), result.ACIdentifier, result.InnerMessage);
                        OnNewAlarmOccurred(ProcessAlarm, result, false);
                    }
                }
            }
        }

        private PickingPos AddPickingPos(DatabaseApp dbApp, Picking picking, string materialNo, Facility toFacility)
        {
            Material material = dbApp.Material.FirstOrDefault(c => c.MaterialNo == materialNo);
            if (material == null)
            {
                // Error50419: The picking order can not be adjusted by temperature calculator. The material with material No {0} can not be found in the database.
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "AddPickingPos(10)", 1590, "Error50419", materialNo);
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                {
                    OnNewAlarmOccurred(ProcessAlarm, msg);
                    Root.Messages.LogMessageMsg(msg);
                }

                return null;
            }

            Facility source = null;
            var possibleSources = material.Facility_Material.ToList();
            if (possibleSources.Count > 1)
            {
                IEnumerable<string> sources = possibleSources.Where(c => c.VBiFacilityACClassID != null).Select(x => x.VBiFacilityACClass.ACURLComponentCached);
                gip.core.datamodel.ACClass module = ParentPWGroup.AccessedProcessModule.ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(dbApp.ContextIPlus);

                RoutingResult rResult = ACRoutingService.SelectRoutes(RoutingService, dbApp.ContextIPlus, false, module, sources, RouteDirections.Backwards, PAMTank.SelRuleID_Silo,
                                                                      null, null, null, 10, true, true);

                if (rResult != null)
                {
                    if (rResult.Routes.Any())
                    {
                        Route route = rResult.Routes.FirstOrDefault();
                        source = possibleSources.FirstOrDefault(c => c.VBiFacilityACClass.ACClassID == route.GetRouteSource().SourceGuid);
                    }
                }
            }
            else
            {
                source = possibleSources.FirstOrDefault();
            }

            PickingPos pos = PickingPos.NewACObject(dbApp, picking);
            pos.PickingMaterial = material;
            pos.Sequence = picking.PickingPos_Picking.Max(c => c.Sequence) + 1;
            pos.FromFacility = source;
            pos.ToFacility = toFacility;
            picking.PickingPos_Picking.Add(pos);
            dbApp.PickingPos.AddObject(pos);

            return pos;
        }

        #endregion

        #region Methods => Components temperature

        private List<MaterialTemperature> DetermineComponentsTemperature(ProdOrderPartslist prodOrderPartslist, IEnumerable<ProdOrderPartslistPos> intermediates,
                                                                         BakeryReceivingPoint recvPoint, PartslistACClassMethod plMethod, DatabaseApp dbApp,
                                                                         List<MaterialTemperature> tempFromService, string matNoColdWater,
                                                                         string matNoCityWater, string matNoWarmWater, string matNoDryIce)
        {
            Guid? recvPointID = recvPoint?.ComponentClass?.ACClassID;
            if (!recvPointID.HasValue)
            {
                //TODO: error
                return null;
            }

            IEnumerable<PartslistPos> partslistPosList = prodOrderPartslist.Partslist?.PartslistPos_Partslist.Where(c => c.IsOutwardRoot).ToArray();

            List<MaterialTemperature> componentTemp = partslistPosList.Select(c => new MaterialTemperature()
            {
                Material = c.Material,
                AverageTemperature = c.Material.MaterialConfig_Material
                                      .FirstOrDefault(x => x.KeyACUrl == PABakeryTempService.MaterialTempertureConfigKeyACUrl
                                                        && x.VBiACClassID == recvPointID)?.Value as double?
            }).ToList();

            var cityWater = componentTemp.FirstOrDefault(c => c.Material.MaterialNo == matNoCityWater);
            if (cityWater != null)
                cityWater.Water = WaterType.CityWater;

            var coldWater = componentTemp.FirstOrDefault(c => c.Material.MaterialNo == matNoColdWater);
            if (cityWater != null && coldWater == null)
            {
                Material mat = dbApp.Material.FirstOrDefault(c => c.MaterialNo == matNoColdWater);
                MaterialTemperature mt = new MaterialTemperature()
                {
                    Material = mat,
                    AverageTemperature = mat.MaterialConfig_Material
                                            .FirstOrDefault(x => x.KeyACUrl == PABakeryTempService.MaterialTempertureConfigKeyACUrl
                                                              && x.VBiACClassID == recvPointID)?.Value as double?
                };
                componentTemp.Add(mt);
                coldWater = mt;
            }

            if (coldWater != null)
                coldWater.Water = WaterType.ColdWater;

            var warmWater = componentTemp.FirstOrDefault(c => c.Material.MaterialNo == matNoWarmWater);
            if (cityWater != null && warmWater == null)
            {
                Material mat = dbApp.Material.FirstOrDefault(c => c.MaterialNo == matNoWarmWater);
                MaterialTemperature mt = new MaterialTemperature()
                {
                    Material = mat,
                    AverageTemperature = mat.MaterialConfig_Material
                                            .FirstOrDefault(x => x.KeyACUrl == PABakeryTempService.MaterialTempertureConfigKeyACUrl
                                                              && x.VBiACClassID == recvPointID)?.Value as double?
                };
                componentTemp.Add(mt);
                warmWater = mt;
            }

            if (warmWater != null)
                warmWater.Water = WaterType.WarmWater;

            var dryIceMaterial = componentTemp.FirstOrDefault(c => c.Material.MaterialNo == matNoDryIce);
            if (dryIceMaterial != null)
                dryIceMaterial.Water = WaterType.DryIce;

            DetermineCompTempFromPartslistOrMaterial(componentTemp, partslistPosList, recvPoint.RoomTemperature.ValueT);

            SetRoomTemp(componentTemp, recvPoint.RoomTemperature.ValueT);

            if (!componentTemp.Any(c => c.Material.MaterialNo == matNoDryIce))
            {
                Material dryIce = dbApp.Material.FirstOrDefault(c => c.MaterialNo == matNoDryIce);
                if (dryIce != null)
                {
                    MaterialTemperature mt = new MaterialTemperature() { Material = dryIce, AverageTemperature = -5, Water = WaterType.DryIce }; //TODO get temp from material, if is not set error
                    componentTemp.Add(mt);
                }
                else
                {
                    // TODO: error 
                }
            }

            return componentTemp;
        }

        private List<MaterialTemperature> DetermineComponentsTemperature(Picking picking, BakeryReceivingPoint recvPoint, DatabaseApp dbApp,
                                                                         List<MaterialTemperature> tempFromService, string matNoColdWater,
                                                                         string matNoCityWater, string matNoWarmWater, string matNoDryIce)
        {
            Guid? recvPointID = recvPoint?.ComponentClass?.ACClassID;
            if (!recvPointID.HasValue)
            {
                //TODO: error
                return null;
            }

            List<MaterialTemperature> componentTemp = picking.PickingPos_Picking
                                                             .Select(c => new MaterialTemperature()
                                                             {
                                                                 Material = c.Material,
                                                                 AverageTemperature = c.Material.MaterialConfig_Material
                                                                                       .FirstOrDefault(x => x.KeyACUrl == PABakeryTempService.MaterialTempertureConfigKeyACUrl
                                                                                                         && x.VBiACClassID == recvPointID)?.Value as double?
                                                             }).ToList();

            var cityWater = componentTemp.FirstOrDefault(c => c.Material.MaterialNo == matNoCityWater);
            if (cityWater != null)
                cityWater.Water = WaterType.CityWater;

            var coldWater = componentTemp.FirstOrDefault(c => c.Material.MaterialNo == matNoColdWater);
            if (cityWater != null && coldWater == null)
            {
                Material mat = dbApp.Material.FirstOrDefault(c => c.MaterialNo == matNoColdWater);
                MaterialTemperature mt = new MaterialTemperature()
                {
                    Material = mat,
                    AverageTemperature = mat.MaterialConfig_Material
                                            .FirstOrDefault(x => x.KeyACUrl == PABakeryTempService.MaterialTempertureConfigKeyACUrl
                                                              && x.VBiACClassID == recvPointID)?.Value as double?
                };
                componentTemp.Add(mt);
                coldWater = mt;
            }

            if (coldWater != null)
                coldWater.Water = WaterType.ColdWater;

            var warmWater = componentTemp.FirstOrDefault(c => c.Material.MaterialNo == matNoWarmWater);
            if (cityWater != null && warmWater == null)
            {
                Material mat = dbApp.Material.FirstOrDefault(c => c.MaterialNo == matNoWarmWater);
                MaterialTemperature mt = new MaterialTemperature()
                {
                    Material = mat,
                    AverageTemperature = mat.MaterialConfig_Material
                                            .FirstOrDefault(x => x.KeyACUrl == PABakeryTempService.MaterialTempertureConfigKeyACUrl
                                                              && x.VBiACClassID == recvPointID)?.Value as double?
                };
                componentTemp.Add(mt);
                warmWater = mt;
            }

            if (warmWater != null)
                warmWater.Water = WaterType.WarmWater;

            var dryIceMaterial = componentTemp.FirstOrDefault(c => c.Material.MaterialNo == matNoDryIce);
            if (dryIceMaterial != null)
                dryIceMaterial.Water = WaterType.DryIce;

            DetermineCompTempFromMaterial(componentTemp, picking, recvPoint.RoomTemperature.ValueT);

            SetRoomTemp(componentTemp, recvPoint.RoomTemperature.ValueT);

            if (!componentTemp.Any(c => c.Material.MaterialNo == matNoDryIce))
            {
                Material dryIce = dbApp.Material.FirstOrDefault(c => c.MaterialNo == matNoDryIce);
                if (dryIce != null)
                {
                    MaterialTemperature mt = new MaterialTemperature() { Material = dryIce, AverageTemperature = -5, Water = WaterType.DryIce }; //TODO get temp from material, if is not set error
                    componentTemp.Add(mt);
                }
                else
                {
                    // TODO: error 
                }
            }

            return componentTemp;
        }

        private void DetermineCompTempFromPartslistOrMaterial(List<MaterialTemperature> componentTemp, IEnumerable<PartslistPos> partslistPosList, double roomTemp)
        {
            foreach (PartslistPos pos in partslistPosList)
            {
                MaterialTemperature mt = componentTemp.FirstOrDefault(c => c.Material.MaterialID == pos.Material.MaterialID);
                if (mt == null)
                {
                    mt = new MaterialTemperature() { Material = pos.Material };
                    componentTemp.Add(mt);
                }

                if (mt.AverageTemperature.HasValue)
                    continue;

                ACPropertyExt ext = pos.ACProperties.GetOrCreateACPropertyExtByName("Temperature", false);
                if (ext != null)
                {
                    double? value = (ext.Value as double?);
                    if (value.HasValue && value.Value != 0)
                    {
                        mt.AverageTemperature = value.Value;
                        continue;
                    }
                }

                ext = pos.Material.ACProperties.GetOrCreateACPropertyExtByName("Temperature", false);
                if (ext != null)
                {
                    double? value = (ext.Value as double?);
                    if (value.HasValue && value.Value != 0)
                    {
                        mt.AverageTemperature = value.Value;
                        continue;
                    }
                }
            }
        }

        private void DetermineCompTempFromMaterial(List<MaterialTemperature> componentTemp, Picking picking, double roomTemp)
        {
            foreach (PickingPos pos in picking.PickingPos_Picking)
            {
                ACPropertyExt ext = pos.Material.ACProperties.GetOrCreateACPropertyExtByName("Temperature", false);
                if (ext != null)
                {
                    double? value = (ext.Value as double?);
                    if (value.HasValue && value.Value != 0)
                    {
                        MaterialTemperature mt = componentTemp.FirstOrDefault(c => c.Material.MaterialID == pos.Material.MaterialID);
                        if (mt != null)
                            mt.AverageTemperature = value.Value;
                        continue;
                    }
                }
            }
        }

        private void SetRoomTemp(List<MaterialTemperature> componentTempList, double roomTemp)
        {
            foreach (var compTemp in componentTempList.Where(c => !c.AverageTemperature.HasValue))
            {
                compTemp.AverageTemperature = roomTemp;
            }
        }

        [ACMethodInfo("", "", 9999, true)]
        public void SaveWorkplaceTemperatureSettings(double waterTemperature, bool isOnlyForWaterTempCalculation)
        {
            using (DatabaseApp dbApp = new DatabaseApp())
            {
                IACConfigStore configStore = (MandatoryConfigStores?.FirstOrDefault(c => c is Partslist) as Partslist)?.FromAppContext<Partslist>(dbApp);
                if (configStore == null)
                {
                    configStore = (MandatoryConfigStores?.FirstOrDefault(c => c is Picking) as Picking)?.FromAppContext<Picking>(dbApp);
                }


                if (configStore != null)
                {
                    Guid? accessedProcessModuleID = configStore is Picking ? null : ParentPWGroup?.AccessedProcessModule?.ComponentClass?.ACClassID;

                    if (!accessedProcessModuleID.HasValue)
                    {
                        Root.Messages.LogMessage(eMsgLevel.Error, this.GetACUrl(), "SaveWorkplaceTemperatureSettings(10)", "AccessedProcessModuleID is null!");

                        return;
                    }

                    var configEntries = configStore.ConfigurationEntries.Where(c => c.PreConfigACUrl == PreValueACUrl && c.LocalConfigACUrl.StartsWith(ConfigACUrl)
                                                                                                                      && c.VBiACClassID == accessedProcessModuleID);

                    if (configEntries != null)
                    {
                        string propertyACUrl = string.Format("{0}\\{1}\\WaterTemp", ConfigACUrl, ACStateEnum.SMStarting);

                        // Water temp 
                        IACConfig waterTempConfig = configEntries.FirstOrDefault(c => c.LocalConfigACUrl == propertyACUrl);
                        if (waterTempConfig == null)
                        {
                            waterTempConfig = InsertTemperatureConfiguration(propertyACUrl, "WaterTemp", accessedProcessModuleID, configStore);
                        }

                        if (waterTempConfig == null)
                        {
                            //Error50420: The calculation water temperature configuration can not be added.
                            Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "SaveWorkplaceTemperatureSettings(10)", 1916, "Error50420");
                            if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                            {
                                OnNewAlarmOccurred(ProcessAlarm, msg);
                                Root.Messages.LogMessageMsg(msg);
                            }
                        }
                        else
                            waterTempConfig.Value = waterTemperature;

                        // Water temp 
                        propertyACUrl = string.Format("{0}\\{1}\\UseWaterTemp", ConfigACUrl, ACStateEnum.SMStarting);
                        IACConfig useOnlyForWaterTempCalculation = configEntries.FirstOrDefault(c => c.LocalConfigACUrl == propertyACUrl);
                        if (useOnlyForWaterTempCalculation == null)
                        {
                            useOnlyForWaterTempCalculation = InsertTemperatureConfiguration(propertyACUrl, "UseWaterTemp", accessedProcessModuleID, configStore);
                        }

                        if (useOnlyForWaterTempCalculation == null)
                        {
                            //Error50421: The calculation over water temperature configuration can not be added.
                            Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "SaveWorkplaceTemperatureSettings(20)", 1916, "Error50421");
                            if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                            {
                                OnNewAlarmOccurred(ProcessAlarm, msg);
                                Root.Messages.LogMessageMsg(msg);
                            }
                        }
                        else
                            useOnlyForWaterTempCalculation.Value = isOnlyForWaterTempCalculation;
                    }
                }

                Msg result = dbApp.ACSaveChanges();
                if (result != null)
                {
                    if (IsAlarmActive(ProcessAlarm, result.Message) == null)
                        Messages.LogError(this.GetACUrl(), result.ACIdentifier, result.InnerMessage);
                    OnNewAlarmOccurred(ProcessAlarm, result, false);
                }

                RootPW.ReloadConfig();

                ResetMembers();

            }
            //ConfigManagerIPlus.ACConfigFactory(CurrentConfigStore, )
            using (ACMonitor.Lock(_20015_LockValue))
            {
                _RecalculateTemperatures = true;
            }
            SubscribeToProjectWorkCycle();
        }

        private IACConfig InsertTemperatureConfiguration(string propertyACUrl, string paramACIdentifier, Guid? processModuleID, IACConfigStore configStore)
        {
            ACMethod acMethod = ACClassMethods.FirstOrDefault(c => c.ACIdentifier == ACStateConst.SMStarting)?.ACMethod;
            if (acMethod != null)
            {
                ACValue valItem = acMethod.ParameterValueList.GetACValue(paramACIdentifier);

                ACConfigParam param = new ACConfigParam()
                {
                    ACIdentifier = valItem.ACIdentifier,
                    ACCaption = acMethod.GetACCaptionForACIdentifier(valItem.ACIdentifier),
                    ValueTypeACClassID = valItem.ValueTypeACClass.ACClassID,
                    ACClassWF = ContentACClassWF
                };

                IACConfig configParam = ConfigManagerIPlus.ACConfigFactory(configStore, param, PreValueACUrl, propertyACUrl, processModuleID);
                param.ConfigurationList.Insert(0, configParam);

                configStore.ConfigurationEntries.Append(configParam);

                return configParam;
            }

            return null;
        }

        [ACMethodInfo("","",9999,true)]
        public ACValueList GetTemperaturesUsedInCalc()
        {
            return WaterTemperaturesUsedInCalc;
        }

        #endregion

        #region Methods => Other

        //TODO: dump
        protected override void DumpPropertyList(XmlDocument doc, XmlElement xmlACPropertyList)
        {
            base.DumpPropertyList(doc, xmlACPropertyList);

            XmlElement xmlChild = xmlACPropertyList["DoughTemp"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("DoughTemp");
                if (xmlChild != null)
                    xmlChild.InnerText = DoughTemp.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["WaterTemp"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("WaterTemp");
                if (xmlChild != null)
                    xmlChild.InnerText = WaterTemp.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }
        }

        private void ResetMembers()
        {
            using (ACMonitor.Lock(_20015_LockValue))
            {
                _RecalculateTemperatures = true;
                TemperatureCalculationResult.ValueT = null;
                ColdWaterQuantity.ValueT = 0;
                CityWaterQuantity.ValueT = 0;
                WarmWaterQuantity.ValueT = 0;
                DryIceQuantity.ValueT = 0;
                WaterTotalQuantity.ValueT = 0;
                _CalculatorMode = TempCalcMode.Calcuate;
            }
        }

        #endregion

        #region Execute-Helper-Handlers

        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            switch (acMethodName)
            {
                case "SaveWorkplaceTemperatureSettings":
                    SaveWorkplaceTemperatureSettings((double)acParameter[0], (bool)acParameter[1]);
                    return true;

                case "GetTemperaturesUsedInCalc":
                    result = GetTemperaturesUsedInCalc();
                    return true;
            }
            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        public static bool HandleExecuteACMethod_PWBakeryTempCalc(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PWNodeUserAck(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

        #endregion

        #endregion

        private enum TempCalcMode : short
        {
            Calcuate = 0,
            AdjustOrder = 10
        }
    }

    [ACSerializeableInfo]
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Melting heat option'}de{'Schmelzwärme-Option'}", Global.ACKinds.TACEnum, Global.ACStorableTypes.NotStorable, true, false)]
    [DataContract]
    public enum MeltingHeatOptionEnum : short
    {
        [EnumMember(Value = "Off")]
        Off = 0,
        [EnumMember(Value = "On")]
        On = 10,
        [EnumMember(Value = "OnlyForDoughTempCalc")]
        OnlyForDoughTempCalc = 20
    }
}
