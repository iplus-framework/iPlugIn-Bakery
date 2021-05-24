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

        #region Constructors

        static PWBakeryTempCalc()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("MessageText", typeof(string), "", Global.ParamOption.Required));
            paramTranslation.Add("MessageText", "en{'Question text'}de{'Abfragetext'}");

            method.ParameterValueList.Add(new ACValue("PasswordDlg", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("PasswordDlg", "en{'With password dialogue'}de{'Mit Passwort-Dialog'}");

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
            ResetPrivateMembers();
            return base.ACDeInit(deleteACClassTask);
        }

        public override void Recycle(IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
        {
            ResetPrivateMembers();
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

        [ACPropertyBindingSource]
        public IACContainerTNet<double> WaterCalcResult
        {
            get;
            set;
        }

        private Type _PWManualWeighingType = typeof(PWManualWeighing);

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

        private bool _RecalculateTemperatures = true;

        #region Properties => Configuration

        ////TODO: clear config on idle or recycle
        //private ACMethod _MyConfiguration;
        //[ACPropertyInfo(9999)]
        //public ACMethod MyConfiguration
        //{
        //    get
        //    {
        //        using (ACMonitor.Lock(_20015_LockValue))
        //        {
        //            if (_MyConfiguration != null)
        //                return _MyConfiguration;
        //        }

        //        var myNewConfig = NewACMethodWithConfiguration();
        //        _MyConfiguration = myNewConfig;
        //        return myNewConfig;
        //    }
        //}

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

        #region Execute-Helper-Handlers
        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            //result = null;
            //switch (acMethodName)
            //{
            //}
            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        public static bool HandleExecuteACMethod_PWBakeryTempCalc(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            //switch (acMethodName)
            //{
            //    case MN_AckStartClient:
            //        AckStartClient(acComponent);
            //        return true;
            //    case Const.IsEnabledPrefix + MN_AckStartClient:
            //        result = IsEnabledAckStartClient(acComponent);
            //        return true;
            //}
            return HandleExecuteACMethod_PWBaseNodeProcess(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
        #endregion

        public override void Start()
        {
            base.Start();
        }

        public override void SMIdle()
        {
            ResetPrivateMembers();
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
                CalculateTargetTemperature();

            base.SMRunning();
        }

        [ACMethodState("en{'Completed'}de{'Beendet'}", 40, true)]
        public override void SMCompleted()
        {
            base.SMCompleted();
        }

        public override void Reset()
        {
            ResetPrivateMembers();
            base.Reset();
        }

        [ACMethodInteractionClient("", "en{'Acknowledge'}de{'Bestätigen'}", 450, false)]
        public new static void AckStartClient(IACComponent acComponent)
        {
            ACComponent _this = acComponent as ACComponent;
            if (!IsEnabledAckStartClient(acComponent))
                return;
            ACStateEnum acState = (ACStateEnum)_this.ACUrlCommand("ACState");

            // TODO: Open Businesobject with calculation of water components
            string result = _this.Messages.InputBox("Temperatur", "0.0");

            // If needs Password
            if (acState == ACStateEnum.SMStarting)
            {
                string bsoName = "BSOChangeMyPW";
                ACBSO childBSO = acComponent.Root.Businessobjects.ACUrlCommand("?" + bsoName) as ACBSO;
                if (childBSO == null)
                    childBSO = acComponent.Root.Businessobjects.StartComponent(bsoName, null, new object[] { }) as ACBSO;
                if (childBSO == null)
                    return;
                VBDialogResult dlgResult = childBSO.ACUrlCommand("!ShowCheckUserDialog") as VBDialogResult;
                if (dlgResult != null && dlgResult.SelectedCommand == eMsgButton.OK)
                {
                    acComponent.ACUrlCommand("!AckStart");
                }
                childBSO.Stop();
            }
            else
                acComponent.ACUrlCommand("!AckStart");
        }

        public new static bool IsEnabledAckStartClient(IACComponent acComponent)
        {
            ACComponent _this = acComponent as ACComponent;
            ACStateEnum acState = (ACStateEnum)_this.ACUrlCommand("ACState");
            return acState == ACStateEnum.SMRunning || acState == ACStateEnum.SMStarting;
        }


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

        public void ResetPrivateMembers()
        {
            using (ACMonitor.Lock(_20015_LockValue))
            {
                _RecalculateTemperatures = true;
            }
        }

        private void CalculateTargetTemperature()
        {
            bool recalc = false;

            using(ACMonitor.Lock(_20015_LockValue))
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
            }

            PWMethodProduction pwMethodProduction = ParentPWMethod<PWMethodProduction>();
            // If dosing is not for production, then do nothing
            if (pwMethodProduction == null)
                return;

            BakeryReceivingPoint recvPoint = ParentPWGroup.AccessedProcessModule as BakeryReceivingPoint;
            if (recvPoint == null)
            {
                //TODO: Error msg: Accessed process module on PWGroup is null or is not BakeryReceivingPoint!
                return;
            }

            //TODO: check if something changed (water temp or dough temp ....)

            if (string.IsNullOrEmpty(DryIceMaterialNo))
            {
                //TODO: error => configure dry ice material number
            }

            using (Database db = new gip.core.datamodel.Database())
            using (DatabaseApp dbApp = new DatabaseApp())
            {
                // Dough temperature active
                if (!UseWaterTemp)
                {
                    if (DoughTemp == null)
                    {
                        //TODO: alarm
                        return;
                    }

                    double kneedingRiseTemperature = 0;
                    bool kneedingTempResult = GetKneedingRiseTemperature(dbApp, out kneedingRiseTemperature);
                    if (!kneedingTempResult)
                        return;

                    double recvPointCorrTemp = recvPoint.DoughCorrTemp.ValueT;

                    double doughTargetTempBeforeKneeding = DoughTemp.Value - kneedingRiseTemperature + recvPointCorrTemp;

                    ACValueList componentTemperaturesService = recvPoint.GetComponentTemperatures();
                    List<MaterialTemperature> tempFromService = componentTemperaturesService.Select(c => c.Value as MaterialTemperature).ToList();

                    string coldWater = tempFromService.FirstOrDefault(c => c.Water == WaterType.ColdWater)?.MaterialNo;
                    string cityWater = tempFromService.FirstOrDefault(c => c.Water == WaterType.CityWater)?.MaterialNo;
                    string warmWater = tempFromService.FirstOrDefault(c => c.Water == WaterType.WarmWater)?.MaterialNo;
                    string dryIce = DryIceMaterialNo;

                    if (string.IsNullOrEmpty(coldWater) || string.IsNullOrEmpty(cityWater) || string.IsNullOrEmpty(warmWater) || string.IsNullOrEmpty(dryIce))
                    {
                        //TODO error
                    }

                    ProdOrderPartslistPos endBatchPos = pwMethodProduction.CurrentProdOrderPartslistPos.FromAppContext<ProdOrderPartslistPos>(dbApp);

                    if (pwMethodProduction.CurrentProdOrderBatch == null)
                    {
                        // Error50276: No batch assigned to last intermediate material of this workflow
                        Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartManualWeighingProd(30)", 1010, "Error50276");

                        //if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                        //    Messages.LogError(this.GetACUrl(), msg.ACIdentifier, msg.InnerMessage);
                        //OnNewAlarmOccurred(ProcessAlarm, msg, false);
                        //return StartNextCompResult.CycleWait;
                    }

                    var contentACClassWFVB = ContentACClassWF.FromAppContext<gip.mes.datamodel.ACClassWF>(dbApp);
                    ProdOrderBatch batch = pwMethodProduction.CurrentProdOrderBatch.FromAppContext<ProdOrderBatch>(dbApp);
                    ProdOrderBatchPlan batchPlan = batch.ProdOrderBatchPlan;

                    PartslistACClassMethod plMethod = endBatchPos.ProdOrderPartslist.Partslist.PartslistACClassMethod_Partslist.FirstOrDefault();
                    ProdOrderPartslist currentProdOrderPartslist = endBatchPos.ProdOrderPartslist.FromAppContext<ProdOrderPartslist>(dbApp);

                    IEnumerable<ProdOrderPartslistPos> intermediates = currentProdOrderPartslist.ProdOrderPartslistPos_ProdOrderPartslist
                                                                                                .Where(c => c.MaterialID.HasValue
                                                                                                         && c.MaterialPosType == GlobalApp.MaterialPosTypes.InwardIntern
                                                                                                        && !c.ParentProdOrderPartslistPosID.HasValue)
                                                                                                .SelectMany(p => p.ProdOrderPartslistPos_ParentProdOrderPartslistPos)
                                                                                                .ToArray();


                    var relations = intermediates.Select(c => new Tuple<bool?, ProdOrderPartslistPos>(c.Material.ACProperties
                                                             .GetOrCreateACPropertyExtByName("UseInTemperatureCalculation", false)?.Value as bool?, c))
                                                 .Where(c => c.Item1.HasValue && c.Item1.Value)
                                                 .SelectMany(x => x.Item2.ProdOrderPartslistPosRelation_TargetProdOrderPartslistPos)
                                                 .Where(c => c.SourceProdOrderPartslistPos.IsOutwardRoot).ToArray();

                    List<MaterialTemperature> compTemps = DetermineComponentsTemperature(currentProdOrderPartslist, intermediates, recvPoint, plMethod, dbApp, tempFromService, coldWater,
                                                                                         cityWater, warmWater, dryIce);


                    double componentsQ = CalculateComponents_Q_(recvPoint, kneedingRiseTemperature, relations, coldWater, cityWater, warmWater, dryIce, compTemps);

                    bool isOnlyWaterCompsInPartslist = relations.Count() == 1 && relations.FirstOrDefault(c => c.SourceProdOrderPartslistPos.Material.MaterialNo == cityWater) != null;
                    var waterComp = relations.FirstOrDefault(c => c.SourceProdOrderPartslistPos.Material.MaterialNo == cityWater);
                    double? waterTargetQuantity = waterComp?.TargetQuantity;
                    if (!waterTargetQuantity.HasValue)
                    {
                        //todo error: in partslist missing water
                    }

                    double defaultWaterTemp = 0;
                    double suggestedWaterTemp = CalculateWaterTemperatureSuggestion(UseWaterTemp, isOnlyWaterCompsInPartslist, waterComp, DoughTemp.Value, doughTargetTempBeforeKneeding,
                                                                                    componentsQ, cityWater, out defaultWaterTemp);

                    CalculateWaterTypes(compTemps, suggestedWaterTemp, waterTargetQuantity.Value, defaultWaterTemp, componentsQ, isOnlyWaterCompsInPartslist, doughTargetTempBeforeKneeding);
                }
                else
                {
                    //TODO only water calc
                }
            }

            using (ACMonitor.Lock(_20015_LockValue))
            {
                _RecalculateTemperatures = false;
            }
            UnSubscribeToProjectWorkCycle();
        }

        //TODO: half quantity
        private bool GetKneedingRiseTemperature(DatabaseApp dbApp, out double kneedingTemperature)
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
                if (compTemp != null)
                    componentTemperature = compTemp.AverageTemperature;

                totalQ += rel.SourceProdOrderPartslistPos.Material.SpecHeatCapacity * rel.TargetQuantity * (kneedingTemperature - componentTemperature);
            }

            return totalQ;
        }


        private double CalculateWaterTemperatureSuggestion(bool calculateWaterTemp, bool isOnlyWater, ProdOrderPartslistPosRelation waterComp, double doughTargetTempAfterKneeding,
                                                           double doughTargetTempBeforeKneeding, double componentsQ, string cityWaterMatNo, out double defaultWaterTemp)
        {
            double suggestedWaterTemperature = 20;
            defaultWaterTemp = 0; //TODO

            double? waterTargetQuantity = waterComp?.TargetQuantity;
            double? waterSpecHeatCapacity = waterComp.SourceProdOrderPartslistPos.Material.SpecHeatCapacity;

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
                                         double componentsQ, bool isOnlyWaterCompInPartslist, double doughTempBeforeKneeding)
        {
            MaterialTemperature coldWater = componentTemperatures.FirstOrDefault(c => c.Water == WaterType.ColdWater);
            MaterialTemperature cityWater = componentTemperatures.FirstOrDefault(c => c.Water == WaterType.CityWater);
            MaterialTemperature warmWater = componentTemperatures.FirstOrDefault(c => c.Water == WaterType.WarmWater);
            MaterialTemperature dryIce = componentTemperatures.FirstOrDefault(c => c.Water == WaterType.DryIce); //TODO ice from config

            if (coldWater == null)
            {
                //TODO: error
            }

            if (cityWater == null)
            {
                //TODO: error
            }

            if (warmWater == null)
            {
                //TODO error
                return;
            }

            if (dryIce == null)
            {
                //TODO: error
                return;
            }

            //check warm water 
            if (targetWaterTemperature > warmWater.AverageTemperature)
            {
                string message = string.Format("Calculated water temperature is {0} °C and the target quantity is {1} kg.", targetWaterTemperature.ToString("F2"), totalWaterQuantity);  //TODO unit
                TemperatureCalculationResult.ValueT = message;
                WaterCalcResult.ValueT = targetWaterTemperature;

                //TODO: Temperature Alarm, the calculated water temperature can not be reached

                return;
            }

            if (CombineWarmCityWater(warmWater, cityWater, targetWaterTemperature, totalWaterQuantity))
            {
                string message = string.Format("Calculated water temperature is {0} °C and the target quantity is {1} kg.", targetWaterTemperature.ToString("F2"), totalWaterQuantity);  //TODO unit
                TemperatureCalculationResult.ValueT = message;
                WaterCalcResult.ValueT = targetWaterTemperature;

                return;
            }

            if (CombineColdCityWater(coldWater, cityWater, targetWaterTemperature, totalWaterQuantity))
            {
                string message = string.Format("Calculated water temperature is {0} °C and the target quantity is {1} kg.", targetWaterTemperature.ToString("F2"), totalWaterQuantity);  //TODO unit
                TemperatureCalculationResult.ValueT = message;
                WaterCalcResult.ValueT = targetWaterTemperature;

                return;
            }

            if (!CombineWatersWithDryIce(coldWater, dryIce, targetWaterTemperature, totalWaterQuantity, defaultWaterTemp, isOnlyWaterCompInPartslist, componentsQ, doughTempBeforeKneeding))
            {
                //TODO: alarm
            }

            string message1 = string.Format("Calculated water temperature is {0} °C and the target quantity is {1} kg.", targetWaterTemperature.ToString("F2"), totalWaterQuantity);  //TODO unit
            TemperatureCalculationResult.ValueT = message1;
            WaterCalcResult.ValueT = targetWaterTemperature;
        }

        private bool CombineWarmCityWater(MaterialTemperature warmWater, MaterialTemperature cityWater, double targetWaterTemperature, double totalWaterQuantity)
        {
            if (targetWaterTemperature <= warmWater.AverageTemperature && targetWaterTemperature > cityWater.AverageTemperature)
            {
                double warmWaterSHC = warmWater.Material.SpecHeatCapacity;
                double cityWaterSHC = cityWater.Material.SpecHeatCapacity;

                double warmWaterQuantity = (totalWaterQuantity * (cityWaterSHC * (cityWater.AverageTemperature - targetWaterTemperature)))
                                           / ((cityWaterSHC * (cityWater.AverageTemperature - targetWaterTemperature))
                                           + (warmWaterSHC * (targetWaterTemperature - warmWater.AverageTemperature)));

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

                double cityWaterQuantity = (totalWaterQuantity * (coldWaterSHC * (coldWater.AverageTemperature - targetWaterTemperature)))
                                           / ((coldWaterSHC * (coldWater.AverageTemperature - targetWaterTemperature))
                                           + (cityWaterSHC * (targetWaterTemperature - cityWater.AverageTemperature)));

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

                    double coldWaterQuantity = (totalWaterQuantity * (waterSHC * (dryIce.AverageTemperature - targetWaterTemperature))) / ((waterSHC * (dryIce.AverageTemperature - targetWaterTemperature)) + (waterSHC * (targetWaterTemperature - coldWater.AverageTemperature)));
                    double dryIceQuantity = totalWaterQuantity - coldWaterQuantity;

                    if (coldWaterQuantity < coldWater.WaterMinDosingQuantity)
                    {
                        coldWaterQuantity = totalWaterQuantity;
                        dryIceQuantity = 0;
                    }
                    else if (dryIceQuantity < coldWater.WaterMinDosingQuantity) // TODO: find manual scale on receiving point and get min weighing quantity
                    {
                        coldWaterQuantity = 0;
                        dryIceQuantity = totalWaterQuantity;
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
                    return false; //TODO: Error message
                }
            }
            else
            {
                if (CalculateWaterTypesWithComponentsQ(defaultWaterTemp, isOnlyWaterCompInPartslist))
                {
                    double deltaTemp = defaultWaterTemp - targetWaterTemperature;
                    double deltaTempCold = coldWater.AverageTemperature + deltaTemp;

                    double waterSHC = coldWater.Material.SpecHeatCapacity;

                    double iceQuantity = (componentsQ + totalWaterQuantity * waterSHC * (doughTempBeforeKneeding - deltaTempCold))
                                        / ((WaterMeltingHeat.Value * MeltingHeatInfluence.Value) - waterSHC * (doughTempBeforeKneeding - deltaTempCold) + waterSHC
                                        * (doughTempBeforeKneeding - dryIce.AverageTemperature));

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
                        coldWaterQuantity = (totalWaterQuantity * (waterSHC * (dryIce.AverageTemperature - targetWaterTemperature)))
                                           / ((waterSHC * (dryIce.AverageTemperature - targetWaterTemperature))
                                           + (waterSHC * (targetWaterTemperature - coldWater.AverageTemperature)));

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

                    if (targetWaterTemperature <= coldWater.AverageTemperature && targetWaterTemperature > dryIce.AverageTemperature)
                    {
                        bool ice = true; //TODO: ask Damir

                        if (ice)
                        {
                            double iceQuantity = (waterSHC * totalWaterQuantity * (coldWater.AverageTemperature - targetWaterTemperature))
                                                 / ((iceSCH * (0 - dryIce.AverageTemperature)) + (WaterMeltingHeat.Value * MeltingHeatInfluence.Value)
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
                            double iceQuantity = (totalWaterQuantity * (coldWater.AverageTemperature - targetWaterTemperature)) / (coldWater.AverageTemperature - dryIce.AverageTemperature);
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
                        //error
                    }
                }
            }
            return true;
        }
        //TODO: phases
        private bool CalculateWaterTypesWithComponentsQ(double defaultWaterTemp, bool isOnlyWaterCompInPartslist)
        {
            if (defaultWaterTemp < 0.00001 && !isOnlyWaterCompInPartslist)
            {
                return true;
            }

            return false;
        }


        private List<MaterialTemperature> DetermineComponentsTemperature(ProdOrderPartslist prodOrderPartslist, IEnumerable<ProdOrderPartslistPos> intermediates,
                                                                         BakeryReceivingPoint recvPoint, PartslistACClassMethod plMethod, DatabaseApp dbApp,
                                                                         List<MaterialTemperature> tempFromService, string matNoColdWater,
                                                                         string matNoCityWater, string matNoWarmWater, string matNoDryIce)
        {
            IEnumerable<PartslistPos> partslistPosList = prodOrderPartslist.Partslist?.PartslistPos_Partslist.Where(c => c.IsOutwardRoot).ToArray();

            List<MaterialTemperature> componentTemp = partslistPosList.Select(c => new MaterialTemperature() { Material = c.Material }).ToList();

            var coldWater = componentTemp.FirstOrDefault(c => c.Material.MaterialNo == matNoColdWater);
            if (coldWater != null)
                coldWater.Water = WaterType.ColdWater;

            var cityWater = componentTemp.FirstOrDefault(c => c.Material.MaterialNo == matNoCityWater);
            if (cityWater != null)
                cityWater.Water = WaterType.CityWater;

            var warmWater = componentTemp.FirstOrDefault(c => c.Material.MaterialNo == matNoWarmWater);
            if (warmWater != null)
                warmWater.Water = WaterType.WarmWater;

            var dryIceMaterial = componentTemp.FirstOrDefault(c => c.Material.MaterialNo == matNoDryIce);
            if (dryIceMaterial != null)
                dryIceMaterial.Water = WaterType.DryIce;

            DetermineCompTempFromPartslistOrMaterial(componentTemp, partslistPosList, recvPoint.RoomTemperature.ValueT);

            if (plMethod != null)
            {
                var intermediatesManual = intermediates.Where(c => c.Material.MaterialWFConnection_Material
                                                                             .Any(x => x.MaterialWFACClassMethodID == plMethod.MaterialWFACClassMethodID
                                                                          && _PWManualWeighingType.IsAssignableFrom(x.ACClassWF.PWACClass
                                                                                                                     .FromIPlusContext<gip.core.datamodel.ACClass>(dbApp.ContextIPlus)
                                                                                                                     .ObjectType)));

                if (intermediatesManual != null && intermediatesManual.Any())
                {
                    var components = intermediatesManual.SelectMany(c => c.ProdOrderPartslistPosRelation_TargetProdOrderPartslistPos.Select(x => x.SourceProdOrderPartslistPos.Material))
                                                        .ToArray().Where(m => m.MaterialNo != matNoColdWater && m.MaterialNo != matNoCityWater && m.MaterialNo != matNoWarmWater
                                                                           && m.MaterialNo != matNoDryIce);

                    SetManualCompTemp(componentTemp, components, recvPoint.RoomTemperature.ValueT);
                }
            }

            SetCompTempFromService(componentTemp, tempFromService, recvPoint.RoomTemperature.ValueT);

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

        //TODO: double comparation with 0
        private void DetermineCompTempFromPartslistOrMaterial(List<MaterialTemperature> componentTemp, IEnumerable<PartslistPos> partslistPosList, double roomTemp)
        {
            foreach (PartslistPos pos in partslistPosList)
            {
                if (pos.ACProperties == null)
                    continue;

                ACPropertyExt ext = pos.ACProperties.GetOrCreateACPropertyExtByName("Temperature", false);
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

                ext = pos.ACProperties.GetOrCreateACPropertyExtByName("UseRoomTemperature", false);
                if (ext != null)
                {
                    bool? value = (ext.Value as bool?);
                    if (value.HasValue && value.Value)
                    {
                        MaterialTemperature mt = componentTemp.FirstOrDefault(c => c.Material.MaterialID == pos.Material.MaterialID);
                        if (mt != null)
                            mt.AverageTemperature = roomTemp;
                        continue;
                    }
                }

                ext = pos.Material.ACProperties.GetOrCreateACPropertyExtByName("Temperature", false);
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

                ext = pos.Material.ACProperties.GetOrCreateACPropertyExtByName("UseRoomTemperature", false);
                if (ext != null)
                {
                    bool? value = (ext.Value as bool?);
                    if (value.HasValue && value.Value)
                    {
                        MaterialTemperature mt = componentTemp.FirstOrDefault(c => c.Material.MaterialID == pos.Material.MaterialID);
                        if (mt != null)
                            mt.AverageTemperature = roomTemp;
                        continue;
                    }
                }
            }
        }

        private void SetManualCompTemp(List<MaterialTemperature> componentTempList, IEnumerable<Material> manualComponents, double roomTemp)
        {
            foreach (Material manualComponent in manualComponents)
            {
                MaterialTemperature mt = componentTempList.FirstOrDefault(c => c.Material.MaterialNo == manualComponent.MaterialNo && c.AverageTemperature == 0);
                if (mt != null)
                    mt.AverageTemperature = roomTemp;

            }
        }

        private void SetCompTempFromService(List<MaterialTemperature> componentTempList, List<MaterialTemperature> serviceTempList, double roomTemp)
        {
            foreach (var compTemp in componentTempList.Where(c => c.AverageTemperature == 0))
            {
                MaterialTemperature serviceTemp = serviceTempList.FirstOrDefault(c => c.MaterialNo == compTemp.Material.MaterialNo);
                if (serviceTemp != null)
                {
                    compTemp.AverageTemperature = serviceTemp.AverageTemperature;
                }
                else
                {
                    //TODO: check if this OK
                    //If in service temperature not exist for this component then use room temperature
                    compTemp.AverageTemperature = roomTemp;
                }
            }
        }


        [ACMethodInfo("", "", 9999, true)]
        public void SaveWorkplaceTemperatureSettings(double waterTemperature, bool isOnlyForWaterTempCalculation)
        {
            using (DatabaseApp dbApp = new DatabaseApp())
            {
                IACConfigStore partslistConfigStore = (MandatoryConfigStores?.FirstOrDefault(c => c is Partslist) as Partslist)?.FromAppContext<Partslist>(dbApp);
                if (partslistConfigStore != null)
                {
                    Guid? accessedProcessModuleID = ParentPWGroup?.AccessedProcessModule?.ComponentClass?.ACClassID;

                    if (accessedProcessModuleID.HasValue)
                    {
                        var configEntries = partslistConfigStore.ConfigurationEntries.Where(c => c.PreConfigACUrl == PreValueACUrl && c.LocalConfigACUrl.StartsWith(ConfigACUrl)
                                                                                              && c.VBiACClassID == accessedProcessModuleID);

                        if (configEntries != null)
                        {
                            //if (isOnlyForWaterTempCalculation)
                            //{
                            string propertyACUrl = string.Format("{0}\\{1}\\WaterTemp", ConfigACUrl, ACStateEnum.SMStarting);

                            // Water temp 
                            IACConfig waterTempConfig = configEntries.FirstOrDefault(c => c.LocalConfigACUrl == propertyACUrl);
                            if (waterTempConfig == null)
                            {
                                waterTempConfig = InsertTemperatureConfiguration(propertyACUrl, "WaterTemp", accessedProcessModuleID.Value, partslistConfigStore);
                            }

                            if (waterTempConfig == null)
                            {
                                //TODO: alarm
                            }
                            else
                                waterTempConfig.Value = waterTemperature;

                            // Water temp 
                            propertyACUrl = string.Format("{0}\\{1}\\UseWaterTemp", ConfigACUrl, ACStateEnum.SMStarting);
                            IACConfig useOnlyForWaterTempCalculation = configEntries.FirstOrDefault(c => c.LocalConfigACUrl == propertyACUrl);
                            if (useOnlyForWaterTempCalculation == null)
                            {
                                useOnlyForWaterTempCalculation = InsertTemperatureConfiguration(propertyACUrl, "UseWaterTemp", accessedProcessModuleID.Value, partslistConfigStore);
                            }

                            if (useOnlyForWaterTempCalculation == null)
                            {
                                //TODO: alarm
                            }
                            else
                                useOnlyForWaterTempCalculation.Value = isOnlyForWaterTempCalculation;
                            //}
                        }
                    }
                }

                //TODO: alarm
                dbApp.ACSaveChanges();

                RootPW.ReloadConfig();

                ResetPrivateMembers();

            }
            //ConfigManagerIPlus.ACConfigFactory(CurrentConfigStore, )
            using(ACMonitor.Lock(_20015_LockValue))
            {
                _RecalculateTemperatures = true;
            }
            SubscribeToProjectWorkCycle();
        }

        private IACConfig InsertTemperatureConfiguration(string propertyACUrl, string paramACIdentifier, Guid processModuleID, IACConfigStore configStore)
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

        #endregion

        #region User Interaction
        #endregion

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
