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
    //Todo: move config to material
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

        public const string MN_GetTemperaturesInfo = "GetTemperaturesInfo";
        public const string MN_GetAverageTemperatures = "GetAverageTemperatures";
        public const string PN_TemperatureServiceInfo = "TemperatureServiceInfo";

        public const string MaterialTempertureConfigKeyACUrl = "MaterialTempConfig";
        public const string PN_CyclicMeasurement = "CyclicMeasurement";

        #endregion

        #region Properties

        private bool _RecalculationComplete = false;

        public Dictionary<BakeryReceivingPoint, BakeryRecvPointTemperature> Temperatures
        {
            get;
            set;
        }

        // 1 Refresh average temperatures
        // 2 Refresh temperatures info
        [ACPropertyBindingSource]
        public IACContainerTNet<short> TemperatureServiceInfo
        {
            get;
            set;
        }

        [ACPropertyPointProperty(9999, "", typeof(BakeryTempConfig))]
        public IEnumerable<BakeryTempConfig> BakeryServiceConfiguration
        {
            get
            {
                //TODO: Check is lock needed for Component class and properties
                try
                {
                    var query = ComponentClass.Properties.Where(c => (c.ObjectType == typeof(BakeryTempConfig))
                                                                    && (c.ACKind == Global.ACKinds.PSPropertyExt))
                                                            .OrderByDescending(x => x.UpdateDate)
                                                            .Select(c => c.ConfigValue as BakeryTempConfig)
                                                            .ToArray();
                    return query;
                }
                catch (Exception e)
                {
                    string msg = e.Message;
                    if (e.InnerException != null && e.InnerException.Message != null)
                        msg += " Inner:" + e.InnerException.Message;

                    if (gip.core.datamodel.Database.Root != null && gip.core.datamodel.Database.Root.Messages != null && gip.core.datamodel.Database.Root.InitState == ACInitState.Initialized)
                        gip.core.datamodel.Database.Root.Messages.LogException("BakeryTempService", "BakeryServiceConfiguration", msg);
                }

                return null;
            }
        }

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
            if (Temperatures == null || !_RecalculationComplete)
                return null;

            KeyValuePair<BakeryReceivingPoint, BakeryRecvPointTemperature>? cacheItem = Temperatures.FirstOrDefault(c => c.Key.ComponentClass.ACClassID == receivingPointID);
            if (cacheItem == null)
                return null;

            return new ACValueList(cacheItem.Value.Value.MaterialTempInfos.Where(t => !string.IsNullOrEmpty(t.MaterialNo)).Select(c => new ACValue(c.MaterialNo, c)).ToArray());
        }

        [ACMethodInfo("", "", 9999)]
        public ACValueList GetAverageTemperatures(Guid receivingPointID)
        {
            if (Temperatures == null)
                return null;

            KeyValuePair<BakeryReceivingPoint, BakeryRecvPointTemperature>? cacheItem = Temperatures.FirstOrDefault(c => c.Key.ComponentClass.ACClassID == receivingPointID);
            if (cacheItem == null)
                return null;

            return new ACValueList(cacheItem.Value.Value.MaterialTempInfos.Where(t => !string.IsNullOrEmpty(t.MaterialNo)).Select(c => new ACValue(c.MaterialNo, c.AverageTemperature)).ToArray());
        }

        public void GetDefaultWaterTemperatures(Guid receivingPointID)
        {

        }

        //TODO Deinit service (event handlers)
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


                // Temperatures from configuration

                var bakeryTempConfig = BakeryServiceConfiguration;
                if (bakeryTempConfig != null)
                {
                    var bakeryTempConfigs = bakeryTempConfig.ToArray();

                    foreach (BakeryTempConfig config in bakeryTempConfigs)
                    {
                        if (string.IsNullOrEmpty(config.BakeryThermometerACUrl) || string.IsNullOrEmpty(config.MaterialNo))
                            continue;

                        PAEBakeryThermometer bakeryThermometer = Root.ACUrlCommand(config.BakeryThermometerACUrl, null) as PAEBakeryThermometer;
                        if (bakeryThermometer == null)
                            continue;

                        BakeryReceivingPoint configReceivingPoint = receivingPoints.FirstOrDefault(c => c.ACUrl == config.ReceivingPointACUrl);
                        if (configReceivingPoint != null)
                        {
                            //specific for receiving point

                            BakeryRecvPointTemperature tempInfo = null;
                            if (!Temperatures.TryGetValue(configReceivingPoint, out tempInfo))
                            {
                                tempInfo = new BakeryRecvPointTemperature(this);
                                Temperatures.Add(configReceivingPoint, tempInfo);
                            }

                            MaterialTemperature materialInfo = tempInfo.MaterialTempInfos.FirstOrDefault(c => c.MaterialNo == config.MaterialNo);
                            if (materialInfo == null)
                            {
                                materialInfo = new MaterialTemperature();
                                materialInfo.MaterialNo = config.MaterialNo;
                                materialInfo.Material = dbApp.Material.FirstOrDefault(c => c.MaterialNo == config.MaterialNo);
                                materialInfo.Water = WaterType.NotWater;
                                tempInfo.MaterialTempInfos.Add(materialInfo);
                            }

                            materialInfo.AddThermometer(bakeryThermometer);

                        }
                        else if (string.IsNullOrEmpty(config.ReceivingPointACUrl))
                        {
                            //for all receiving points

                            foreach (BakeryRecvPointTemperature tempInfo in Temperatures.Values)
                            {
                                MaterialTemperature materialInfo = tempInfo.MaterialTempInfos.FirstOrDefault(c => c.MaterialNo == config.MaterialNo);
                                if (materialInfo == null)
                                {
                                    materialInfo = new MaterialTemperature();
                                    materialInfo.MaterialNo = config.MaterialNo;
                                    materialInfo.Material = dbApp.Material.FirstOrDefault(c => c.MaterialNo == config.MaterialNo);
                                    materialInfo.Water = WaterType.NotWater;
                                    tempInfo.MaterialTempInfos.Add(materialInfo);
                                }

                                materialInfo.AddThermometer(bakeryThermometer);
                            }
                        }
                    }
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
                TemperatureServiceInfo.ValueT = 0;
                foreach (var bakeryTempItem in Temperatures.Values)
                {
                    bakeryTempItem.SiloMaterialNoPropertyChanged(sender, e);
                }
                TemperatureServiceInfo.ValueT = 2;
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

        private void InitializeWaterSensor(KeyValuePair<BakeryReceivingPoint, BakeryRecvPointTemperature> cacheItem, PAPoint paPointMatIn, gip.mes.datamodel.DatabaseApp dbApp, WaterType wType)
        {
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
                            //TODO: error
                        }

                        PAFDosing dosing = paPointMatIn.ConnectionList.Where(c => c.TargetParentComponent is PAFDosing)
                                                                                     .FirstOrDefault()?.TargetParentComponent as PAFDosing;
                        if (dosing == null)
                        {
                            //TODO: error
                        }

                        if (dosing.CurrentScaleForWeighing == null)
                        {
                            //TODO: error
                        }

                        PAEBakeryThermometer thermometer = dosing.CurrentScaleForWeighing.FindChildComponents<PAEBakeryThermometer>(c => c is PAEBakeryThermometer, null, 1)
                                                                                         .FirstOrDefault();

                        if (thermometer == null || thermometer.DisabledForTempCalculation)
                        {
                            //TODO: error
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

        private void RecalculateAverageTemperature()
        {
            if (Temperatures == null)
                return;

            _RecalculationComplete = false;

            TemperatureServiceInfo.ValueT = 0;

            foreach (var recvPoint in Temperatures)
            {
                recvPoint.Value.RecalculateAverageTemperature(recvPoint.Key);
            }

            TemperatureServiceInfo.ValueT = 1;

            _RecalculationComplete = true;

            WriteAverageTemperatureToMaterialConfig();
        }

        private void WriteAverageTemperatureToMaterialConfig()
        {
            var temperatures = Temperatures.ToArray();

            using (gip.mes.datamodel.DatabaseApp dbApp = new gip.mes.datamodel.DatabaseApp())
            {
                foreach (var temp in temperatures)
                {
                    Guid acClassID = temp.Key.ComponentClass.ACClassID;

                    foreach (var material in temp.Value.MaterialTempInfos)
                    {
                        if (material.Material == null)
                            continue;

                        Material mt = material.Material.FromAppContext<Material>(dbApp);
                        //TODO nullcheck
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

                var msg = dbApp.ACSaveChanges();

            }
        }

        //private void BuildTempCyclicMeasurement(DatabaseApp dbApp)
        //{
        //    var materialConfisWithTempMeasurement = dbApp.MaterialConfig.Where(c => c.KeyACUrl == MaterialTempertureConfigKeyACUrl && c.Expression != null);

        //    foreach (MaterialConfig matConfig in materialConfisWithTempMeasurement)
        //    {
        //        if (IsTempMeasurementConfigured(matConfig.Material))
        //            continue;

        //        matConfig.Expression = null;
        //    }

        //    Msg msg = dbApp.ACSaveChanges();

        //    //TODO:improve this, include config
        //    var materials = dbApp.Material.ToArray().Where(c => c.ACProperties != null && c.ACProperties.GetOrCreateACPropertyExtByName("CyclicMeasurement", false).Value != null);
        //    //todo lock
        //    var recvPoints = Temperatures.Select(c => c.Key).ToArray();

        //    foreach (var recvPoint in recvPoints)
        //    {
        //        foreach (var material in materials)
        //        {
        //            if (!IsTempMeasurementConfigured(material))
        //                continue;

        //            MaterialConfig matTempConfig = material.MaterialConfig_Material.FirstOrDefault(c => c.KeyACUrl == MaterialTempertureConfigKeyACUrl 
        //                                                                                             && c.VBiACClassID == recvPoint.ComponentClass.ACClassID );
        //            if (matTempConfig == null)
        //            {
        //                matTempConfig = MaterialConfig.NewACObject(dbApp, material);
        //                matTempConfig.VBiACClassID = recvPoint.ComponentClass.ACClassID;
        //                matTempConfig.KeyACUrl = MaterialTempertureConfigKeyACUrl;
        //                matTempConfig.SetValueTypeACClass(dbApp.ContextIPlus.GetACType("double"));

        //                material.MaterialConfig_Material.Add(matTempConfig);
        //                dbApp.MaterialConfig.AddObject(matTempConfig);
        //            }

        //            if (matTempConfig.Expression != null)
        //                continue;

        //            matTempConfig.Expression = TempMeasurementModeEnum.MatOn.ToString();
        //        }
        //    }

        //    var msg1 = dbApp.ACSaveChanges(); 
        //}

        //public static bool IsTempMeasurementConfigured(Material material)
        //{
        //    var prop = material.ACProperties?.GetOrCreateACPropertyExtByName("CyclicMeasurement", false);
        //    if (prop == null)
        //        return false;

        //    TimeSpan? ts = prop.Value as TimeSpan?;
        //    if (!ts.HasValue)
        //        return false;

        //    if (ts.Value.TotalMinutes > 1)
        //        return true;

        //    return false;
        //}

        #endregion
    }

    #region Model

    public class BakeryRecvPointTemperature
    {
        public BakeryRecvPointTemperature(PABakeryTempService tempService)
        {
            MaterialTempInfos = new List<MaterialTemperature>();
            TemperatureService = tempService;
        }

        public PABakeryTempService TemperatureService
        {
            get;
            set;
        }

        public void AddSilo(BakerySilo silo)
        {
            if (silo == null || silo.MaterialNo == null)
                return;

            var materialNo = silo.MaterialNo.ValueT;
            if (string.IsNullOrEmpty(materialNo))
            {
                var silosWithoutMaterial = MaterialTempInfos.FirstOrDefault(c => string.IsNullOrEmpty(c.MaterialNo));
                if (silosWithoutMaterial == null)
                {
                    silosWithoutMaterial = new MaterialTemperature();
                    MaterialTempInfos.Add(silosWithoutMaterial);
                }

                silosWithoutMaterial.AddSilo(silo);
            }
            else
            {
                var targetMaterialInfo = MaterialTempInfos.FirstOrDefault(c => c.MaterialNo == materialNo);
                if (targetMaterialInfo == null)
                {
                    using (gip.mes.datamodel.DatabaseApp dbApp = new gip.mes.datamodel.DatabaseApp())
                    {
                        targetMaterialInfo = new MaterialTemperature();
                        targetMaterialInfo.MaterialNo = materialNo;
                        targetMaterialInfo.Material = dbApp.Material.Include(x => x.MaterialConfig_Material).FirstOrDefault(c => c.MaterialNo == materialNo);
                        targetMaterialInfo.Water = WaterType.NotWater;
                        MaterialTempInfos.Add(targetMaterialInfo);
                    }
                }

                targetMaterialInfo.AddSilo(silo);
            }
        }

        internal void SiloMaterialNoPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                ACPropertyNet<string> materialNoProp = sender as ACPropertyNet<string>;
                if (materialNoProp != null)
                {
                    TemperatureService.TemperatureServiceInfo.ValueT = 0;

                    var silo = materialNoProp.ParentACComponent as BakerySilo;
                    if (silo != null)
                    {
                        var affectedMaterialTemperatures = MaterialTempInfos.Where(c => c.SilosWithPTC.Contains(silo)).ToList();

                        foreach (var item in affectedMaterialTemperatures)
                        {
                            string materialNo = silo.MaterialNo.ValueT;

                            if (item.MaterialNo == materialNo)
                                continue;

                            item.RemoveSilo(silo);

                            if (!item.BakeryThermometersACUrls.Any())
                            {
                                MaterialTempInfos.Remove(item);
                            }

                            var mt = MaterialTempInfos.FirstOrDefault(c => c.MaterialNo == materialNo);
                            if (mt == null)
                            {
                                using (gip.mes.datamodel.DatabaseApp dbApp = new gip.mes.datamodel.DatabaseApp())
                                {
                                    mt = new MaterialTemperature();
                                    mt.MaterialNo = materialNo;
                                    mt.Material = dbApp.Material.Include(c => c.MaterialConfig_Material).FirstOrDefault(c => c.MaterialNo == materialNo);
                                    mt.Water = WaterType.NotWater;
                                    MaterialTempInfos.Add(mt);
                                }
                            }

                            mt.AddSilo(silo);
                        }
                    }
                }
            }
        }

        public List<MaterialTemperature> MaterialTempInfos
        {
            get;
            set;
        }

        public void RecalculateAverageTemperature(BakeryReceivingPoint recvPoint)
        {
            if (MaterialTempInfos == null)
                return;

            foreach (MaterialTemperature mt in MaterialTempInfos)
            {
                if (mt.IsRoomTemperature)
                {
                    mt.AverageTemperature = recvPoint.RoomTemperature.ValueT;
                }
                else
                {
                    mt.AverageTemperature = mt.AverageTemperatureCalc;
                }
            }
        }

        public void AddRoomTemperature(BakeryReceivingPoint recvPoint)
        {
            MaterialTemperature mt = new MaterialTemperature();
            mt.IsRoomTemperature = true;
            mt.Water = WaterType.NotWater;
            mt.MaterialNo = TemperatureService.Root.Environment.TranslateText(TemperatureService, "RoomTemp");
            mt.AverageTemperature = recvPoint.RoomTemperature.ValueT;
            MaterialTempInfos.Add(mt);
        }

        public void DeInit()
        {
            foreach(var mt in MaterialTempInfos)
            {
                foreach(var silo in mt.SilosWithPTC)
                {
                    silo.MaterialNo.PropertyChanged -= SiloMaterialNoPropertyChanged;
                }
            }
        }
    }

    [DataContract]
    [ACSerializeableInfo()]
    [ACClassInfo(Const.PackName_VarioSystem, "en{'Material temperature'}de{'Material temperature'}", Global.ACKinds.TACSimpleClass)]
    public class MaterialTemperature : IACObject, INotifyPropertyChanged
    {
        public MaterialTemperature()
        {
            SilosWithPTC = new List<BakerySilo>();
            BakeryThermometers = new List<PAEBakeryThermometer>();
            BakeryThermometersACUrls = new List<string>();
        }

        [IgnoreDataMember]
        public bool IsRoomTemperature
        {
            get;
            set;
        }

        [DataMember]
        [ACPropertyInfo(9999)]
        public string MaterialNo
        {
            get;
            set;
        }

        [IgnoreDataMember]
        [ACPropertyInfo(9999)]
        public gip.mes.datamodel.Material Material
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public List<BakerySilo> SilosWithPTC
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public List<PAEBakeryThermometer> BakeryThermometers
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public double AverageTemperatureCalc
        {
            get
            {
                var temp = GetBakeryThermometers();
                if (temp.Any())
                {
                    return temp.Sum(c => c.ActualValueForCalculation) / temp.Count();
                }
                return 0.0;
            }
        }

        private double? _AverageTemperature;
        [DataMember]
        [ACPropertyInfo(9999)]
        public double? AverageTemperature
        {
            get => _AverageTemperature;
            set
            {
                _AverageTemperature = value;
                OnPropertyChanged("AverageTemperature");
            }
        }

        [DataMember]
        public List<string> BakeryThermometersACUrls
        {
            get;
            set;
        }

        [IgnoreDataMember]
        [ACPropertyInfo(9999)]
        public List<Tuple<string,string,string>> BakeryThermometersInfo
        {
            get;
            set;
        }

        [DataMember]
        public WaterType Water
        {
            get;
            set;
        }

        [DataMember]
        public double? WaterMinDosingQuantity
        {
            get;
            set;
        }

        [DataMember]
        public double? WaterDefaultTemperature
        {
            get;
            set;
        }

        #region IACObject

        [IgnoreDataMember]
        public IACObject ParentACObject => null;

        [IgnoreDataMember]
        public IACType ACType => this.ReflectACType();

        [IgnoreDataMember]
        public IEnumerable<IACObject> ACContentList => this.ReflectGetACContentList();

        [IgnoreDataMember]
        public string ACIdentifier => this.ReflectGetACIdentifier();

        [IgnoreDataMember]
        public string ACCaption => this.ACIdentifier;

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddSilo(BakerySilo bakerySilo)
        {
            SilosWithPTC.Add(bakerySilo);
            BakeryThermometersACUrls.AddRange(bakerySilo.Thermometers.Where(c => !c.DisabledForTempCalculation).Select(t => t.ACUrl));
        }

        public void RemoveSilo(BakerySilo bakerySilo)
        {
            SilosWithPTC.Remove(bakerySilo);
            foreach (string ptc in bakerySilo.Thermometers.Where(c => !c.DisabledForTempCalculation).Select(t => t.ACUrl))
            {
                BakeryThermometersACUrls.Remove(ptc);
            }
        }

        public void AddThermometer(PAEBakeryThermometer thermometer)
        {
            BakeryThermometers.Add(thermometer);
            BakeryThermometersACUrls.Add(thermometer.ACUrl);
        }

        public IEnumerable<PAEBakeryThermometer> GetBakeryThermometers()
        {
            return BakeryThermometers.Concat(SilosWithPTC.SelectMany(c => c.Thermometers.Where(t => !t.DisabledForTempCalculation)));
        }

        public void BuildBakeryThermometersInfo(DatabaseApp dbApp)
        {
            Material = dbApp.Material.FirstOrDefault(c => c.MaterialNo == MaterialNo);
            BakeryThermometersInfo = new List<Tuple<string, string, string>>();

            foreach(string tACUrl in BakeryThermometersACUrls)
            {
                ACComponent tProxy = dbApp.Root().ACUrlCommand(tACUrl) as ACComponent;
                if (tProxy != null)
                {
                    string siloACCaption = null;

                    if (typeof(PAMSilo).IsAssignableFrom(tProxy.ParentACComponent.ComponentClass.ObjectType))
                    {
                        siloACCaption += tProxy.ParentACComponent.ACCaption;
                    }

                    BakeryThermometersInfo.Add(new Tuple<string, string, string>(siloACCaption, tProxy.ACCaption, tACUrl));
                }
            }
        }

        public object ACUrlCommand(string acUrl, params object[] acParameter)
        {
            return this.ReflectACUrlCommand(acUrl, acParameter);
        }

        public bool IsEnabledACUrlCommand(string acUrl, params object[] acParameter)
        {
            return this.IsEnabledACUrlCommand(acUrl, acParameter);
        }

        public string GetACUrl(IACObject rootACObject = null)
        {
            return this.ReflectGetACUrl(rootACObject);
        }

        public bool ACUrlBinding(string acUrl, ref IACType acTypeInfo, ref object source, ref string path, ref Global.ControlModes rightControlMode)
        {
            return this.ReflectACUrlBinding(acUrl, ref acTypeInfo, ref source, ref path, ref rightControlMode);
        }

        #endregion

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum WaterType : short
    {
        NotWater = 0,
        ColdWater = 10,
        CityWater = 20,
        WarmWater = 30,
        DryIce = 40
    }

    #endregion

    #region Config

    [DataContract]
    [ACSerializeableInfo]
    [ACClassInfo(Const.PackName_VarioSystem, "en{'Bakery temperature configuration'}de{'Bäckerei-Temperatur Konfiguration'}", Global.ACKinds.TACClass, Global.ACStorableTypes.NotStorable, true, false)]
    public class BakeryTempConfig : INotifyPropertyChanged
    {
        private string _MaterialNo;

        [DataMember(Name = "A")]
        [ACPropertyInfo(9999)]
        public string MaterialNo
        {
            get => _MaterialNo;
            set
            {
                _MaterialNo = value;
                OnPropertyChanged("MaterialNo");
            }
        }

        private string _ReceivingPointACUrl;

        [DataMember(Name = "B")]
        [ACPropertyInfo(9999)]
        public string ReceivingPointACUrl
        {
            get => _ReceivingPointACUrl;
            set
            {
                _ReceivingPointACUrl = value;
                OnPropertyChanged("ReceivingPointACUrl");
            }
        }

        private string _BakeryThermometerACUrl;

        [DataMember(Name = "C")]
        [ACPropertyInfo(9999)]
        public string BakeryThermometerACUrl
        {
            get => _BakeryThermometerACUrl;
            set
            {
                _BakeryThermometerACUrl = value;
                OnPropertyChanged("BakeryThermometerACUrl");
            }
        }

        public bool _UseOnlyThisThermometer;

        [DataMember(Name = "D")]
        [ACPropertyInfo(9999)]
        public bool UseOnlyThisThermometer
        {
            get => _UseOnlyThisThermometer;
            set
            {
                _UseOnlyThisThermometer = value;
                OnPropertyChanged("UseOnlyThisThermometer");
            }
        }

        #region INotifyPropertyChanged Member

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

    }

    #endregion
}
