﻿using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
using gip.mes.processapplication;
using gip.mes.datamodel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Bakery temperature service'}de{'Bäckerei-Temperaturservice'}", Global.ACKinds.TPAModule, IsRightmanagement = true)]
    public class PABakeryTempService : PAJobScheduler
    {
        #region c'tors

        public PABakeryTempService(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACInit(Global.ACStartTypes startChildMode = Global.ACStartTypes.Automatic)
        {
            return base.ACInit(startChildMode);
        }

        public override bool ACPreDeInit(bool deleteACClassTask = false)
        {
            return base.ACPreDeInit(deleteACClassTask);
        }

        public override bool ACPostInit()
        {
            bool result = base.ACPostInit();
            if (!result)
                return result;

            AddToPostInitQueue(() => {
            try
            {
                InitializeService();
            }
            catch (Exception ex)
            {
                Messages.LogException(this.GetACUrl(), "Root_PropertyChanged(10)", ex);
            }
            });

            return result;
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            //Root.PropertyChanged -= Root_PropertyChanged;
            return base.ACDeInit(deleteACClassTask);
        }

        public const string ClassName = "PABakeryTempService";
        public const string MN_GetTemperaturesInfo = "GetTemperaturesInfo";
        public const string MN_GetAverageTemperatures = "GetAverageTemperatures";
        public const string PN_TemperatureServiceInfo = "TemperatureServiceInfo";
        public const string MaterialTempertureConfigKeyACUrl = "MaterialTempConfig";

        #endregion

        #region Properties

        private readonly ACMonitorObject _30010_LockTempServiceInfo = new ACMonitorObject(30010);

        public Dictionary<BakeryReceivingPoint, BakeryRecvPointTemperature> Temperatures
        {
            get;
            set;
        }

        [ACPropertyInfo(700)]
        public bool ServiceInitialized
        {
            get
            {
                bool result = false;
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    result = Temperatures != null && Temperatures.Any();
                }
                return result;
            }
        }

        // 1 Refresh average temperatures
        // 2 Refresh temperatures info
        [ACPropertyBindingSource]
        public IACContainerTNet<short> TemperatureServiceInfo
        {
            get;
            set;
        }

        [ACPropertyBindingSource(710, "Error", "en{'Service alarm'}de{'Service Alarm'}", "", false, false)]
        public IACContainerTNet<PANotifyState> ServiceAlarm { get; set; }

        private List<ACRef<BakerySilo>> _PossibleSilos;

        #endregion

        #region Methods
        [ACMethodInteraction("", "", 700, true)]
        public void InitService()
        {
            try
            {
                InitializeService();
                RecalculateAverageTemperature();
            }
            catch (Exception ex)
            {
                Messages.LogException(this.GetACUrl(), "Root_PropertyChanged(10)", ex);
                //OnNewAlarmOccurred();
            }
        }

        protected override void RunJob(DateTime now, DateTime lastRun, DateTime nextRun)
        {
            base.RunJob(now, lastRun, nextRun);
            ApplicationManager.ApplicationQueue.Add(() => RecalculateAverageTemperature());
        }


        /// <summary>
        /// Returns a material temperatures in the ACValueList
        /// </summary>
        /// <param name="receivingPointID"></param>
        [ACMethodInfo("","",9999)]
        public ACValueList GetTemperaturesInfo(Guid receivingPointID)
        {
            KeyValuePair<BakeryReceivingPoint, BakeryRecvPointTemperature>? cacheItem = null;

            using (ACMonitor.Lock(_20015_LockValue))
            {
                cacheItem = Temperatures?.FirstOrDefault(c => c.Key.ComponentClass.ACClassID == receivingPointID);
            }

            if (cacheItem == null)
                return null;

            return new ACValueList(cacheItem.Value.Value.MaterialTempInfos.Where(t => !string.IsNullOrEmpty(t.MaterialNo)).Select(c => new ACValue(c.MaterialNo, c)).ToArray());
        }

        [ACMethodInfo("", "", 9999)]
        public MaterialTempBaseList GetAverageTemperatures(Guid receivingPointID)
        {
            KeyValuePair<BakeryReceivingPoint, BakeryRecvPointTemperature>? cacheItem = null;

            using (ACMonitor.Lock(_20015_LockValue))
            {
                cacheItem = Temperatures?.FirstOrDefault(c => c.Key.ComponentClass.ACClassID == receivingPointID);
            }

            if (cacheItem == null)
                return null;

            return new MaterialTempBaseList(cacheItem.Value.Value.MaterialTempInfos.Where(t => !string.IsNullOrEmpty(t.MaterialNo)));

            //return new ACValueList(cacheItem.Value.Value.MaterialTempInfos.Where(t => !string.IsNullOrEmpty(t.MaterialNo)).Select(c => new ACValue(c.MaterialNo, c.AverageTemperature)).ToArray());
        }

        [ACMethodInfo("", "", 9999)]
        public ACValueList GetWaterMaterialNo(Guid receivingPointID)
        {
            KeyValuePair<BakeryReceivingPoint, BakeryRecvPointTemperature>? cacheItem = null;
            using (ACMonitor.Lock(_20015_LockValue))
            {
                cacheItem = Temperatures?.FirstOrDefault(c => c.Key.ComponentClass.ACClassID == receivingPointID);
            }

            if (cacheItem == null)
                return null;

            return new ACValueList(cacheItem.Value.Value.MaterialTempInfos.Where(t => !string.IsNullOrEmpty(t.MaterialNo) && t.Water > WaterType.NotWater).Select(c => new ACValue(c.MaterialNo, c)).ToArray());
        }

        private void InitializeService()
        {
            DeinitCache();

            List<ACRef<BakerySilo>> silosWithBakeryPTC = new List<ACRef<BakerySilo>>();
            var temperatures = new Dictionary<BakeryReceivingPoint, BakeryRecvPointTemperature>();

            var projects = Root.FindChildComponents<ApplicationManager>(c => c is ApplicationManager, null, 1);

            List<BakeryReceivingPoint> receivingPoints = new List<BakeryReceivingPoint>();

            // Temperatures from PAMSilo

            foreach (var project in projects)
            {
                var possibleSilos = project.FindChildComponents<BakerySilo>(c => c is BakerySilo && c.ACComponentChilds.Any(x => x is PAEBakeryThermometer));

                foreach (var possibleSilo in possibleSilos)
                {
                    if (possibleSilo.Thermometers.Any(c => !c.DisabledForTempCalculation))
                    {
                        silosWithBakeryPTC.Add(new ACRef<BakerySilo>(possibleSilo,this));
                        possibleSilo.MaterialNo.PropertyChanged += MaterialNo_PropertyChanged;
                        possibleSilo.OutwardEnabled.PropertyChanged += OutwardEnabled_PropertyChanged;
                    }
                }

                receivingPoints.AddRange(project.FindChildComponents<BakeryReceivingPoint>(c => c is BakeryReceivingPoint));
            }

            _PossibleSilos = silosWithBakeryPTC;

            using (Database db = new gip.core.datamodel.Database())
            using (gip.mes.datamodel.DatabaseApp dbApp = new gip.mes.datamodel.DatabaseApp(db))
            {
                ACRoutingParameters routingParameters = new ACRoutingParameters()
                {
                    RoutingService = this.RoutingService,
                    Database = db,
                    AttachRouteItemsToContext = false,
                    Direction = RouteDirections.Forwards,
                    SelectionRuleID = PAMParkingspace.SelRuleID_ParkingSpace_Deselector,
                    MaxRouteAlternativesInLoop = 1,
                    IncludeReserved = true,
                    IncludeAllocated = true
                };

                foreach (var receivingPoint in receivingPoints)
                {
                    BakeryRecvPointTemperature tempInfo = new BakeryRecvPointTemperature(this);

                    foreach (var silo in silosWithBakeryPTC)
                    {
                        RoutingResult routingResult = ACRoutingService.SelectRoutes(silo.ValueT.ComponentClass, receivingPoint.ComponentClass, routingParameters);

                        if (routingResult != null && routingResult.Routes != null && routingResult.Routes.Any())
                            tempInfo.AddSilo(silo.ValueT);
                    }

                    temperatures.Add(receivingPoint, tempInfo);
                }

                // Temperatures for water/room

                foreach (var cacheItem in temperatures)
                {
                    //City water
                    InitializeWaterSensor(cacheItem, cacheItem.Key.PAPointMatIn2, dbApp, WaterType.CityWater);

                    //Cold water
                    InitializeWaterSensor(cacheItem, cacheItem.Key.PAPointMatIn3, dbApp, WaterType.ColdWater);

                    //Warm water
                    InitializeWaterSensor(cacheItem, cacheItem.Key.PAPointMatIn4, dbApp, WaterType.WarmWater);

                    //Room temp
                    cacheItem.Value.AddRoomTemperature(cacheItem.Key);
                }
            }

            using (ACMonitor.Lock(_20015_LockValue))
            {
                Temperatures = temperatures;
            }
        }

        private void MaterialNo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                SetTempServiceInfo(0);
                BakeryRecvPointTemperature[] values = null;

                using (ACMonitor.Lock(_20015_LockValue))
                {
                    values = Temperatures.Values.ToArray();
                }

                foreach (var bakeryTempItem in values)
                {
                    bakeryTempItem.SiloMaterialNoPropertyChanged(sender, e);
                }
                SetTempServiceInfo(2);
            }
        }

        private void OutwardEnabled_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<bool> propValue = sender as IACContainerTNet<bool>;
                if (propValue != null)
                {
                    SetTempServiceInfo(0);

                    BakeryRecvPointTemperature[] values = null;

                    using (ACMonitor.Lock(_20015_LockValue))
                    {
                        values = Temperatures.Values.ToArray();
                    }

                    foreach (var bakeryTempItem in values)
                    {
                        bakeryTempItem.SiloMaterialNoPropertyChanged(sender, e);
                    }
                    SetTempServiceInfo(2);
                }
            }
        }

        private void DeinitCache()
        {
            BakeryRecvPointTemperature[] values = null;

            using (ACMonitor.Lock(_20015_LockValue))
            {
                if (Temperatures == null)
                    return;

                values = Temperatures.Values.ToArray();
            }

            if (values == null)
                return;

            if (_PossibleSilos != null && _PossibleSilos.Any())
            {
                foreach (ACRef<BakerySilo> silo in _PossibleSilos)
                {
                    silo.ValueT.MaterialNo.PropertyChanged -= MaterialNo_PropertyChanged;
                    silo.ValueT.OutwardEnabled.PropertyChanged -= OutwardEnabled_PropertyChanged;
                }
            }

            foreach (var cacheItem in values)
            {
                cacheItem.DeInit();
            }
        }

        private void InitializeWaterSensor(KeyValuePair<BakeryReceivingPoint, BakeryRecvPointTemperature> cacheItem, PAPoint paPointMatIn, DatabaseApp dbApp, WaterType wType)
        {
            OnInitializeWaterSensor(cacheItem, paPointMatIn, dbApp, wType);

            ACRoutingParameters routingParameters = new ACRoutingParameters()
            {
                RoutingService = this.RoutingService,
                Database = dbApp.ContextIPlus,
                AttachRouteItemsToContext = false,
                SelectionRuleID = PAMTank.SelRuleID_Silo,
                Direction = RouteDirections.Backwards,
                MaxRouteAlternativesInLoop = 1,
                IncludeReserved = true,
                IncludeAllocated = true
            };

            RoutingResult rr = ACRoutingService.FindSuccessorsFromPoint(cacheItem.Key.ComponentClass, paPointMatIn.PropertyInfo, routingParameters);

            if (rr != null && rr.Routes != null && rr.Routes.Any())
            {
                RouteItem rItem = rr.Routes.FirstOrDefault()?.GetRouteSource();
                if (rItem != null)
                {
                    PAMSilo silo = rItem.SourceACComponent as PAMSilo;
                    if (silo != null)
                    {
                        string materialNo = silo.MaterialNo.ValueT;
                        if (string.IsNullOrEmpty(materialNo))
                        {
                            // Error50423 : Can not initalize {0} because in the water tank/source Material No missing.
                            Msg msg = new Msg(this, eMsgLevel.Error, ClassName, "InitializeWaterSensor(10)", 271, "Error50423", wType.ToString());
                            if (IsAlarmActive(ServiceAlarm, msg.Message) == null)
                            {
                                OnNewAlarmOccurred(ServiceAlarm, msg);
                                Root.Messages.LogMessageMsg(msg);
                            }
                            return;
                        }

                        PAFDosing dosing = paPointMatIn.ConnectionList.Where(c => c.TargetParentComponent is PAFDosing)
                                                                                   .FirstOrDefault()?.TargetParentComponent as PAFDosing;

                        PAEBakeryThermometer bakeryThermometer = null;

                        //try find temperature sensor under water scales/meters
                        if (dosing != null && dosing.CurrentScaleForWeighing != null)
                        {
                            bakeryThermometer = dosing.CurrentScaleForWeighing.FindChildComponents<PAEBakeryThermometer>(c => c is PAEBakeryThermometer, null, 1)
                                                                              .FirstOrDefault();

                            if (bakeryThermometer != null && bakeryThermometer.DisabledForTempCalculation)
                                bakeryThermometer = null;

                        }
                        
                        //try find temperature sensor under water tank
                        if (bakeryThermometer == null)
                        {
                            bakeryThermometer = silo.FindChildComponents<PAEBakeryThermometer>(c => c is PAEBakeryThermometer, null, 1)
                                                    .FirstOrDefault();

                            if (bakeryThermometer != null && bakeryThermometer.DisabledForTempCalculation)
                                bakeryThermometer = null;
                        }

                        var materialTempInfo = cacheItem.Value.MaterialTempInfos.FirstOrDefault(c => c.MaterialNo == materialNo && c.Water == wType);
                        if (materialTempInfo == null)
                        {
                            materialTempInfo = new MaterialTemperature();
                            materialTempInfo.MaterialNo = materialNo;
                            materialTempInfo.Material = dbApp.Material.FirstOrDefault(c => c.MaterialNo == materialNo);
                            if (dosing != null && dosing.CurrentScaleForWeighing !=  null)
                                materialTempInfo.WaterMinDosingQuantity = dosing.CurrentScaleForWeighing.MinDosingWeight.ValueT;
                            if (bakeryThermometer != null)
                            {
                                materialTempInfo.WaterDefaultTemperature = bakeryThermometer.TemperatureDefault;
                            }
                            else
                            {
                                ACPropertyExt ext = materialTempInfo.Material.ACProperties.GetOrCreateACPropertyExtByName("Temperature", false);
                                if (ext != null)
                                {
                                    double? value = (ext.Value as double?);
                                    if (value.HasValue && (value.Value >= 0.00001 || value.Value <= -0.00001))
                                    {
                                        materialTempInfo.WaterDefaultTemperature = value.Value;
                                    }
                                }

                                if (!materialTempInfo.WaterDefaultTemperature.HasValue)
                                {
                                    if (wType == WaterType.CityWater)
                                        materialTempInfo.WaterDefaultTemperature = 15;
                                    else if (wType == WaterType.ColdWater)
                                        materialTempInfo.WaterDefaultTemperature = 3;
                                    else if (wType == WaterType.WarmWater)
                                        materialTempInfo.WaterDefaultTemperature = 50;
                                }
                            }
                            materialTempInfo.Water = wType;
                            cacheItem.Value.MaterialTempInfos.Add(materialTempInfo);
                        }

                        if (bakeryThermometer != null)
                        {
                            materialTempInfo.AddThermometer(bakeryThermometer);
                        }
                    }
                }
            }
        }

        public virtual void OnInitializeWaterSensor(KeyValuePair<BakeryReceivingPoint, BakeryRecvPointTemperature> cacheItem, PAPoint paPointMatIn, DatabaseApp dbApp, WaterType wType)
        {

        }

        private void RecalculateAverageTemperature()
        {
            KeyValuePair<BakeryReceivingPoint, BakeryRecvPointTemperature>[] recalcItems = null;
            using (ACMonitor.Lock(_20015_LockValue))
            {
                if (Temperatures == null)
                    return;

                recalcItems = Temperatures?.ToArray();
            }

            SetTempServiceInfo(0);

            if (recalcItems == null)
                return;

            foreach (var recvPoint in recalcItems)
            {
                recvPoint.Value.RecalculateAverageTemperature(recvPoint.Key);
            }

            SetTempServiceInfo(1);

            WriteAverageTemperatureToMaterialConfig(recalcItems);

            using (ACMonitor.Lock(_20015_LockValue))
            {
                Temperatures = recalcItems.ToDictionary(c => c.Key, x => x.Value);
            }
        }

        private void WriteAverageTemperatureToMaterialConfig(KeyValuePair<BakeryReceivingPoint, BakeryRecvPointTemperature>[] recalcItems)
        {
            using (DatabaseApp dbApp = new DatabaseApp())
            {
                foreach (var temp in recalcItems)
                {
                    Guid acClassID = temp.Key.ComponentClass.ACClassID;

                    foreach (var material in temp.Value.MaterialTempInfos)
                    {
                        if (material.Material == null)
                            continue;

                        Material mt = material.Material.FromAppContext<Material>(dbApp);
                        if (mt == null)
                        {
                            continue;
                        }

                        MaterialConfig materialConfig = mt.MaterialConfig_Material.FirstOrDefault(c => c.VBiACClassID == acClassID && c.KeyACUrl == MaterialTempertureConfigKeyACUrl);
                        if (materialConfig == null)
                        {
                            materialConfig = MaterialConfig.NewACObject(dbApp, mt);
                            materialConfig.VBiACClassID = acClassID;
                            materialConfig.KeyACUrl = MaterialTempertureConfigKeyACUrl;
                            materialConfig.SetValueTypeACClass(dbApp.ContextIPlus.GetACType("double"));

                            mt.MaterialConfig_Material.Add(materialConfig);
                            dbApp.MaterialConfig.Add(materialConfig);
                        }

                        materialConfig.Value = material.AverageTemperatureWithOffset;
                    }
                }

                Msg msg = dbApp.ACSaveChanges();
                if (msg != null)
                {
                    if (IsAlarmActive(ServiceAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ServiceAlarm, msg);
                        Root.Messages.LogMessageMsg(msg);
                    }
                }
            }
        }

        internal void SetTempServiceInfo(short info)
        {
            using (ACMonitor.Lock(_30010_LockTempServiceInfo))
            {
                TemperatureServiceInfo.ValueT = info;
            }
        }

        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;

            switch (acMethodName)
            {
                case nameof(GetTemperaturesInfo):
                    result = GetTemperaturesInfo((Guid)acParameter[0]);
                    return true;
                case nameof(GetAverageTemperatures):
                    result = GetAverageTemperatures((Guid)acParameter[0]);
                    return true;
                case nameof(GetWaterMaterialNo):
                    result = GetWaterMaterialNo((Guid)acParameter[0]);
                    return true;
                case nameof(InitService):
                    InitService();
                    return true;
                default:
                    break;
            }
            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        #endregion
    }

    public enum WaterType : short
    {
        NotWater = 0,
        ColdWater = 10,
        CityWater = 20,
        WarmWater = 30,
        DryIce = 40
    }

    
}
