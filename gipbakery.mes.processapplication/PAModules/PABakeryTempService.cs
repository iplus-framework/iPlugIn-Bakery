using gip.core.autocomponent;
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

        public override bool ACPostInit()
        {
            bool result = base.ACPostInit();
            if (!result)
                return result;

            Root.PropertyChanged += Root_PropertyChanged;

            return result;
        }

        private void Root_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "InitState" && Root.InitState == ACInitState.Initialized)
            {
                InitializeService();
                Root.PropertyChanged -= Root_PropertyChanged;
            }
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            Root.PropertyChanged -= Root_PropertyChanged;
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

        #endregion

        #region Methods
        [ACMethodInteraction("", "", 700, true)]
        public void InitService()
        {
            InitializeService();
            RecalculateAverageTemperature();
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
        public ACValueList GetAverageTemperatures(Guid receivingPointID)
        {
            KeyValuePair<BakeryReceivingPoint, BakeryRecvPointTemperature>? cacheItem = null;

            using (ACMonitor.Lock(_20015_LockValue))
            {
                cacheItem = Temperatures?.FirstOrDefault(c => c.Key.ComponentClass.ACClassID == receivingPointID);
            }

            if (cacheItem == null)
                return null;

            return new ACValueList(cacheItem.Value.Value.MaterialTempInfos.Where(t => !string.IsNullOrEmpty(t.MaterialNo)).Select(c => new ACValue(c.MaterialNo, c.AverageTemperature)).ToArray());
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

            List<BakerySilo> silosWithBakeryPTC = new List<BakerySilo>();
            Temperatures = new Dictionary<BakeryReceivingPoint, BakeryRecvPointTemperature>();

            var projects = Root.ACComponentChilds.Where(c => c.ComponentClass.ACProject.ACProjectTypeIndex == (short)Global.ACProjectTypes.Application
                                                          && c.ACIdentifier != "DataAccess");

            List<BakeryReceivingPoint> receivingPoints = new List<BakeryReceivingPoint>();

            // Temperatures from PAMSilo

            foreach (var project in projects)
            {
                var possibleSilos = project.FindChildComponents<BakerySilo>(c => c.ACComponentChilds.Any(x => x is PAEBakeryThermometer));

                foreach (var possibleSilo in possibleSilos)
                {
                    if (possibleSilo.Thermometers.Any(c => !c.DisabledForTempCalculation))
                    {
                        silosWithBakeryPTC.Add(possibleSilo);
                        possibleSilo.MaterialNo.PropertyChanged += MaterialNo_PropertyChanged;
                    }
                }

                receivingPoints.AddRange(project.FindChildComponents<BakeryReceivingPoint>(c => c is BakeryReceivingPoint));
            }

            using (Database db = new gip.core.datamodel.Database())
            using (gip.mes.datamodel.DatabaseApp dbApp = new gip.mes.datamodel.DatabaseApp(db))
            {
                foreach (var receivingPoint in receivingPoints)
                {
                    BakeryRecvPointTemperature tempInfo = new BakeryRecvPointTemperature(this);

                    foreach (var silo in silosWithBakeryPTC)
                    {
                        RoutingResult routingResult = ACRoutingService.SelectRoutes(RoutingService, db, false, silo.ComponentClass, receivingPoint.ComponentClass,
                                                                                    RouteDirections.Forwards, "", null, null, null, 1, true, true);

                        if (routingResult != null && routingResult.Routes != null && routingResult.Routes.Any())
                        {
                            tempInfo.AddSilo(silo);
                        }
                    }

                    Temperatures.Add(receivingPoint, tempInfo);
                }

                // Temperatures for water/room

                foreach (var cacheItem in Temperatures)
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
        }

        private void MaterialNo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                SetTempServiceInfo(0);
                foreach (var bakeryTempItem in Temperatures.Values)
                {
                    bakeryTempItem.SiloMaterialNoPropertyChanged(sender, e);
                }
                SetTempServiceInfo(2);
            }
        }

        private void DeinitCache()
        {
            if (Temperatures == null)
                return;

            foreach(var cacheItem in Temperatures.Values)
            {
                cacheItem.DeInit();
            }
        }

        private void InitializeWaterSensor(KeyValuePair<BakeryReceivingPoint, BakeryRecvPointTemperature> cacheItem, PAPoint paPointMatIn, DatabaseApp dbApp, WaterType wType)
        {
            OnInitializeWaterSensor(cacheItem, paPointMatIn, dbApp, wType);

            RoutingResult rr = ACRoutingService.FindSuccessorsFromPoint(RoutingService, dbApp.ContextIPlus, false, cacheItem.Key.ComponentClass,
                                                            paPointMatIn.PropertyInfo, PAMTank.SelRuleID_Silo, RouteDirections.Backwards,
                                                            null, null, null, 1, true, true);

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
                        }

                        PAFDosing dosing = paPointMatIn.ConnectionList.Where(c => c.TargetParentComponent is PAFDosing)
                                                                                     .FirstOrDefault()?.TargetParentComponent as PAFDosing;
                        if (dosing == null)
                        {
                            //Error50424: The dosing function for {0} can not be found at {1}.
                            Msg msg = new Msg(this, eMsgLevel.Error, ClassName, "InitializeWaterSensor(20)", 285, "Error50424", wType.ToString(), cacheItem.Key.ComponentClass.ACUrlComponent);
                            //if (IsAlarmActive(ServiceAlarm, msg.Message) == null)
                            //{
                                //OnNewAlarmOccurred(ServiceAlarm, msg);
                                Root.Messages.LogMessageMsg(msg);
                            //}
                        }
                        else
                        {
                            if (dosing.CurrentScaleForWeighing == null)
                            {
                                //Error50425: Can not found the CurrentScaleForWeighing at the dosing function for the {0}.
                                Msg msg = new Msg(this, eMsgLevel.Error, ClassName, "InitializeWaterSensor(30)", 297, "Error50425", wType.ToString());
                                if (IsAlarmActive(ServiceAlarm, msg.Message) == null)
                                {
                                    OnNewAlarmOccurred(ServiceAlarm, msg);
                                    Root.Messages.LogMessageMsg(msg);
                                }
                            }

                            PAEBakeryThermometer thermometer = dosing.CurrentScaleForWeighing.FindChildComponents<PAEBakeryThermometer>(c => c is PAEBakeryThermometer, null, 1)
                                                                                             .FirstOrDefault();

                            if (thermometer == null || thermometer.DisabledForTempCalculation)
                            {
                                //Error50426: Can not found the PAEBakeryThermometer under CurrentScaleForWeighing for the {0}.
                                Msg msg = new Msg(this, eMsgLevel.Error, ClassName, "InitializeWaterSensor(40)", 309, "Error50426", wType.ToString());
                                if (IsAlarmActive(ServiceAlarm, msg.Message) == null)
                                {
                                    OnNewAlarmOccurred(ServiceAlarm, msg);
                                    Root.Messages.LogMessageMsg(msg);
                                }
                            }

                            var materialTempInfo = cacheItem.Value.MaterialTempInfos.FirstOrDefault(c => c.MaterialNo == materialNo);
                            if (materialTempInfo == null)
                            {
                                materialTempInfo = new MaterialTemperature();
                                materialTempInfo.MaterialNo = materialNo;
                                materialTempInfo.Material = dbApp.Material.FirstOrDefault(c => c.MaterialNo == materialNo);
                                materialTempInfo.WaterMinDosingQuantity = dosing.CurrentScaleForWeighing.MinDosingWeight.ValueT;
                                materialTempInfo.WaterDefaultTemperature = thermometer.TemperatureDefault;
                                materialTempInfo.Water = wType;
                                cacheItem.Value.MaterialTempInfos.Add(materialTempInfo);
                            }

                            if (thermometer != null)
                                materialTempInfo.AddThermometer(thermometer);
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
            if (Temperatures == null)
                return;

            SetTempServiceInfo(0);

            KeyValuePair<BakeryReceivingPoint, BakeryRecvPointTemperature>[] recalcItems = null;
            using (ACMonitor.Lock(_20015_LockValue))
            {
                recalcItems = Temperatures?.ToArray();
            }

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
                            dbApp.MaterialConfig.AddObject(materialConfig);
                        }

                        materialConfig.Value = material.AverageTemperature;
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
