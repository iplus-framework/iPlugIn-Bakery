using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
using gip.mes.processapplication;
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
    public class PABakeryTempService : PAClassAlarmingBase
    {
        #region c'tors

        public PABakeryTempService(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
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

            return result;
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            return base.ACDeInit(deleteACClassTask);
        }

        #endregion

        #region Properties

        [ACPropertyInfo(true, 9999)]
        public TimeSpan RecalculationTime
        {
            get;
            set;
        }

        public List<BakerySilo> SilosWithBakeryPTC
        {
            get;
            set;
        }

        public Dictionary<BakeryReceivingPoint, BakeryRecvPointTemperature> Cache
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
        }

        public BakeryRecvPointTemperature GetBakeryTemperatures()
        {
            return null;
        }

        /// <summary>
        /// Returns info(material, silos, average temp) for BSOTemperature
        /// </summary>
        /// <param name="receivingPointID"></param>
        [ACMethodInfo("","",9999)]
        public ACValueList GetTemperaturesInfo(Guid receivingPointID)
        {
            if (Cache == null)
                return null;

            KeyValuePair<BakeryReceivingPoint, BakeryRecvPointTemperature>? cacheItem = Cache.FirstOrDefault(c => c.Key.ComponentClass.ACClassID == receivingPointID);
            if (cacheItem == null)
                return null;

            return new ACValueList(cacheItem.Value.Value.MaterialTempInfos.Select(c => new ACValue(c.MaterialNo, c)).ToArray());
        }

        //TODO Deinit service (event handlers)
        private void InitializeService()
        {
            SilosWithBakeryPTC = new List<BakerySilo>();
            Cache = new Dictionary<BakeryReceivingPoint, BakeryRecvPointTemperature>();

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
                        SilosWithBakeryPTC.Add(possibleSilo);
                }

                receivingPoints.AddRange(project.FindChildComponents<BakeryReceivingPoint>(c => c is BakeryReceivingPoint));
            }

            using (Database db = new gip.core.datamodel.Database())
            {
                foreach (var receivingPoint in receivingPoints)
                {
                    BakeryRecvPointTemperature tempInfo = new BakeryRecvPointTemperature();

                    foreach (var silo in SilosWithBakeryPTC)
                    {
                        RoutingResult routingResult = ACRoutingService.SelectRoutes(RoutingService, db, false, silo.ComponentClass, receivingPoint.ComponentClass,
                                                                                    RouteDirections.Forwards, "", null, null, null, 1, true, true);

                        if (routingResult != null && routingResult.Routes != null && routingResult.Routes.Any())
                        {
                            tempInfo.AddSilo(silo);
                        }
                    }

                    Cache.Add(receivingPoint, tempInfo);

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
                            if (!Cache.TryGetValue(configReceivingPoint, out tempInfo))
                            {
                                tempInfo = new BakeryRecvPointTemperature();
                                Cache.Add(configReceivingPoint, tempInfo);
                            }

                            MaterialTemperature materialInfo = tempInfo.MaterialTempInfos.FirstOrDefault(c => c.MaterialNo == config.MaterialNo);
                            if (materialInfo == null)
                            {
                                materialInfo = new MaterialTemperature();
                                materialInfo.MaterialNo = config.MaterialNo;
                            }

                            materialInfo.AddThermometer(bakeryThermometer);

                        }
                        else if (string.IsNullOrEmpty(config.ReceivingPointACUrl))
                        {
                            //for all receiving points

                            foreach (BakeryRecvPointTemperature tempInfo in Cache.Values)
                            {
                                MaterialTemperature materialInfo = tempInfo.MaterialTempInfos.FirstOrDefault(c => c.MaterialNo == config.MaterialNo);
                                if (materialInfo == null)
                                {
                                    materialInfo = new MaterialTemperature();
                                    materialInfo.MaterialNo = config.MaterialNo;
                                }

                                materialInfo.AddThermometer(bakeryThermometer);
                            }
                        }
                    }
                }

                // Temperatures for water

                foreach (var cacheItem in Cache)
                {
                    //Cold water
                    InitializeWaterSensor(cacheItem, cacheItem.Key.PAPointMatIn2.PropertyInfo, db);

                    //City water
                    InitializeWaterSensor(cacheItem, cacheItem.Key.PAPointMatIn3.PropertyInfo, db);

                    //Warm water
                    InitializeWaterSensor(cacheItem, cacheItem.Key.PAPointMatIn4.PropertyInfo, db);
                }
            }
        }

        private void InitializeWaterSensor(KeyValuePair<BakeryReceivingPoint, BakeryRecvPointTemperature> cacheItem, ACClassProperty paPointMatIn, Database db)
        {
            RoutingResult rr = ACRoutingService.FindSuccessorsFromPoint(RoutingService, db, false, cacheItem.Key.ComponentClass,
                                                            paPointMatIn, PAMTank.SelRuleID_Silo, RouteDirections.Backwards,
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

                        PAFDosing dosing = cacheItem.Key.PAPointMatIn2.ConnectionList.Where(c => c.TargetParentComponent is PAFDosing)
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
                            cacheItem.Value.MaterialTempInfos.Add(materialTempInfo);
                        }

                        if (thermometer != null)
                            materialTempInfo.AddThermometer(thermometer);
                    }
                }
            }
        }


        #endregion
    }

    public class BakeryRecvPointTemperature
    {
        public BakeryRecvPointTemperature()
        {
            MaterialTempInfos = new List<MaterialTemperature>();
        }

        public void AddSilo(BakerySilo silo)
        {
            if (silo == null || silo.MaterialNo == null)
                return;

            silo.MaterialNo.PropertyChanged += SiloMaterialNoPropertyChanged;

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
                    targetMaterialInfo = new MaterialTemperature();
                    targetMaterialInfo.MaterialNo = materialNo;
                    MaterialTempInfos.Add(targetMaterialInfo);
                }

                targetMaterialInfo.AddSilo(silo);
            }
        }

        private void SiloMaterialNoPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {

            }
        }

        public List<MaterialTemperature> MaterialTempInfos
        {
            get;
            set;
        }

    }

    [DataContract]
    [ACSerializeableInfo()]
    [ACClassInfo(Const.PackName_VarioSystem, "en{'Material temperature'}de{'Material temperature'}", Global.ACKinds.TACSimpleClass)]
    public class MaterialTemperature
    {
        public MaterialTemperature()
        {
            SilosWithPTC = new List<BakerySilo>();
            BakeryThermometers = new List<PAEBakeryThermometer>();
            BakeryThermometersACUrls = new List<string>();
        }

        [DataMember]
        public string MaterialNo
        {
            get;
            set;
        }

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
        public double AverageTemperature
        {
            get
            {
                var temp = GetBakeryThermometers();
                if (temp.Any())
                {
                    return temp.Sum(c => c.ActualValue.ValueT) / temp.Count();
                }
                return 0.0;
            }
        }

        [DataMember]
        public List<string> BakeryThermometersACUrls
        {
            get;
            set;
        }

        public void AddSilo(BakerySilo bakerySilo)
        {
            SilosWithPTC.Add(bakerySilo);
            BakeryThermometersACUrls.AddRange(bakerySilo.Thermometers.Where(c => !c.DisabledForTempCalculation).Select(t => t.ACUrl));
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

        
    }

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
}
