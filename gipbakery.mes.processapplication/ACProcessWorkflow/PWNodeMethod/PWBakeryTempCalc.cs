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

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Dough temperature calculato'}de{'Teigtemperaturberechnung'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryTempCalc : PWNodeProcessMethod
    {
        public const string PWClassName = "PWBakeryTempCalc";

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
            method.ParameterValueList.Add(new ACValue("UseWaterTemp", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("UseWaterTemp", "en{'Use Watertemperature'}de{'Wassertemperatur verwenden'}");
            method.ParameterValueList.Add(new ACValue("UseWaterMixer", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("UseWaterMixer", "en{'Calculate for water mixer'}de{'Berechnen für Wassermischer'}");
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
            ClearMyConfiguration();
            return base.ACDeInit(deleteACClassTask);
        }

        public override void Recycle(IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
        {
            ClearMyConfiguration();
            base.Recycle(content, parentACObject, parameter, acIdentifier);
        }

        #endregion

        #region Properties
        public T ParentPWMethod<T>() where T : PWMethodVBBase
        {
            if (ParentRootWFNode == null)
                return null;
            return ParentRootWFNode as T;
        }

        //TODO: clear config on idle or recycle
        private ACMethod _MyConfiguration;
        [ACPropertyInfo(9999)]
        public ACMethod MyConfiguration
        {
            get
            {
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    if (_MyConfiguration != null)
                        return _MyConfiguration;
                }

                var myNewConfig = NewACMethodWithConfiguration();
                _MyConfiguration = myNewConfig;
                return myNewConfig;
            }
        }

        public bool IsProduction
        {
            get
            {
                return ParentPWMethod<PWMethodProduction>() != null;
            }
        }

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
                        return (double?) acValue.Value;
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

        private Type _PWManualWeighingType = typeof(PWManualWeighing);

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
            ClearMyConfiguration();
            base.SMIdle();
        }

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        public override void SMRunning()
        {
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
            ClearMyConfiguration();
            base.Reset();
        }

        [ACMethodInteractionClient("", "en{'Acknowledge'}de{'Bestätigen'}", 450, false)]
        new public static void AckStartClient(IACComponent acComponent)
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

        new public static bool IsEnabledAckStartClient(IACComponent acComponent)
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

        public void ClearMyConfiguration()
        {
            using (ACMonitor.Lock(_20015_LockValue))
            {
                _MyConfiguration = null;
            }
        }

        private void CalculateTargetTemperature()
        {
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
                    string dryIce = "";

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


                    double componentsQ = CalculateComponents_Q_(recvPoint, dbApp, pwMethodProduction, kneedingRiseTemperature, currentProdOrderPartslist, intermediates,
                                                                relations, plMethod, tempFromService, coldWater, cityWater, warmWater, dryIce);

                    double suggestedWaterTemp = CalculateWaterTemperatureSuggestion(UseWaterTemp, relations, DoughTemp.Value, doughTargetTempBeforeKneeding,
                                                                                    componentsQ, cityWater);
                }
            }


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

        private double CalculateComponents_Q_(BakeryReceivingPoint recvPoint, DatabaseApp dbApp, PWMethodProduction pwMethodProduction, double kneedingTemperature,
                                              ProdOrderPartslist poPartslist, IEnumerable<ProdOrderPartslistPos> intermediates, IEnumerable<ProdOrderPartslistPosRelation> relations,
                                              PartslistACClassMethod plMethod, List<MaterialTemperature> tempFromService, string coldWaterMatNo, string cityWaterMatNo, 
                                              string warmWaterMatNo, string dryIceMatNo)
        {

            List<MaterialTemperature> compTemps = DetermineComponentsTemperature(poPartslist, intermediates, recvPoint, plMethod, dbApp, tempFromService, coldWaterMatNo,
                                                                                 cityWaterMatNo, warmWaterMatNo, dryIceMatNo);

            var relationsWithoutWater = relations.Where(c => c.SourceProdOrderPartslistPos.Material.MaterialNo != coldWaterMatNo
                                                          && c.SourceProdOrderPartslistPos.Material.MaterialNo != cityWaterMatNo
                                                          && c.SourceProdOrderPartslistPos.Material.MaterialNo != warmWaterMatNo
                                                          && c.SourceProdOrderPartslistPos.Material.MaterialNo != dryIceMatNo);

            double totalQ = 0;

            //n_Q_komp += n_C_spez * nSollKg * (n_T_TeigSollTempVorKneten - n_T_komp);

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


        private double CalculateWaterTemperatureSuggestion(bool calculateWaterTemp, IEnumerable<ProdOrderPartslistPosRelation> relations, double doughTargetTempAfterKneeding,
                                                           double doughTargetTempBeforeKneeding, double componentsQ, string cityWaterMatNo)
        {
            double suggestedWaterTemperature = 20;
            double defaultWaterTemp = 0; //TODO

            bool isOnlyWater = relations.GroupBy(c => c.SourceProdOrderPartslistPos).Count(x => x.Key.Material.MaterialNo == cityWaterMatNo) == 1;
            var waterComp = relations.FirstOrDefault(c => c.SourceProdOrderPartslistPos.Material.MaterialNo == cityWaterMatNo);
            double? waterTargetQuantity = waterComp?.TargetQuantity;
            double? waterSpecHeatCapacity = waterComp.SourceProdOrderPartslistPos.Material.SpecHeatCapacity;

            if (calculateWaterTemp)
            {
                if (isOnlyWater)
                {
                    suggestedWaterTemperature = doughTargetTempAfterKneeding;
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
                }
                else if( waterTargetQuantity.HasValue && waterSpecHeatCapacity.HasValue && waterTargetQuantity > 0.00001 && waterSpecHeatCapacity > 0.00001)
                {
                    suggestedWaterTemperature = (componentsQ / (waterTargetQuantity.Value * waterSpecHeatCapacity.Value)) + doughTargetTempBeforeKneeding;
                }
            }


            return suggestedWaterTemperature;
        }

        private void CalculateWaterTypes()
        {

        }


        private List<MaterialTemperature> DetermineComponentsTemperature(ProdOrderPartslist prodOrderPartslist, IEnumerable<ProdOrderPartslistPos> intermediates, 
                                                                         BakeryReceivingPoint recvPoint, PartslistACClassMethod plMethod, DatabaseApp dbApp, 
                                                                         List<MaterialTemperature> tempFromService, string matNoColdWater, 
                                                                         string matNoCityWater, string matNoWarmWater, string matNoDryIce)
        {
            IEnumerable<PartslistPos> partslistPosList = prodOrderPartslist.Partslist?.PartslistPos_Partslist.Where(c => c.IsOutwardRoot).ToArray();

            List<MaterialTemperature> componentTemp = partslistPosList.Select(c => new MaterialTemperature() { Material = c.Material }).ToList();

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
                if(ext != null)
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
            foreach(Material manualComponent in manualComponents)
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

        [ACMethodInfo("","",9999, true)]
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
                            if (isOnlyForWaterTempCalculation)
                            {
                                string propertyACUrl = string.Format("{0}\\{1}\\WaterTemp", ConfigACUrl, ACStateEnum.SMStarting);

                                // Water temp 
                                IACConfig waterTempConfig = configEntries.FirstOrDefault(c => c.LocalConfigACUrl == propertyACUrl);
                                if (waterTempConfig == null)
                                {
                                    waterTempConfig = InsertTemperatureConfiguration(propertyACUrl, "WaterTemp", accessedProcessModuleID.Value, partslistConfigStore); 
                                }

                                if(waterTempConfig == null)
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

                                if(useOnlyForWaterTempCalculation == null)
                                {
                                    //TODO: alarm
                                }
                                else
                                    useOnlyForWaterTempCalculation.Value = isOnlyForWaterTempCalculation;
                            }
                        }
                    }
                }

                //TODO: alarm
                dbApp.ACSaveChanges();

                ClearMyConfiguration();

            }
            //ConfigManagerIPlus.ACConfigFactory(CurrentConfigStore, )
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
}
