﻿using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using gip.mes.datamodel;
using System.Runtime.Serialization;
using gip.mes.facility;
using Microsoft.EntityFrameworkCore;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Dough temperature calculation'}de{'Teigtemperaturberechnung'}", Global.ACKinds.TPWNodeStatic, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryTempCalc : PWNodeUserAck
    {
        public new const string PWClassName = nameof(PWBakeryTempCalc);

        public const string KneedingRiseTemp = "KneedingRiseTemp";

        public const string ParamPLWaterTQ = "PLWaterTQ";

        #region Constructors

        static PWBakeryTempCalc()
        {
            List<ACMethodWrapper> wrappers = ACMethod.OverrideFromBase(typeof(PWBakeryTempCalc), ACStateConst.SMStarting);
            if (wrappers != null)
            {
                foreach (ACMethodWrapper wrapper in wrappers)
                {
                    wrapper.Method.ParameterValueList.Add(new ACValue("DoughTemp", typeof(double?), false, Global.ParamOption.Required));
                    wrapper.ParameterTranslation.Add("DoughTemp", "en{'Doughtemperature °C'}de{'Teigtemperatur °C'}");

                    wrapper.Method.ParameterValueList.Add(new ACValue("WaterTemp", typeof(double?), false, Global.ParamOption.Required));
                    wrapper.ParameterTranslation.Add("WaterTemp", "en{'Watertemperature °C'}de{'Wassertemperatur °C'}");

                    wrapper.Method.ParameterValueList.Add(new ACValue("DryIceMatNo", typeof(string), "", Global.ParamOption.Required));
                    wrapper.ParameterTranslation.Add("DryIceMatNo", "en{'Dry ice MaterialNo'}de{'Eis MaterialNo'}");

                    wrapper.Method.ParameterValueList.Add(new ACValue("UseWaterTemp", typeof(bool), false, Global.ParamOption.Required));
                    wrapper.ParameterTranslation.Add("UseWaterTemp", "en{'Use Watertemperature'}de{'Wassertemperatur verwenden'}");

                    wrapper.Method.ParameterValueList.Add(new ACValue("UseWaterMixer", typeof(bool), false, Global.ParamOption.Required));
                    wrapper.ParameterTranslation.Add("UseWaterMixer", "en{'Calculate for water mixer'}de{'Berechnen für Wassermischer'}");

                    wrapper.Method.ParameterValueList.Add(new ACValue("IncludeMeltingHeat", typeof(MeltingHeatOptionEnum), MeltingHeatOptionEnum.Off, Global.ParamOption.Required));
                    wrapper.ParameterTranslation.Add("IncludeMeltingHeat", "en{'Include melting heat'}de{'Schmelzwärme einrechnen'}");

                    wrapper.Method.ParameterValueList.Add(new ACValue("WaterMeltingHeat", typeof(double), 332500, Global.ParamOption.Required));
                    wrapper.ParameterTranslation.Add("MeltingHeatWater", "en{'Water melting heat[J/kg]'}de{'SchmelzwaermeWasser[J/kg]'}");

                    wrapper.Method.ParameterValueList.Add(new ACValue("MeltingHeatInfluence", typeof(double), 100, Global.ParamOption.Required));
                    wrapper.ParameterTranslation.Add("MeltingHeatInfluence", "en{'Melting heat influence[%]'}de{'SchmelzwaermeEinfluss[%]'}");

                    wrapper.Method.ParameterValueList.Add(new ACValue("AskUserIsWaterNeeded", typeof(bool), false, Global.ParamOption.Optional));
                    wrapper.ParameterTranslation.Add("AskUserIsWaterNeeded", "en{'Ask user if is water needed'}de{'Ask user if is water needed'}");

                    wrapper.Method.ParameterValueList.Add(new ACValue("CompSequenceNo", typeof(int), (int)0, Global.ParamOption.Optional));
                    wrapper.ParameterTranslation.Add("CompSequenceNo", "en{'Sequence-No. for adding into BOM'}de{'Folgenummer beim Hinzufügen in die Rezeptur'}");

                    wrapper.Method.ParameterValueList.Add(new ACValue("WeighIceInPicking", typeof(bool), false, Global.ParamOption.Optional));
                    wrapper.ParameterTranslation.Add("WeighIceInPicking", "en{'Weigh ice in picking'}de{'Eis beim Kommissionieren wiegen'}");

                    wrapper.Method.ParameterValueList.Add(new ACValue("WaterQCorrectionStep", typeof(double), 0.5, Global.ParamOption.Optional));
                    wrapper.ParameterTranslation.Add("WaterQCorrectionStep", "en{'Water quantity correction step'}de{'Schritt zur Korrektur der Wassermenge'}");

                    wrapper.Method.ParameterValueList.Add(new ACValue("MaxWaterCorrectionDiff", typeof(double), 10, Global.ParamOption.Optional));
                    wrapper.ParameterTranslation.Add("MaxWaterCorrectionDiff", "en{'Max water correction difference'}de{'Maximale Wasserkorrekturdifferenz'}");
                }
            }

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

        public override bool IsSkippable => true;

        public override bool MustBeInsidePWGroup
        {
            get
            {
                return true;
            }
        }


        public const double C_WaterSpecHeatCapacity = 4187; // Alwaya 4187 Joule/kg
        public static double GetWaterSpecHeatCapacity(double value)
        {
            return value < 0.000001 ? C_WaterSpecHeatCapacity : value;
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

        private bool? _UserResponse = null;
        private double? _NewWaterQuantity = null;
        private double? _WaterTopParentPlPosRelQ = null;

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

        internal bool AskUserIsWaterNeeded
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("AskUserIsWaterNeeded");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }

        internal int CompSequenceNo
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("CompSequenceNo");
                    if (acValue != null)
                    {
                        return acValue.ParamAsInt32;
                    }
                }
                return 0;
            }
        }

        internal bool WeighIceInPicking
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("WeighIceInPicking");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return false;
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
            if (Root.Initialized)
            {
                RefreshNodeInfoOnModule();
                if (AskUserIsWaterNeeded)
                {
                    bool? userResponse = null;
                    using (ACMonitor.Lock(_20015_LockValue))
                    {
                        userResponse = _UserResponse;
                    }

                    if (userResponse == null)
                    {
                        UnSubscribeToProjectWorkCycle();
                        return;
                    }
                    else if (!userResponse.Value)
                    {
                        SetIntermediateComponentsToCompleted();
                        CurrentACState = ACStateEnum.SMCompleted;
                        return;
                    }
                }

                BakeryReceivingPoint recvPoint = ParentPWGroup?.AccessedProcessModule as BakeryReceivingPoint;
                bool? isTempServiceInitialized = recvPoint?.ExecuteMethod("IsTemperatureServiceInitialized") as bool?;
                if (isTempServiceInitialized.HasValue || isTempServiceInitialized.Value)
                {
                    TempCalcMode calcMode = TempCalcMode.Calcuate;
                    using (ACMonitor.Lock(_20015_LockValue))
                    {
                        calcMode = _CalculatorMode;
                    }

                    bool? isFirstItem = IsFirstItemForDosingInPicking();
                    if (isFirstItem.HasValue && !isFirstItem.Value)
                    {
                        CurrentACState = ACStateEnum.SMCompleted;
                    }

                    if (calcMode == TempCalcMode.Calcuate)
                    {
                        CalculateTargetTemperature();
                        UnSubscribeToProjectWorkCycle();
                    }
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

        protected override void OnAckStart(bool skipped)
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

        [ACMethodInfo("", "en{'Calculate water target temperature'}de{'Calculate water target temperature'}", 700)]
        public double? CalculateWaterTargetTemperature()
        {
            WarmWaterQuantity.ValueT = 0;
            CityWaterQuantity.ValueT = 0;
            ColdWaterQuantity.ValueT = 0;
            DryIceQuantity.ValueT = 0;
            WaterTotalQuantity.ValueT = 0;

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
                return null;
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
                return null;
            }

            PWMethodProduction pwMethodProduction = ParentPWMethod<PWMethodProduction>();
            // If dosing is not for production, then do nothing
            if (pwMethodProduction != null)
            {
                return CalculateProdOrderTargetTemperature(recvPoint, pwMethodProduction, true);
            }
            else
            {
                PWMethodRelocation pwMethodRelocation = ParentPWMethod<PWMethodRelocation>();
                if (pwMethodRelocation != null)
                {
                    return CalculateRelocationTargetTemperature(recvPoint, pwMethodRelocation, true);
                }
            }

            return null;
        }

        private void CalculateTargetTemperature()
        {
            bool recalc = false;

            using (ACMonitor.Lock(_20015_LockValue))
            {
                recalc = _RecalculateTemperatures;
            }

            if (!recalc)
                return;

            WarmWaterQuantity.ValueT = 0;
            CityWaterQuantity.ValueT = 0;
            ColdWaterQuantity.ValueT = 0;
            DryIceQuantity.ValueT = 0;
            WaterTotalQuantity.ValueT = 0;

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
                    CalculateRelocationTargetTemperature(recvPoint, pwMethodRelocation);
                }
            }

            using (ACMonitor.Lock(_20015_LockValue))
            {
                _RecalculateTemperatures = false;
            }
            UnSubscribeToProjectWorkCycle();
        }

        private double? CalculateProdOrderTargetTemperature(BakeryReceivingPoint recvPoint, PWMethodProduction pwMethodProduction, bool onlyCalculation = false)
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
                    return null;
                }

                double kneedingRiseTemperature = 0;
                double recvPointCorrTemp = 0;
                double doughTargetTempBeforeKneeding = 0;

                ProdOrderPartslistPos endBatchPos = pwMethodProduction.CurrentProdOrderPartslistPos.FromAppContext<ProdOrderPartslistPos>(dbApp);

                bool kneedingTempResult = GetKneedingRiseTemperature(dbApp, endBatchPos.TargetQuantityUOM, out kneedingRiseTemperature);
                if (!kneedingTempResult)
                    return null;

                if (!UseWaterTemp)
                {
                    recvPointCorrTemp = recvPoint.DoughCorrTemp.ValueT;
                    doughTargetTempBeforeKneeding = DoughTemp.Value - kneedingRiseTemperature + recvPointCorrTemp;
                }

                ACValueList componentTemperaturesService = recvPoint.GetWaterComponentsFromTempService();
                if (componentTemperaturesService == null)
                    return null;

                List<MaterialTemperature> tempFromService = componentTemperaturesService.Select(c => c.Value as MaterialTemperature).ToList();

                _ColdWaterMaterialNo = tempFromService.FirstOrDefault(c => c.Water == WaterType.ColdWater)?.MaterialNo;
                _CityWaterMaterialNo = tempFromService.FirstOrDefault(c => c.Water == WaterType.CityWater)?.MaterialNo;
                _WarmWaterMaterialNo = tempFromService.FirstOrDefault(c => c.Water == WaterType.WarmWater)?.MaterialNo;
                 string dryIce = DryIceMaterialNo;

                if (string.IsNullOrEmpty(_ColdWaterMaterialNo))
                {
                    //Error50410: The Temperature service didn't return temperature informations for water type {0}.
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, nameof(CalculateProdOrderTargetTemperature) + "(510)", 510, "Error50410", WaterType.ColdWater.ToString());
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ProcessAlarm, msg);
                        Root.Messages.LogMessageMsg(msg);
                    }
                    return null;
                }

                if (string.IsNullOrEmpty(_CityWaterMaterialNo))
                {
                    //Error50410: The Temperature service didn't return temperature informations for water type {0}.
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, nameof(CalculateProdOrderTargetTemperature) + "(520)", 520, "Error50410", WaterType.CityWater.ToString());
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ProcessAlarm, msg);
                        Root.Messages.LogMessageMsg(msg);
                    }
                    return null;
                }

                if (string.IsNullOrEmpty(_WarmWaterMaterialNo))
                {
                    //Error50410: The Temperature service didn't return temperature informations for water type {0}.
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, nameof(CalculateProdOrderTargetTemperature) + "(530)", 530, "Error50410", WaterType.WarmWater.ToString());
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ProcessAlarm, msg);
                        Root.Messages.LogMessageMsg(msg);
                    }
                    return null;
                }


                if (pwMethodProduction.CurrentProdOrderBatch == null)
                {
                    // Error50411: No batch assigned to last intermediate material of this workflow
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CalculateProdOrderTargetTemperature(30)", 554, "Error50411");

                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                        Messages.LogError(this.GetACUrl(), msg.ACIdentifier, msg.InnerMessage);
                    OnNewAlarmOccurred(ProcessAlarm, msg, false);
                    return null;
                }

                var contentACClassWFVB = ContentACClassWF.FromAppContext<gip.mes.datamodel.ACClassWF>(dbApp);
                ProdOrderBatch batch = pwMethodProduction.CurrentProdOrderBatch.FromAppContext<ProdOrderBatch>(dbApp);
                ProdOrderBatchPlan batchPlan = batch.ProdOrderBatchPlan;

                PartslistACClassMethod plMethod = endBatchPos.ProdOrderPartslist.Partslist.PartslistACClassMethod_Partslist.FirstOrDefault();
                ProdOrderPartslist currentProdOrderPartslist = endBatchPos.ProdOrderPartslist.FromAppContext<ProdOrderPartslist>(dbApp);

                if (currentProdOrderPartslist == null)
                {
                    Messages.LogMessageMsg(new Msg(eMsgLevel.Error, "currentProdOrderPartslist is null."));
                    return null;
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
                if (cityWaterComp != null)
                {
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
                        return null;
                    }

                    bool isInRelatedIntermediate = false;

                    foreach (MaterialWFConnection matWFConn in matWFConnections)
                    {
                        if (cityWaterComp.TargetProdOrderPartslistPos != null && 
                            matWFConn.MaterialID == cityWaterComp.TargetProdOrderPartslistPos.MaterialID)
                        {
                            isInRelatedIntermediate = true;
                            break;
                        }
                    }

                    if (!isInRelatedIntermediate)
                        cityWaterComp = null;
                }


                if (cityWaterComp == null)
                {
                    // If partslist not contains city water, skip node
                    if (!onlyCalculation)
                        CurrentACState = ACStateEnum.SMCompleted;
                    return null;
                }

                ProdOrderPartslistPosRelation coldWaterComp = relations.FirstOrDefault(c => c.SourceProdOrderPartslistPos.Material.MaterialNo == _ColdWaterMaterialNo);
                ProdOrderPartslistPosRelation warmWaterComp = relations.FirstOrDefault(c => c.SourceProdOrderPartslistPos.Material.MaterialNo == _WarmWaterMaterialNo);
                ProdOrderPartslistPosRelation iceComp = relations.FirstOrDefault(c => c.SourceProdOrderPartslistPos.Material.MaterialNo == DryIceMaterialNo);

                double? waterTargetQuantity = cityWaterComp.TargetQuantity;
                bool saveChanges = false;

                if (coldWaterComp != null && coldWaterComp.TargetQuantity > 0)
                {
                    waterTargetQuantity += coldWaterComp.TargetQuantity;
                    coldWaterComp.TargetQuantity = 0;
                    saveChanges = true;
                }

                if (warmWaterComp != null && warmWaterComp.TargetQuantity > 0)
                {
                    waterTargetQuantity += warmWaterComp.TargetQuantity;
                    warmWaterComp.TargetQuantity = 0;
                    saveChanges = true;
                }

                if (iceComp != null && iceComp.TargetQuantity > 0 && IterationCount.ValueT > 0)
                {
                    waterTargetQuantity += iceComp.TargetQuantity;
                    iceComp.TargetQuantity = 0;
                    saveChanges = true;
                }

                if (saveChanges)
                {
                    var saveMsg = dbApp.ACSaveChanges();
                    if (saveMsg != null)
                        Messages.LogMessageMsg(saveMsg);
                }

                // Modify prodorder with new water quantity
                if (!onlyCalculation && _NewWaterQuantity.HasValue)
                {
                    if (_NewWaterQuantity.Value > 0.00001)
                    {
                        var topRelation = cityWaterComp.TopParentPartslistPosRelation;
                        if (topRelation != null)
                        {
                            _WaterTopParentPlPosRelQ = topRelation.TargetQuantityUOM / waterTargetQuantity.Value * _NewWaterQuantity.Value;
                        }
                        waterTargetQuantity = _NewWaterQuantity.Value;
                    }
                    _NewWaterQuantity = null;
                }

                List<MaterialTemperature> compTemps = DetermineComponentsTemperature(currentProdOrderPartslist, intermediates, recvPoint, plMethod, dbApp, tempFromService, _ColdWaterMaterialNo,
                                                                                     _CityWaterMaterialNo, _WarmWaterMaterialNo, dryIce);

                double componentsQ = 0;

                if (!UseWaterTemp)
                    componentsQ = CalculateComponents_Q_(recvPoint, doughTargetTempBeforeKneeding, relations, _ColdWaterMaterialNo, _CityWaterMaterialNo, _WarmWaterMaterialNo, dryIce, compTemps);

                bool isOnlyWaterCompsInPartslist = relations.Count() == 1 && relations.FirstOrDefault(c => c.SourceProdOrderPartslistPos.Material.MaterialNo == _CityWaterMaterialNo) != null;


                double defaultWaterTemp = 0;
                double suggestedWaterTemp = CalculateWaterTemperatureSuggestion(UseWaterTemp, isOnlyWaterCompsInPartslist, cityWaterComp.SourceProdOrderPartslistPos.Material, DoughTemp.Value,
                                                                                doughTargetTempBeforeKneeding, componentsQ, waterTargetQuantity, out defaultWaterTemp);

                if (onlyCalculation)
                    return suggestedWaterTemp;

                CalculateWaterTypes(compTemps, suggestedWaterTemp, waterTargetQuantity.Value, defaultWaterTemp, componentsQ, isOnlyWaterCompsInPartslist, doughTargetTempBeforeKneeding,
                                    false);

                FillInfoForBSO(compTemps, kneedingRiseTemperature, cityWaterComp, endBatchPos);
            }
            return null;
        }

        private double? CalculateRelocationTargetTemperature(BakeryReceivingPoint recvPoint, PWMethodRelocation pwMethodRelocation, bool onlyCalculation = false)
        {
            using (Database db = new gip.core.datamodel.Database())
            using (DatabaseApp dbApp = new DatabaseApp())
            {
                ACValueList componentTemperaturesService = recvPoint.GetComponentTemperatures();
                if (componentTemperaturesService == null)
                    return null;

                List<MaterialTemperature> tempFromService = componentTemperaturesService.Select(c => c.Value as MaterialTemperature).ToList();

                _ColdWaterMaterialNo = tempFromService.FirstOrDefault(c => c.Water == WaterType.ColdWater)?.MaterialNo;
                _CityWaterMaterialNo = tempFromService.FirstOrDefault(c => c.Water == WaterType.CityWater)?.MaterialNo;
                _WarmWaterMaterialNo = tempFromService.FirstOrDefault(c => c.Water == WaterType.WarmWater)?.MaterialNo;
                string dryIce = DryIceMaterialNo;

                if (string.IsNullOrEmpty(_ColdWaterMaterialNo))
                {
                    //Error50410: The Temperature service didn't return temperature informations for water type {0}.
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, nameof(CalculateRelocationTargetTemperature) + "(610)", 610, "Error50410", WaterType.ColdWater.ToString());
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ProcessAlarm, msg);
                        Root.Messages.LogMessageMsg(msg);
                    }
                    return null;
                }

                if (string.IsNullOrEmpty(_CityWaterMaterialNo))
                {
                    //Error50410: The Temperature service didn't return temperature informations for water type {0}.
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, nameof(CalculateRelocationTargetTemperature) + "(620)", 620, "Error50410", WaterType.CityWater.ToString());
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ProcessAlarm, msg);
                        Root.Messages.LogMessageMsg(msg);
                    }
                    return null;
                }

                if (string.IsNullOrEmpty(_WarmWaterMaterialNo))
                {
                    //Error50410: The Temperature service didn't return temperature informations for water type {0}.
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, nameof(CalculateRelocationTargetTemperature) + "(630)", 630, "Error50410", WaterType.WarmWater.ToString());
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ProcessAlarm, msg);
                        Root.Messages.LogMessageMsg(msg);
                    }
                    return null;
                }

                Picking picking = pwMethodRelocation.CurrentPicking.FromAppContext<Picking>(dbApp);
                PickingPos pickingPos = pwMethodRelocation.CurrentPickingPos.FromAppContext<PickingPos>(dbApp);

                Material cityWaterComp = pickingPos.Material.MaterialNo == _CityWaterMaterialNo ? pickingPos.Material : null;
                if (cityWaterComp == null)
                {
                    //CurrentACState = ACStateEnum.SMCompleted;
                    return null;
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

                if (onlyCalculation)
                    return suggestedWaterTemp;

                CalculateWaterTypes(compTemps, suggestedWaterTemp, waterTargetQuantity.Value, defaultWaterTemp, 0, isOnlyWaterCompsInPartslist, 0, true);

                FillInfoForBSO(compTemps, null, null, null);
            }
            return null;
        }

        private bool GetKneedingRiseTemperature(DatabaseApp dbApp, double batchSize, out double kneedingTemperature)
        {
            kneedingTemperature = 0;

            var kneedingNodes = RootPW.FindChildComponents<PWBakeryKneading>();

            if (kneedingNodes != null && kneedingNodes.Any())
            {
                if (kneedingNodes.Count > 1)
                {
                    //Error50454: The workflow contains two or more PWBakeryKneading nodes. 
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "GetKneedingRiseTemperature(10)", 752, "Error50454");
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ProcessAlarm, msg);
                        Messages.LogMessageMsg(msg);
                    }
                    return false;
                }

                bool fullQuantity = true;
                PWNodeProcessWorkflowVB planningNode = RootPW.InvokingWorkflow as PWNodeProcessWorkflowVB;
                if (planningNode != null)
                {
                    ACMethod config = planningNode.MyConfiguration;
                    ACValue standardBatchSize = config?.ParameterValueList.GetACValue(ProdOrderBatchPlan.C_BatchSizeStandard);
                    if (standardBatchSize != null)
                    {
                        double halfQuantity = standardBatchSize.ParamAsDouble / 2;
                        if (batchSize <= halfQuantity)
                            fullQuantity = false;
                    }
                }

                PWBakeryKneading kneedingNode = kneedingNodes.FirstOrDefault();

                return kneedingNode.GetKneedingRiseTemperature(dbApp, fullQuantity, out kneedingTemperature);
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
                double componentTemperature = recvPoint.RoomTemp;
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

            double waterSpecHeatCapacity = GetWaterSpecHeatCapacity(waterComp.SpecHeatCapacity);

            if (calculateWaterTemp)
            {
                //if (isOnlyWater)
                //{
                //    suggestedWaterTemperature = doughTargetTempAfterKneeding;
                //    defaultWaterTemp = doughTargetTempAfterKneeding;
                //}
                //else
                {
                    double? waterTemp = WaterTemp.Value;

                    if (waterTemp.HasValue /*&& (waterTemp > 0.00001 || waterTemp < -0.0001)*/)
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
                else if (waterTargetQuantity.HasValue /*&& waterTargetQuantity > 0.00001*/ && waterSpecHeatCapacity > 0.00001)
                {
                    suggestedWaterTemperature = (componentsQ / (waterTargetQuantity.Value * waterSpecHeatCapacity)) + doughTargetTempBeforeKneeding;
                }
            }

            return suggestedWaterTemperature;
        }



        private void CalculateWaterTypes(IEnumerable<MaterialTemperature> componentTemperatures, double targetWaterTemperature, double totalWaterQuantity, double defaultWaterTemp,
                                         double componentsQ, bool isOnlyWaterCompInPartslist, double doughTempBeforeKneeding, bool isForPicking)
        {
            var watersByTemp = componentTemperatures.Where(c => c.Water > WaterType.NotWater && c.Water != WaterType.DryIce).OrderBy(c => c.AverageTemperature).ToArray();


            MaterialTemperature coldWater = watersByTemp.FirstOrDefault(); // componentTemperatures.FirstOrDefault(c => c.Water == WaterType.ColdWater);
            MaterialTemperature cityWater = watersByTemp.Skip(1).FirstOrDefault(); // componentTemperatures.FirstOrDefault(c => c.Water == WaterType.CityWater);
            MaterialTemperature warmWater = watersByTemp.LastOrDefault(); // componentTemperatures.FirstOrDefault(c => c.Water == WaterType.WarmWater);
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

            WaterTotalQuantity.ValueT = totalWaterQuantity;

            //check warm water 
            if (targetWaterTemperature > warmWater.AverageTemperature)
            {
                if (warmWater.AverageTemperature > 0.0001)
                {
                    SetWaterQuantity(warmWater, totalWaterQuantity, null, 0);
                }
                else
                {
                    var cityW = watersByTemp.FirstOrDefault(c => c.Water == WaterType.CityWater);
                    if (cityW != null)
                    {
                        SetWaterQuantity(cityW, totalWaterQuantity, null, 0);
                    }
                    else
                    {
                        //alarm
                        TemperatureCalculationResult.ValueT = "Error: City water can not be found!";
                    }
                }
                // The calculated water temperature of {0} °C can not be reached, the maximum water temperature is {1} °C and the target quantity is {2} {3}. 
                TemperatureCalculationResult.ValueT = Root.Environment.TranslateText(this, "TempCalcResultMax", targetWaterTemperature.ToString("F2"), warmWater.AverageTemperature,
                                                                                                                totalWaterQuantity.ToString("F2"), "kg");
                WaterCalcResult.ValueT = targetWaterTemperature;

                return;
            }


            if (warmWater.WaterDefaultTemperature != 9999 && cityWater.WaterDefaultTemperature != 9999 && CombineWarmCityWater(warmWater, cityWater, targetWaterTemperature, totalWaterQuantity))
            {
                //The calculated water temperature is {0} °C and the target quantity is {1} {2}.
                TemperatureCalculationResult.ValueT = Root.Environment.TranslateText(this, "TempCalcResult", targetWaterTemperature.ToString("F2"),
                                                                                                             totalWaterQuantity.ToString("F2"), "kg");
                WaterCalcResult.ValueT = targetWaterTemperature;
                return;
            }


            if (coldWater.WaterDefaultTemperature != 9999 && cityWater.WaterDefaultTemperature != 9999 && CombineColdCityWater(coldWater, cityWater, targetWaterTemperature, totalWaterQuantity))
            {
                //The calculated water temperature is {0} °C and the target quantity is {1} {2}.
                TemperatureCalculationResult.ValueT = Root.Environment.TranslateText(this, "TempCalcResult", targetWaterTemperature.ToString("F2"),
                                                                                                             totalWaterQuantity.ToString("F2"), "kg");
                WaterCalcResult.ValueT = targetWaterTemperature;
                return;
            }

            if (coldWater.WaterDefaultTemperature != 9999 && warmWater.WaterDefaultTemperature != 9999 && CombineWarmCityWater(warmWater, coldWater, targetWaterTemperature, totalWaterQuantity))
            {
                //The calculated water temperature is {0} °C and the target quantity is {1} {2}.
                TemperatureCalculationResult.ValueT = Root.Environment.TranslateText(this, "TempCalcResult", targetWaterTemperature.ToString("F2"),
                                                                                                             totalWaterQuantity.ToString("F2"), "kg");
                WaterCalcResult.ValueT = targetWaterTemperature;
                return;
            }

            MaterialTemperature waterWithIce = coldWater;
            if (coldWater.WaterDefaultTemperature == 9999 && cityWater.WaterDefaultTemperature != 9999)
                waterWithIce = cityWater;

            if (!CombineWatersWithDryIce(coldWater, dryIce, targetWaterTemperature, totalWaterQuantity, defaultWaterTemp, 
                                                                                   isOnlyWaterCompInPartslist, componentsQ, doughTempBeforeKneeding, isForPicking))
            {
                // The calculated water temperature of {0} °C can not be reached, the ice is {1} °C and the target quantity is {2} {3}. 
                TemperatureCalculationResult.ValueT = Root.Environment.TranslateText(this, "TempCalcResultMin", targetWaterTemperature.ToString("F2"), dryIce.AverageTemperature,
                                                                                                                totalWaterQuantity.ToString("F2"), "kg");
                WaterCalcResult.ValueT = targetWaterTemperature;
                return;
            }

            //SetWaterQuantity(realCityWater, totalWaterQuantity, null, 0);
            //The calculated water temperature is {0} °C and the target quantity is {1} {2}.
            TemperatureCalculationResult.ValueT = Root.Environment.TranslateText(this, "TempCalcResult", targetWaterTemperature.ToString("F2"),
                                                                                                         totalWaterQuantity.ToString("F2"), "kg");
            WaterCalcResult.ValueT = targetWaterTemperature;
        }

        private bool CombineWarmCityWater(MaterialTemperature warmWater, MaterialTemperature water, double targetWaterTemperature, double totalWaterQuantity)
        {
            if (targetWaterTemperature <= warmWater.AverageTemperature && targetWaterTemperature > water.AverageTemperature)
            {
                double warmWaterSHC = GetWaterSpecHeatCapacity(warmWater.Material.SpecHeatCapacity);
                double cityWaterSHC = GetWaterSpecHeatCapacity(water.Material.SpecHeatCapacity);

                double warmWaterQuantity = (totalWaterQuantity * (cityWaterSHC * (water.AverageTemperature.Value - targetWaterTemperature)))
                                           / ((cityWaterSHC * (water.AverageTemperature.Value - targetWaterTemperature))
                                           + (warmWaterSHC * (targetWaterTemperature - warmWater.AverageTemperature.Value)));

                double cityWaterQuantity = totalWaterQuantity - warmWaterQuantity;

                if (cityWaterQuantity < water.WaterMinDosingQuantity && warmWater.WaterDefaultTemperature != 9999)
                {
                    warmWaterQuantity = totalWaterQuantity;
                    cityWaterQuantity = 0;
                }
                else if (warmWaterQuantity < warmWater.WaterMinDosingQuantity || warmWater.WaterDefaultTemperature == 9999)
                {
                    warmWaterQuantity = 0;
                    cityWaterQuantity = totalWaterQuantity;
                }

                SetWaterQuantity(warmWater, warmWaterQuantity, water, cityWaterQuantity);

                return true;
            }

            return false;
        }

        private bool CombineColdCityWater(MaterialTemperature coldWater, MaterialTemperature cityWater, double targetWaterTemperature, double totalWaterQuantity)
        {
            if (targetWaterTemperature <= cityWater.AverageTemperature && targetWaterTemperature > coldWater.AverageTemperature)
            {
                double coldWaterSHC = GetWaterSpecHeatCapacity(coldWater.Material.SpecHeatCapacity);
                double cityWaterSHC = GetWaterSpecHeatCapacity(cityWater.Material.SpecHeatCapacity);

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

                SetWaterQuantity(coldWater, coldWaterQuantity, cityWater, cityWaterQuantity);

                return true;
            }

            return false;
        }

        private bool CombineWatersWithDryIce(MaterialTemperature water, MaterialTemperature dryIce, double targetWaterTemperature, double totalWaterQuantity,
                                             double defaultWaterTemp, bool isOnlyWaterCompInPartslist, double componentsQ, double doughTempBeforeKneeding, bool isForPicking)
        {
            if (IncludeMeltingHeat == MeltingHeatOptionEnum.Off
                || (IncludeMeltingHeat == MeltingHeatOptionEnum.OnlyForDoughTempCalc && !CalculateWaterTypesWithComponentsQ(defaultWaterTemp, isOnlyWaterCompInPartslist)))
            {
                if (targetWaterTemperature <= water.AverageTemperature && targetWaterTemperature > dryIce.AverageTemperature)
                {
                    double waterSHC = GetWaterSpecHeatCapacity(water.Material.SpecHeatCapacity);

                    double coldWaterQuantity = (totalWaterQuantity * (waterSHC * (dryIce.AverageTemperature.Value - targetWaterTemperature))) /
                                               ((waterSHC * (dryIce.AverageTemperature.Value - targetWaterTemperature)) + (waterSHC * (targetWaterTemperature - water.AverageTemperature.Value)));
                    double dryIceQuantity = totalWaterQuantity - coldWaterQuantity;

                    if (coldWaterQuantity < water.WaterMinDosingQuantity)
                    {
                        coldWaterQuantity = 0;
                        dryIceQuantity = totalWaterQuantity;
                    }
                    else if (dryIceQuantity < water.WaterMinDosingQuantity) // TODO: find manual scale on receiving point and get min weighing quantity
                    {
                        coldWaterQuantity = totalWaterQuantity;
                        dryIceQuantity = 0;
                    }

                    if (isForPicking && !WeighIceInPicking)
                    {
                        SetWaterQuantity(water, coldWaterQuantity + dryIceQuantity, dryIce, 0);
                    }
                    else
                    {
                        SetWaterQuantity(water, coldWaterQuantity, dryIce, dryIceQuantity);
                    }

                    return true;
                }
                else
                {
                    //There is no weigh for ice in picking, assign all quantity to colder water
                    if (isForPicking && !WeighIceInPicking)
                    {
                        SetWaterQuantity(water, totalWaterQuantity, dryIce, 0);
                    }
                    else
                    {
                        SetWaterQuantity(water, 0, dryIce, totalWaterQuantity);
                    }
                    return false;
                }
            }
            else
            {
                if (CalculateWaterTypesWithComponentsQ(defaultWaterTemp, isOnlyWaterCompInPartslist))
                {
                    double deltaTemp = defaultWaterTemp - targetWaterTemperature;
                    double deltaTempCold = water.AverageTemperature.Value + deltaTemp;

                    double waterSHC = GetWaterSpecHeatCapacity(water.Material.SpecHeatCapacity);

                    double iceQuantity = (componentsQ + totalWaterQuantity * waterSHC * (doughTempBeforeKneeding - deltaTempCold))
                                        / ((WaterMeltingHeat.Value * MeltingHeatInfluence.Value) - waterSHC * (doughTempBeforeKneeding - deltaTempCold) + waterSHC
                                        * (doughTempBeforeKneeding - dryIce.AverageTemperature.Value));

                    double coldWaterQuantity = 0;

                    if (iceQuantity < 0)
                        iceQuantity *= -1;

                    if (iceQuantity <= totalWaterQuantity)
                    {
                        coldWaterQuantity = totalWaterQuantity - iceQuantity;
                        if (coldWaterQuantity < water.WaterMinDosingQuantity)
                        {
                            iceQuantity = totalWaterQuantity;
                            coldWaterQuantity = 0;
                        }
                    }
                    else
                    {
                        coldWaterQuantity = (totalWaterQuantity * (waterSHC * (dryIce.AverageTemperature.Value - targetWaterTemperature)))
                                           / ((waterSHC * (dryIce.AverageTemperature.Value - targetWaterTemperature))
                                           + (waterSHC * (targetWaterTemperature - water.AverageTemperature.Value)));

                        iceQuantity = totalWaterQuantity - coldWaterQuantity;

                        if (iceQuantity < dryIce.WaterMinDosingQuantity) //TODO find min dosing quantity
                        {
                            coldWaterQuantity = totalWaterQuantity;
                            iceQuantity = 0;
                        }
                        else if (coldWaterQuantity < water.WaterMinDosingQuantity)
                        {
                            coldWaterQuantity = 0;
                            iceQuantity = totalWaterQuantity;
                        }
                    }

                    SetWaterQuantity(water, coldWaterQuantity, dryIce, iceQuantity);
                }
                else
                {
                    double waterSHC = GetWaterSpecHeatCapacity(water.Material.SpecHeatCapacity);
                    double iceSCH = GetWaterSpecHeatCapacity(dryIce.Material.SpecHeatCapacity);

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


                    if (targetWaterTemperature <= water.AverageTemperature && targetWaterTemperature > dryIce.AverageTemperature)
                    {
                        bool ice = true; //TODO: ask Damir

                        if (ice)
                        {
                            double iceQuantity = (waterSHC * totalWaterQuantity * (water.AverageTemperature.Value - targetWaterTemperature))
                                                 / ((iceSCH * (0 - dryIce.AverageTemperature.Value)) + (WaterMeltingHeat.Value * MeltingHeatInfluence.Value)
                                                 + (waterSHC * (targetWaterTemperature - 0)));

                            double coldWaterQuantity = 0;

                            if (iceQuantity <= totalWaterQuantity)
                            {
                                if (isForPicking)
                                {
                                    if (WeighIceInPicking)
                                    {
                                        coldWaterQuantity = totalWaterQuantity - iceQuantity;
                                        if (coldWaterQuantity < water.WaterMinDosingQuantity)
                                        {
                                            iceQuantity = totalWaterQuantity;
                                            coldWaterQuantity = 0;
                                        }
                                    }
                                    else
                                    {
                                        coldWaterQuantity = totalWaterQuantity;
                                        iceQuantity = 0;
                                    }
                                }
                                else
                                {
                                    coldWaterQuantity = totalWaterQuantity - iceQuantity;
                                    if (coldWaterQuantity < water.WaterMinDosingQuantity)
                                    {
                                        iceQuantity = totalWaterQuantity;
                                        coldWaterQuantity = 0;
                                    }
                                }
                            }

                            SetWaterQuantity(water, coldWaterQuantity, dryIce, iceQuantity);
                        }
                        else
                        {
                            double iceQuantity = (totalWaterQuantity * (water.AverageTemperature.Value - targetWaterTemperature)) /
                                                 (water.AverageTemperature.Value - dryIce.AverageTemperature.Value);
                            double coldWaterQuantity = totalWaterQuantity - iceQuantity;

                            if (iceQuantity < dryIce.WaterMinDosingQuantity)
                            {
                                coldWaterQuantity = totalWaterQuantity;
                                iceQuantity = 0;
                            }
                            else if (coldWaterQuantity < water.WaterMinDosingQuantity)
                            {
                                coldWaterQuantity = 0;
                                iceQuantity = totalWaterQuantity;
                            }

                            SetWaterQuantity(water, coldWaterQuantity, dryIce, iceQuantity);
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
            if (!UseWaterTemp /*&& defaultWaterTemp < 0.00001*/ && !isOnlyWaterCompInPartslist)
            {
                return true;
            }

            return false;
        }

        private void SetWaterQuantity(MaterialTemperature firstWater, double firstWaterQ, MaterialTemperature secondWater, double secondWaterQ)
        {
            if (firstWater != null)
            {
                if (firstWater.Water == WaterType.ColdWater)
                {
                    ColdWaterQuantity.ValueT = firstWaterQ;
                }
                else if (firstWater.Water == WaterType.CityWater)
                {
                    CityWaterQuantity.ValueT = firstWaterQ;
                }
                else if (firstWater.Water == WaterType.WarmWater)
                {
                    WarmWaterQuantity.ValueT = firstWaterQ;
                }
                else if (firstWater.Water == WaterType.DryIce)
                {
                    DryIceQuantity.ValueT = firstWaterQ;
                }
            }

            if (secondWater != null)
            {
                if (secondWater.Water == WaterType.ColdWater)
                {
                    ColdWaterQuantity.ValueT = secondWaterQ;
                }
                else if (secondWater.Water == WaterType.CityWater)
                {
                    CityWaterQuantity.ValueT = secondWaterQ;
                }
                else if (secondWater.Water == WaterType.WarmWater)
                {
                    WarmWaterQuantity.ValueT = secondWaterQ;
                }
                else if (secondWater.Water == WaterType.DryIce)
                {
                    DryIceQuantity.ValueT = secondWaterQ;
                }
            }

        }

        private void FillInfoForBSO(IEnumerable<MaterialTemperature> componentTemperatures, double? kneedingRiseTemp, ProdOrderPartslistPosRelation cityWaterComp,
                                    ProdOrderPartslistPos endBatchPos)
        {
            ACValueList temperaturesUsedInCalc = new ACValueList(componentTemperatures.Where(c => c.Water != WaterType.NotWater).Select(x => new ACValue(x.Water.ToString(), x.AverageTemperature)).ToArray());
            if (kneedingRiseTemp.HasValue)
            {
                ACValue acValue = new ACValue(KneedingRiseTemp, kneedingRiseTemp.Value);
                temperaturesUsedInCalc.Add(acValue);
            }

            if (cityWaterComp != null && endBatchPos != null)
            {
                PartslistPos waterPos = cityWaterComp.SourceProdOrderPartslistPos.BasedOnPartslistPos;

                double factor = waterPos.TargetQuantityUOM / waterPos.Partslist.TargetQuantityUOM;
                double? tQuantity = factor * endBatchPos.TargetQuantityUOM;

                if (tQuantity.HasValue)
                {
                    ACValue acValue = new ACValue(ParamPLWaterTQ, tQuantity.Value);
                    temperaturesUsedInCalc.Add(acValue);
                }
            }

            WaterTemperaturesUsedInCalc = temperaturesUsedInCalc;
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

            cityWaterQ = Math.Round(CityWaterQuantity.ValueT, 2);
            coldWaterQ = Math.Round(ColdWaterQuantity.ValueT, 2);
            warmWaterQ = Math.Round(WarmWaterQuantity.ValueT, 2);
            dryIceQ = Math.Round(DryIceQuantity.ValueT, 2);

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

                var intermediateAutomatic = matWFConnections.FirstOrDefault(c => c.Material
                                                                                  .MaterialWFConnection_Material
                                                                                  .FirstOrDefault(x => x.MaterialWFACClassMethodID == c.MaterialWFACClassMethodID
                                                                                                    && c.ACClassWF.ACClassMethodID == x.ACClassWF.ACClassMethodID
                                                                                                    && _PWDosingType
                                                                                                       .IsAssignableFrom(x.ACClassWF.PWACClass
                                                                                                                          .FromIPlusContext<gip.core.datamodel.ACClass>(db).ObjectType))
                                                                                                                          != null)?.Material;

                var intermediateManual = matWFConnections.FirstOrDefault(c => c.Material
                                                                               .MaterialWFConnection_Material
                                                                               .FirstOrDefault(x => x.MaterialWFACClassMethodID == c.MaterialWFACClassMethodID
                                                                                                 && c.ACClassWF.ACClassMethodID == x.ACClassWF.ACClassMethodID
                                                                                                 && _PWManualWeighingType
                                                                                                    .IsAssignableFrom(x.ACClassWF.PWACClass
                                                                                                                       .FromIPlusContext<gip.core.datamodel.ACClass>(db).ObjectType))
                                                                                                                       != null)?.Material;

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

                Material intermediate = intermediateAutomatic != null ? intermediateAutomatic : intermediateManual;


                 int relSequence = AdjustBatchPosInProdOrderPartslist(dbApp, currentProdOrderPartslist, intermediate, posCity, batch, cityWaterQ, totalWatersQuantity, _WaterTopParentPlPosRelQ);
                _WaterTopParentPlPosRelQ = null;

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

                        //Material intermediate = intermediateAutomatic != null ? intermediateAutomatic : intermediateManual;

                        AdjustBatchPosInProdOrderPartslist(dbApp, currentProdOrderPartslist, intermediate, pos, batch, coldWaterQ, totalWatersQuantity, null, relSequence);
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

                        //Material intermediate = intermediateAutomatic != null ? intermediateAutomatic : intermediateManual;

                        AdjustBatchPosInProdOrderPartslist(dbApp, currentProdOrderPartslist, intermediate, pos, batch, warmWaterQ, totalWatersQuantity, null, relSequence);
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
                        if (pos.TargetQuantityUOM > 0.00001)
                        {
                            var relation = batch?.ProdOrderPartslistPosRelation_ProdOrderBatch
                                                 .FirstOrDefault(c => c.SourceProdOrderPartslistPosID == pos.ProdOrderPartslistPosID
                                                                   && c.TargetProdOrderPartslistPos.MaterialID == intermediateManual.MaterialID);

                            if (relation != null)
                            {
                                dryIceQ = dryIceQ + relation.TargetQuantityUOM;
                            }
                        }

                        int? seqNo = null;
                        if (CompSequenceNo > 0)
                            seqNo = CompSequenceNo;

                        AdjustBatchPosInProdOrderPartslist(dbApp, currentProdOrderPartslist, intermediateManual, pos, batch, dryIceQ, totalWatersQuantity, null, seqNo);
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

            dbApp.ProdOrderPartslistPos.Add(pos);

            return pos;
        }

        private int AdjustBatchPosInProdOrderPartslist(DatabaseApp dbApp, ProdOrderPartslist poPartslist, Material intermediateMaterial, ProdOrderPartslistPos sourcePos, ProdOrderBatch batch,
                                                         double waterQuantity, double totalWatersQuantity, double? topRelationNewQuantity = null, int? relSequenceNo = null)
        {
            int result = 0;

            ProdOrderPartslistPos targetPos = poPartslist.ProdOrderPartslistPos_ProdOrderPartslist
                                                             .FirstOrDefault(c => c.MaterialID == intermediateMaterial.MaterialID
                                                                               && c.MaterialPosType == GlobalApp.MaterialPosTypes.InwardIntern);

            if (targetPos == null)
            {
                targetPos = ProdOrderPartslistPos.NewACObject(dbApp, null);
                targetPos.Sequence = 1;
                targetPos.MaterialPosTypeIndex = (short)GlobalApp.MaterialPosTypes.InwardIntern;
                dbApp.ProdOrderPartslistPos.Add(targetPos);
            }

            ProdOrderPartslistPosRelation topRelation = sourcePos.ProdOrderPartslistPosRelation_SourceProdOrderPartslistPos
                                                           .FirstOrDefault(c => c.TargetProdOrderPartslistPos.MaterialID == targetPos.MaterialID
                                                                             && c.TargetProdOrderPartslistPos.MaterialPosType == GlobalApp.MaterialPosTypes.InwardIntern);

            if (topRelation == null)
            {
                topRelation = ProdOrderPartslistPosRelation.NewACObject(dbApp, null);
                topRelation.SourceProdOrderPartslistPos = sourcePos;
                topRelation.TargetProdOrderPartslistPos = targetPos;
                topRelation.Sequence = relSequenceNo == null ? targetPos.ProdOrderPartslistPosRelation_TargetProdOrderPartslistPos
                                                .Where(c => c.TargetProdOrderPartslistPos.MaterialID == targetPos.MaterialID
                                                         && c.TargetProdOrderPartslistPos.MaterialPosType == GlobalApp.MaterialPosTypes.InwardIntern)
                                                .Max(x => x.Sequence) + 1 : relSequenceNo.Value;

                dbApp.ProdOrderPartslistPosRelation.Add(topRelation);
            }

            result = topRelation.Sequence;

            if (topRelationNewQuantity.HasValue && topRelationNewQuantity > 0.00001)
            {
                topRelation.TargetQuantityUOM = topRelationNewQuantity.Value;
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

            return result;
        }

        private void AdjustWatersInPicking()
        {
            double cityWaterQ = 0, coldWaterQ = 0, warmWaterQ = 0, dryIceQ = 0;

            cityWaterQ = Math.Round(CityWaterQuantity.ValueT, 2);
            coldWaterQ = Math.Round(ColdWaterQuantity.ValueT, 2);
            warmWaterQ = Math.Round(WarmWaterQuantity.ValueT, 2);
            dryIceQ = Math.Round(DryIceQuantity.ValueT, 2);

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
                using (Database db = new gip.core.datamodel.Database())
                using (DatabaseApp dbApp = new DatabaseApp(db))
                {
                    Picking picking = pwMethodRelocation.CurrentPicking.FromAppContext<Picking>(dbApp);
                    if (picking == null)
                        return;


                    PickingPos cityWaterPos = picking.PickingPos_Picking.FirstOrDefault(c => c.Material.MaterialNo == _CityWaterMaterialNo);
                    if (cityWaterPos == null)
                        return;

                    Facility targetFacility = cityWaterPos.ToFacility;

                    cityWaterPos.PickingQuantityUOM = cityWaterQ;
                    if (cityWaterQ < 0.00001)
                    {
                        cityWaterPos.MDDelivPosLoadState = DatabaseApp.s_cQry_GetMDDelivPosLoadState(dbApp, MDDelivPosLoadState.DelivPosLoadStates.LoadToTruck)
                                                                      .FirstOrDefault();
                    }

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

                    if (dryIceQ > 0.00001)
                    {
                        PickingPos dryIcePos = picking.PickingPos_Picking.FirstOrDefault(c => c.Material.MaterialNo == DryIceMaterialNo);
                        if (dryIcePos == null)
                        {
                            dryIcePos = AddPickingPos(dbApp, picking, DryIceMaterialNo, targetFacility);
                            if (dryIcePos == null)
                                return;
                        }

                        if (CompSequenceNo > 0)
                            dryIcePos.Sequence = CompSequenceNo;

                        dryIcePos.PickingQuantityUOM = dryIceQ;
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
            List<Facility> possibleSources = material.Facility_Material.ToList();
            //if (!possibleSources.Any())
            //{
            //    //PWMethodRelocation pwMethodRelocation = ParentPWMethod<PWMethodRelocation>();
            //    //if (pwMethodRelocation != null)
            //    //{
            //    //    ACPickingManager pManager = pwMethodRelocation.PickingManager;
            //    //    if (pManager != null)
            //    //    {
            //    //        gip.core.datamodel.ACClass module = ParentPWGroup.AccessedProcessModule.ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(dbApp.ContextIPlus);

            //    //        IList<Facility> sources = null;
            //    //        pManager.GetRoutes(material, dbApp, dbApp.ContextIPlus, module, ACPartslistManager.SearchMode.AllSilos, null, out sources, null, null, null, false);

            //    //        if (sources != null && sources.Count > 0)
            //    //        {
            //    //            source = sources.FirstOrDefault();
            //    //        }
            //    //    }
            //    //}
            //}
            if (possibleSources.Count > 1)
            {
                IEnumerable<string> sources = possibleSources.Where(c => c.VBiFacilityACClassID != null).Select(x => x.VBiFacilityACClass.ACURLComponentCached);
                gip.core.datamodel.ACClass module = ParentPWGroup.AccessedProcessModule.ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(dbApp.ContextIPlus);

                ACRoutingParameters routingParameters = new ACRoutingParameters()
                {
                    RoutingService = this.RoutingService,
                    Database = dbApp.ContextIPlus,
                    AttachRouteItemsToContext = false,
                    Direction = RouteDirections.Backwards,
                    SelectionRuleID = PAMTank.SelRuleID_Silo,
                    MaxRouteAlternativesInLoop = 10,
                    IncludeReserved = true,
                    IncludeAllocated = true
                };
                
                RoutingResult rResult = ACRoutingService.SelectRoutes(module, sources, routingParameters);

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
            pos.MDDelivPosLoadState = dbApp.MDDelivPosLoadState.FirstOrDefault(c => c.MDDelivPosLoadStateIndex == (short)MDDelivPosLoadState.DelivPosLoadStates.ReadyToLoad);

            picking.PickingPos_Picking.Add(pos);
            dbApp.PickingPos.Add(pos);

            return pos;
        }

        private void SetIntermediateComponentsToCompleted()
        {
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

                BakeryReceivingPoint recvPoint = ParentPWGroup.AccessedProcessModule as BakeryReceivingPoint;

                ACValueList componentTemperaturesService = recvPoint.GetWaterComponentsFromTempService();
                if (componentTemperaturesService == null)
                    return;

                List<MaterialTemperature> tempFromService = componentTemperaturesService.Select(c => c.Value as MaterialTemperature).ToList();

                _ColdWaterMaterialNo = tempFromService.FirstOrDefault(c => c.Water == WaterType.ColdWater)?.MaterialNo;
                _CityWaterMaterialNo = tempFromService.FirstOrDefault(c => c.Water == WaterType.CityWater)?.MaterialNo;
                _WarmWaterMaterialNo = tempFromService.FirstOrDefault(c => c.Water == WaterType.WarmWater)?.MaterialNo;
                string dryIce = DryIceMaterialNo;

                var intermediateAutomatic = matWFConnections.FirstOrDefault(c => c.Material
                                                                                  .MaterialWFConnection_Material
                                                                                  .FirstOrDefault(x => x.MaterialWFACClassMethodID == c.MaterialWFACClassMethodID  
                                                                                                    && _PWDosingType
                                                                                                       .IsAssignableFrom(x.ACClassWF.PWACClass
                                                                                                                          .FromIPlusContext<gip.core.datamodel.ACClass>(db).ObjectType)) 
                                                                                                                          != null)?.Material;

                var intermediateManual = matWFConnections.FirstOrDefault(c => c.Material
                                                                               .MaterialWFConnection_Material
                                                                               .FirstOrDefault(x => x.MaterialWFACClassMethodID == c.MaterialWFACClassMethodID 
                                                                                                 && _PWManualWeighingType
                                                                                                    .IsAssignableFrom(x.ACClassWF.PWACClass
                                                                                                                       .FromIPlusContext<gip.core.datamodel.ACClass>(db).ObjectType)) 
                                                                                                                       != null).Material;


                endBatchPos.ProdOrderPartslist.FromAppContext<ProdOrderPartslist>(dbApp);
                var intermediatePositions = currentProdOrderPartslist.ProdOrderPartslistPos_ProdOrderPartslist
                    .Where(c => c.MaterialID.HasValue && ((intermediateAutomatic != null && c.MaterialID == intermediateAutomatic.MaterialID)
                                                      || (intermediateManual != null && c.MaterialID == intermediateManual.MaterialID))
                        && c.MaterialPosType == GlobalApp.MaterialPosTypes.InwardIntern
                        && !c.ParentProdOrderPartslistPosID.HasValue);

                var posState = DatabaseApp.s_cQry_GetMDProdOrderPosState(dbApp, MDProdOrderPartslistPosState.ProdOrderPartslistPosStates.Completed).FirstOrDefault();

                foreach (var intermediatePos in intermediatePositions)
                {
                    var intermediateChildPos = intermediatePos.ProdOrderPartslistPos_ParentProdOrderPartslistPos
                            .Where(c => c.ProdOrderBatchID.HasValue
                                        && c.ProdOrderBatchID.Value == pwMethodProduction.CurrentProdOrderBatch.ProdOrderBatchID)
                            .FirstOrDefault();

                    ProdOrderPartslistPosRelation[] query = dbApp.ProdOrderPartslistPosRelation.Include(c => c.SourceProdOrderPartslistPos)
                                                                 .Include(c => c.SourceProdOrderPartslistPos.Material)
                                                                 .Include(c => c.SourceProdOrderPartslistPos.Material.BaseMDUnit)
                                                                 .Where(c => c.TargetProdOrderPartslistPosID == intermediateChildPos.ProdOrderPartslistPosID)
                                                                 .ToArray();

                    foreach (var rel in query)
                    {
                        Material sMaterial = rel.SourceProdOrderPartslistPos.Material;

                        if (sMaterial.UsageACProgram && (  sMaterial.MaterialNo == _CityWaterMaterialNo 
                                                        || sMaterial.MaterialNo == _ColdWaterMaterialNo
                                                        || sMaterial.MaterialNo == _WarmWaterMaterialNo))
                        {
                            rel.MDProdOrderPartslistPosState = posState;
                        }
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
                //Error50456: Can not find the receiving point ACClassID.
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "DetermineComponentsTemperature(10)", 1830, "Error50456");
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    Messages.LogMessageMsg(msg);
                OnNewAlarmOccurred(ProcessAlarm, msg, true);
                return null;
            }

            IEnumerable<PartslistPos> partslistPosList = prodOrderPartslist.Partslist?.PartslistPos_Partslist.Where(c => c.IsOutwardRoot).ToArray();

            List<MaterialTemperature> componentTemp = partslistPosList.Select(c => new MaterialTemperature()
            {
                Material = c.Material,
                AverageTemperature = c.Material.ConfigurationEntries
                                               .FirstOrDefault(x => x.KeyACUrl == PABakeryTempService.MaterialTempertureConfigKeyACUrl
                                                                 && x.VBiACClassID == recvPointID)?.Value as double?
            }).ToList();

            var cityWater = componentTemp.FirstOrDefault(c => c.Material.MaterialNo == matNoCityWater);
            if (cityWater != null)
            {
                cityWater.Water = WaterType.CityWater;
                var cityWaterFromService = tempFromService.FirstOrDefault(c => c.Water == WaterType.CityWater);
                CopyValuesFromServiceTemp(cityWater, cityWaterFromService);
            }

            var coldWater = componentTemp.FirstOrDefault(c => c.Material.MaterialNo == matNoColdWater);
            if (cityWater != null && coldWater == null)
            {
                Material mat = dbApp.Material.FirstOrDefault(c => c.MaterialNo == matNoColdWater);
                MaterialTemperature mt = new MaterialTemperature()
                {
                    Material = mat,
                    AverageTemperature = mat.ConfigurationEntries
                                            .FirstOrDefault(x => x.KeyACUrl == PABakeryTempService.MaterialTempertureConfigKeyACUrl
                                                              && x.VBiACClassID == recvPointID)?.Value as double?
                };

                var coldWaterFromService = tempFromService.FirstOrDefault(c => c.Water == WaterType.ColdWater);
                CopyValuesFromServiceTemp(mt, coldWaterFromService);

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
                    AverageTemperature = mat.ConfigurationEntries
                                            .FirstOrDefault(x => x.KeyACUrl == PABakeryTempService.MaterialTempertureConfigKeyACUrl
                                                              && x.VBiACClassID == recvPointID)?.Value as double?
                };

                var warmWaterFromService = tempFromService.FirstOrDefault(c => c.Water == WaterType.WarmWater);
                CopyValuesFromServiceTemp(mt, warmWaterFromService);

                componentTemp.Add(mt);
                warmWater = mt;
            }

            if (warmWater != null)
                warmWater.Water = WaterType.WarmWater;

            MaterialTemperature dryIceTemp = componentTemp.FirstOrDefault(c => c.Material.MaterialNo == matNoDryIce);
            if (dryIceTemp != null)
            {
                dryIceTemp.Water = WaterType.DryIce;
            }

            if (dryIceTemp == null)
            {
                Material dryIce = dbApp.Material.FirstOrDefault(c => c.MaterialNo == matNoDryIce);
                if (dryIce != null)
                {
                    double? iceTemp = dryIce.ConfigurationEntries.FirstOrDefault(x => x.KeyACUrl == PABakeryTempService.MaterialTempertureConfigKeyACUrl
                                                                                   && x.VBiACClassID == recvPointID)?.Value as double?;

                    if (iceTemp == null)
                    {
                        ACPropertyExt temperature = dryIce.ACProperties.GetOrCreateACPropertyExtByName("Temperature", false);
                        if (temperature != null)
                        {
                            iceTemp = temperature.Value as double?;
                        }
                    }

                    dryIceTemp = new MaterialTemperature() { Material = dryIce, AverageTemperature = iceTemp, Water = WaterType.DryIce }; 
                    componentTemp.Add(dryIceTemp);
                }
            }

            DetermineCompTempFromPartslistOrMaterial(componentTemp, partslistPosList, recvPoint.RoomTemp);

            if (dryIceTemp != null && !dryIceTemp.AverageTemperature.HasValue)
            {
                dryIceTemp.AverageTemperature = -1;

                //Info50078: The temperature of ICE for calcuation is now -1 °C. Please configure default ICE temperature in the Material master or in the Bill of material.
                Msg msg = new Msg(this, eMsgLevel.Info, PWClassName, "DetermineComponentsTemperature(20)", 1913, "Info50078");
                OnNewAlarmOccurred(ProcessAlarm, msg);
            }

            SetRoomTemp(componentTemp, recvPoint.RoomTemp, matNoDryIce);

            return componentTemp;
        }

        private void CopyValuesFromServiceTemp(MaterialTemperature matTemp, MaterialTemperature matTempFromService)
        {
            if (matTemp != null && matTempFromService != null)
            {
                matTemp.WaterDefaultTemperature = matTempFromService.WaterDefaultTemperature;
                matTemp.WaterMinDosingQuantity = matTempFromService.WaterMinDosingQuantity;
            }
        }

        private List<MaterialTemperature> DetermineComponentsTemperature(Picking picking, BakeryReceivingPoint recvPoint, DatabaseApp dbApp,
                                                                         List<MaterialTemperature> tempFromService, string matNoColdWater,
                                                                         string matNoCityWater, string matNoWarmWater, string matNoDryIce)
        {
            Guid? recvPointID = recvPoint?.ComponentClass?.ACClassID;
            if (!recvPointID.HasValue)
            {
                //Error50456: Can not find the receiving point ACClassID.
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "DetermineComponentsTemperature(11)", 1830, "Error50456");
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    Messages.LogMessageMsg(msg);
                OnNewAlarmOccurred(ProcessAlarm, msg, true);
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
            {
                cityWater.Water = WaterType.CityWater;
                var cityWaterFromService = tempFromService.FirstOrDefault(c => c.Water == WaterType.CityWater);
                CopyValuesFromServiceTemp(cityWater, cityWaterFromService);
            }

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

                var coldWaterFromService = tempFromService.FirstOrDefault(c => c.Water == WaterType.ColdWater);
                CopyValuesFromServiceTemp(mt, coldWaterFromService);

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

                var warmWaterFromService = tempFromService.FirstOrDefault(c => c.Water == WaterType.WarmWater);
                CopyValuesFromServiceTemp(mt, warmWaterFromService);

                componentTemp.Add(mt);
                warmWater = mt;
            }

            if (warmWater != null)
                warmWater.Water = WaterType.WarmWater;

            MaterialTemperature dryIceTemp = componentTemp.FirstOrDefault(c => c.Material.MaterialNo == matNoDryIce);
            if (dryIceTemp != null)
            {
                dryIceTemp.Water = WaterType.DryIce;
            }

            if (dryIceTemp == null)
            {
                Material dryIce = dbApp.Material.FirstOrDefault(c => c.MaterialNo == matNoDryIce);
                if (dryIce != null)
                {
                    double? iceTemp = dryIce.ConfigurationEntries.FirstOrDefault(x => x.KeyACUrl == PABakeryTempService.MaterialTempertureConfigKeyACUrl
                                                                                   && x.VBiACClassID == recvPointID)?.Value as double?;

                    if (iceTemp == null)
                    {
                        ACPropertyExt tempProp = dryIce.ACProperties.GetOrCreateACPropertyExtByName("Temperature", false);
                        if (tempProp != null)
                        {
                            iceTemp = tempProp.Value as double?;
                        }
                    }

                    dryIceTemp = new MaterialTemperature() { Material = dryIce, AverageTemperature = iceTemp, Water = WaterType.DryIce };
                    componentTemp.Add(dryIceTemp);
                }
            }

            DetermineCompTempFromMaterial(componentTemp, picking, recvPoint.RoomTemp);

            if (dryIceTemp != null && !dryIceTemp.AverageTemperature.HasValue)
            {
                dryIceTemp.AverageTemperature = -1;

                //Info50079: The temperature of ICE for calcuation is now -1 °C. Please configure default ICE temperature in the Material master.
                Msg msg = new Msg(this, eMsgLevel.Info, PWClassName, "DetermineComponentsTemperature(21)", 2012, "Info50079");
                OnNewAlarmOccurred(ProcessAlarm, msg);
            }

            SetRoomTemp(componentTemp, recvPoint.RoomTemp, matNoDryIce);

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

        private void SetRoomTemp(List<MaterialTemperature> componentTempList, double roomTemp, string matNoDryIce)
        {
            foreach (var compTemp in componentTempList.Where(c => !c.AverageTemperature.HasValue && c.Material.MaterialNo != matNoDryIce))
            {
                compTemp.AverageTemperature = roomTemp;
            }
        }

        [ACMethodInfo("", "", 9999, true)]
        public void SaveWorkplaceTemperatureSettings(double waterTemperature, bool isOnlyForWaterTempCalculation, double newWaterQuantity)
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
                    var configEntries = configStore.ConfigurationEntries.Where(c => c.PreConfigACUrl == PreValueACUrl && c.LocalConfigACUrl.StartsWith(ConfigACUrl))
                                                                        .OrderBy(x => x.VBiACClassID == null ? "" : x.VBACClass.ACIdentifier);

                    if (configEntries != null)
                    {
                        string propertyACUrl = string.Format("{0}\\{1}\\WaterTemp", ConfigACUrl, ACStateEnum.SMStarting);

                        // Water temp 
                        IACConfig waterTempConfig = configEntries.FirstOrDefault(c => c.LocalConfigACUrl == propertyACUrl);
                        if (waterTempConfig == null)
                        {
                            waterTempConfig = InsertTemperatureConfiguration(propertyACUrl, "WaterTemp", configStore);
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
                            useOnlyForWaterTempCalculation = InsertTemperatureConfiguration(propertyACUrl, "UseWaterTemp", configStore);
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

                if (WaterTotalQuantity.ValueT != newWaterQuantity)
                {
                    using(ACMonitor.Lock(_20015_LockValue))
                        _NewWaterQuantity = newWaterQuantity;
                }

                ResetMembers(false);
            }

            using (ACMonitor.Lock(_20015_LockValue))
            {
                _RecalculateTemperatures = true;
            }
            SubscribeToProjectWorkCycle();
        }

        private IACConfig InsertTemperatureConfiguration(string propertyACUrl, string paramACIdentifier, IACConfigStore configStore)
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

                IACConfig configParam = ConfigManagerIPlus.ACConfigFactory(configStore, param, PreValueACUrl, propertyACUrl, null);
                param.ConfigurationList.Insert(0, configParam);

                configStore.ConfigurationEntries.Append(configParam);

                return configParam;
            }

            return null;
        }

        [ACMethodInfo("", "", 9999, true)]
        public ACValueList GetTemperaturesUsedInCalc()
        {
            return WaterTemperaturesUsedInCalc;
        }

        #endregion

        #region Methods => Other

        [ACMethodInfo("", "", 9999)]
        public void UserResponseYes()
        {
            using (ACMonitor.Lock(_20015_LockValue))
            {
                _UserResponse = true;
            }
            SubscribeToProjectWorkCycle();
        }

        public bool IsEnabledUserResponseYes()
        {
            return AskUserIsWaterNeeded;
        }

        [ACMethodInfo("", "", 9999)]
        public void UserResponseNo()
        {
            using (ACMonitor.Lock(_20015_LockValue))
            {
                _UserResponse = false;
            }
            SubscribeToProjectWorkCycle();
        }

        public bool IsEnabledUserResponseNo()
        {
            return AskUserIsWaterNeeded;
        }

        [ACMethodInfo("", "", 9999)]
        public bool? GetUserResponse()
        {
            using (ACMonitor.Lock(_20015_LockValue))
            {
                return _UserResponse;
            }
        }

        public bool? IsFirstItemForDosingInPicking()
        {
            PWMethodRelocation pwMethodRelocation = ParentPWMethod<PWMethodRelocation>();
            return IsFirstItemForDosingInPicking(pwMethodRelocation);

        }

        public static bool? IsFirstItemForDosingInPicking(PWMethodRelocation pwMethodRelocation)
        {
            if (pwMethodRelocation == null)
                return null;

            using (DatabaseApp dbApp = new DatabaseApp())
            {
                Picking picking = pwMethodRelocation.CurrentPicking.FromAppContext<Picking>(dbApp);

                if (picking == null)
                    return null;

                if (picking.PickingPos_Picking.Any(c => c.MDDelivPosLoadState != null && c.MDDelivPosLoadState.DelivPosLoadState == MDDelivPosLoadState.DelivPosLoadStates.LoadToTruck))
                    return false;

                return true;
            }
        }

        public bool? IsLastItemForDosingInPicking()
        {
            PWMethodRelocation pwMethodRelocation = ParentPWMethod<PWMethodRelocation>();
            return IsLastItemForDosingInPicking(pwMethodRelocation);
        }

        public static bool? IsLastItemForDosingInPicking(PWMethodRelocation pwMethodRelocation)
        {
            if (pwMethodRelocation == null)
                return null;

            using (DatabaseApp dbApp = new DatabaseApp())
            {
                Picking picking = pwMethodRelocation.CurrentPicking.FromAppContext<Picking>(dbApp);

                if (picking == null)
                    return null;

                if (!picking.PickingPos_Picking.Any(c => c.MDDelivPosLoadState.DelivPosLoadState < MDDelivPosLoadState.DelivPosLoadStates.LoadToTruck))
                    return true;

                return false;
            }
        }

        protected override void DumpPropertyList(XmlDocument doc, XmlElement xmlACPropertyList, ref DumpStats dumpStats)
        {
            base.DumpPropertyList(doc, xmlACPropertyList, ref dumpStats);

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

            xmlChild = xmlACPropertyList["_CalculatorMode"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("_CalculatorMode");
                if (xmlChild != null)
                    xmlChild.InnerText = _CalculatorMode.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["_RecalculateTemperatures"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("_RecalculateTemperatures");
                if (xmlChild != null)
                    xmlChild.InnerText = _RecalculateTemperatures.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["_UserResponse"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("_UserResponse");
                if (xmlChild != null)
                    xmlChild.InnerText = _UserResponse?.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["DryIceMaterialNo"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("DryIceMaterialNo");
                if (xmlChild != null)
                    xmlChild.InnerText = DryIceMaterialNo;
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["UseWaterTemp"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("UseWaterTemp");
                if (xmlChild != null)
                    xmlChild.InnerText = UseWaterTemp.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["UseWaterMixer"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("UseWaterMixer");
                if (xmlChild != null)
                    xmlChild.InnerText = UseWaterMixer.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["IncludeMeltingHeat"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("IncludeMeltingHeat");
                if (xmlChild != null)
                    xmlChild.InnerText = IncludeMeltingHeat.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["WaterMeltingHeat"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("WaterMeltingHeat");
                if (xmlChild != null)
                    xmlChild.InnerText = WaterMeltingHeat?.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["MeltingHeatInfluence"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("MeltingHeatInfluence");
                if (xmlChild != null)
                    xmlChild.InnerText = MeltingHeatInfluence?.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["AskUserIsWaterNeeded"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("AskUserIsWaterNeeded");
                if (xmlChild != null)
                    xmlChild.InnerText = AskUserIsWaterNeeded.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }
        }

        private void ResetMembers(bool completeReset = true)
        {
            TemperatureCalculationResult.ValueT = null;
            ColdWaterQuantity.ValueT = 0;
            CityWaterQuantity.ValueT = 0;
            WarmWaterQuantity.ValueT = 0;
            DryIceQuantity.ValueT = 0;
            WaterTotalQuantity.ValueT = 0;
            using (ACMonitor.Lock(_20015_LockValue))
            {
                _RecalculateTemperatures = true;
                _CalculatorMode = TempCalcMode.Calcuate;
                if (completeReset)
                {
                    _UserResponse = null;
                    _NewWaterQuantity = null;
                    _WaterTopParentPlPosRelQ = null;
                    _CityWaterMaterialNo = null;
                    _WarmWaterMaterialNo = null;
                    _ColdWaterMaterialNo = null;
                }
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
                    SaveWorkplaceTemperatureSettings((double)acParameter[0], (bool)acParameter[1], (double)acParameter[2]);
                    return true;
                case "GetTemperaturesUsedInCalc":
                    result = GetTemperaturesUsedInCalc();
                    return true;
                case "UserResponseYes":
                    UserResponseYes();
                    return true;
                case "UserResponseNo":
                    UserResponseNo();
                    return true;
                case "GetUserResponse":
                    result = GetUserResponse();
                    return true;
                case nameof(IsEnabledUserResponseYes):
                    result = IsEnabledUserResponseYes();
                    return true;
                case nameof(IsEnabledUserResponseNo):
                    result = IsEnabledUserResponseNo();
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
