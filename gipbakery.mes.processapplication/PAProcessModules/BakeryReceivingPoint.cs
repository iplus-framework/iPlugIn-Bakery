using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
using gip.mes.processapplication;
using gip.mes.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Receiving point'}de{'Abnahmestelle'}", Global.ACKinds.TPAProcessModule, Global.ACStorableTypes.Required, false, PWGroupVB.PWClassName, true)]
    public class BakeryReceivingPoint : PAMPlatformscale
    {
        #region c'tors

        public const string SelRuleID_RecvPoint = "BakeryReceivingPoint";

        static BakeryReceivingPoint()
        {
            RegisterExecuteHandler(typeof(BakeryReceivingPoint), HandleExecuteACMethod_BakeryReceivingPoint);
            ACRoutingService.RegisterSelectionQuery(SelRuleID_RecvPoint, (c, p) => c.Component.ValueT is BakeryReceivingPoint, null);
        }

        public BakeryReceivingPoint(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
            _PAPointMatIn2 = new PAPoint(this, "PAPointMatIn2");
            _PAPointMatIn3 = new PAPoint(this, "PAPointMatIn3");
            _PAPointMatIn4 = new PAPoint(this, "PAPointMatIn4");
            _PAPointMatIn5 = new PAPoint(this, "PAPointMatIn5");
            _PAPointMatIn6 = new PAPoint(this, "PAPointMatIn6");

            _RecvPointReadyScaleACUrl = new ACPropertyConfigValue<string>(this, "RecvPointReadyScaleACUrl", "");
            _WithCover = new ACPropertyConfigValue<bool>(this, "WithCover", false);
            _HoseDestination = new ACPropertyConfigValue<int>(this, "HoseDestination", 999);
        }

        public override bool ACInit(Global.ACStartTypes startChildMode = Global.ACStartTypes.Automatic)
        {
            if (!base.ACInit(startChildMode))
                return false;
            return true;
        }

        public override bool ACPostInit()
        {
            //IACPropertyNetTarget tProp = MeasureWaterOnNewBatch as IACPropertyNetTarget;
            //if (tProp != null && tProp.Source != null)
            //    tProp.ValueUpdatedOnReceival += MeasureWater_ValueUpdatedOnReceival;

            _ = RecvPointReadyScaleACUrl;
            _ = WithCover;
            _ = HoseDestination;
            return base.ACPostInit();
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            return base.ACDeInit(deleteACClassTask);
        }
        #endregion

        #region Properties 

        private ACPropertyConfigValue<string> _RecvPointReadyScaleACUrl;
        [ACPropertyConfig("en{'Scale ACUrl for PWBakeryRecvPointReady'}de{'Waage ACUrl für PWBakeryRecvPointReady'}")]
        public string RecvPointReadyScaleACUrl
        {
            get => _RecvPointReadyScaleACUrl.ValueT;
            set
            {
                _RecvPointReadyScaleACUrl.ValueT = value;
                OnPropertyChanged("RecvPointReadyScaleACUrl");
            }
        }

        private ACPropertyConfigValue<bool> _WithCover;
        [ACPropertyConfig("en{'Receiving point with cover'}de{'Abnahmestelle mit Abdeckung'}")]
        public bool WithCover
        {
            get => _WithCover.ValueT;
            set
            {
                _WithCover.ValueT = value;
                OnPropertyChanged("WithCover");
            }
        }

        private ACPropertyConfigValue<int> _HoseDestination;
        [ACPropertyConfig("en{'Discharging over hose, target destination ID'}de{'Entladung über Schlauch, Zielort-ID'}")]
        public int HoseDestination
        {
            get => _HoseDestination.ValueT;
            set
            {
                _HoseDestination.ValueT = value;
                OnPropertyChanged("HoseDestination");
            }
        }


        [ACPropertyBindingSource(IsPersistable = true)]
        public IACContainerTNet<double> DoughCorrTemp
        {
            get;
            set;
        }

        [ACPropertyBindingTarget(600, "", "en{'Receiving point cover down / Flour discharge'}de{'Empfangsstellenabdeckung unten / Mehl Ablassen'}", "", true)]
        public IACContainerTNet<bool> IsCoverDown
        {
            get;
            set;
        }

        [ACPropertyBindingTarget(600, "", "en{'Measure water temperature on new batch'}de{'Wassertemperaturen messen bei neuem Batch'}", "", false)]
        public IACContainerTNet<bool> MeasureWaterOnNewBatch
        {
            get;
            set;
        }

        /// <summary>
        /// Represents the room temperature. It can be fix defined or can be bounded to the sensor.
        /// </summary>
        [ACPropertyBindingTarget(IsPersistable = true)]
        public IACContainerTNet<double> RoomTemperature
        {
            get;
            set;
        }

        private bool _SearchTempService = true;
        private ACRef<ACComponent> _TemperatureService;

        public ACComponent TemperatureService
        {
            get
            {
                //TODO: temp service from acurl config

                if (_TemperatureService != null)
                    return _TemperatureService.ValueT;

                if (_SearchTempService)
                {
                    var serviceProj = Root.ACUrlCommand("\\Service") as ACComponent;
                    if (serviceProj != null)
                    {
                        ACComponent comp = serviceProj.FindChildComponents<PABakeryTempService>(c => c is PABakeryTempService).FirstOrDefault();
                        if (comp != null)
                        {
                            _TemperatureService = new ACRef<ACComponent>(comp, this);
                            return _TemperatureService.ValueT;
                        }
                    }

                    _SearchTempService = false;
                }
                return null;
            }
        }

        #endregion

        #region Methods 

        /// <summary>
        /// Get component(material) temperatures from the temperature service.
        /// </summary>
        /// <returns>Returns the list of ACValue(ACIdentifier = MaterialNo; Value = MaterialTemperature)</returns>
        [ACMethodInfo("","",800)]
        public ACValueList GetComponentTemperatures()
        {
            if (TemperatureService == null)
                return null;

            return TemperatureService.ExecuteMethod(PABakeryTempService.MN_GetTemperaturesInfo, ComponentClass.ACClassID) as ACValueList;
        }

        /// <summary>
        /// Get component(material) temperatures from the temperature service.
        /// </summary>
        /// <returns>Returns the list of ACValue(ACIdentifier = MaterialNo; Value = MaterialTemperature)</returns>
        [ACMethodInfo("", "", 800)]
        public ACValueList GetWaterComponentsFromTempService()
        {
            if (TemperatureService == null)
                return null;

            return TemperatureService.ExecuteMethod("GetWaterMaterialNo", ComponentClass.ACClassID) as ACValueList;
        }

        public PAEScaleBase GetRecvPointReadyScale()
        {
            PAEScaleBase scale = null;

            if (!string.IsNullOrEmpty(RecvPointReadyScaleACUrl))
            {
                scale = ACUrlCommand(RecvPointReadyScaleACUrl) as PAEScaleBase;
            }
            
            if (scale == null)
            {
                IPAMContScale scaleCont = this as IPAMContScale;
                if (scaleCont != null)
                    scale = scaleCont.Scale;
            }

            return scale;
        }

        [ACMethodInfo("", "", 9999)]
        public bool IsCoverDownPropertyBounded()
        {
            IACPropertyNetTarget targetProp = IsCoverDown as IACPropertyNetTarget;
            if (targetProp != null)
                return targetProp.Source != null;
            return false;
        }

        [ACMethodInfo("","",9999)]
        public bool IsTemperatureServiceInitialized()
        {
            if (TemperatureService == null)
                return false;

            bool? isEnabled = TemperatureService.ACUrlCommand("ServiceInitialized") as bool?;
            if (isEnabled.HasValue)
                return isEnabled.Value;

            return false;
        }

        public bool WaitIfMeasureWaterIsBound(PWBakeryWaitWaterM requester)
        {
            IACPropertyNetTarget tProp = MeasureWaterOnNewBatch as IACPropertyNetTarget;
            if (tProp == null || tProp.Source == null)
                return false;
            MeasureWaterOnNewBatch.ValueT = true;
            return true;
        }

        public override SingleDosingItems GetDosableComponents(bool withFacilityCombination = true)
        {
            using (Database db = new gip.core.datamodel.Database())
            {
                gip.core.datamodel.ACClass compClass = null;

                using (ACMonitor.Lock(gip.core.datamodel.Database.GlobalDatabase.QueryLock_1X000))
                {
                    compClass = ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(db);
                }

                RoutingResult routeResult = ACRoutingService.FindSuccessorsFromPoint(RoutingService, Database.ContextIPlus, false, compClass, PAPointMatIn1.PropertyInfo, PAMHopperscale.SelRuleID_Hopperscale, RouteDirections.Backwards,
                                                                     null, null, null, 0, true, true);

                RoutingResult rResult = ACRoutingService.FindSuccessors(RoutingService, db, false, compClass, PAMSilo.SelRuleID_SiloDirect, RouteDirections.Backwards,
                                                        null, null, null, 0, true, true);

                List<Route> routes = new List<Route>();

                if (rResult != null && rResult.Routes != null && rResult.Routes.Any())
                {
                    routes.AddRange(rResult.Routes);
                }

                if (routeResult != null && routeResult.Routes != null && routeResult.Routes.Any())
                {
                    RouteItem item = routeResult.Routes.FirstOrDefault()?.GetRouteSource();

                    if (item != null)
                    {
                        ACComponent sourceComp = item.SourceACComponent as ACComponent;

                        if (sourceComp != null)
                        {
                            RoutingResult rr = ACRoutingService.FindSuccessors(RoutingService, db, false, sourceComp, PAMSilo.SelRuleID_Silo, RouteDirections.Backwards,
                                                                null, null, null, 0, true, true);

                            if (rr != null && rr.Routes.Any())
                            {
                                routes.AddRange(rr.Routes);
                            }
                        }
                    }
                }

                if (!routes.Any())
                {
                    if (rResult == null)
                    {
                        return new SingleDosingItems() { Error = new Msg(eMsgLevel.Error, "Routing result is null!") };
                    }

                    if (rResult.Message != null && rResult.Message.MessageLevel == eMsgLevel.Error)
                    {
                        return new SingleDosingItems() { Error = rResult.Message };
                    }
                }

                SingleDosingItems result = new SingleDosingItems();

                foreach (Route route in routes)
                {
                    RouteItem rItem = route.GetRouteSource();
                    if (rItem == null)
                        continue;

                    PAMSilo silo = rItem.SourceACComponent as PAMSilo;
                    if (silo == null || !silo.OutwardEnabled.ValueT || string.IsNullOrEmpty(silo.Facility?.ValueT?.ValueT?.FacilityNo) || string.IsNullOrEmpty(silo.MaterialNo?.ValueT))
                        continue;

                    if (!withFacilityCombination && result.Any(c => c.MaterialNo == silo.MaterialNo.ValueT))
                        continue;

                    result.Add(new SingleDosingItem()
                    {
                        FacilityNo = withFacilityCombination ? silo.Facility.ValueT.ValueT.FacilityNo : "",
                        MaterialName = silo.MaterialName?.ValueT,
                        MaterialNo = silo.MaterialNo.ValueT
                    });
                }

                return result;
            }
        }

        //private void MeasureWater_ValueUpdatedOnReceival(object sender, ACPropertyChangedEventArgs e, ACPropertyChangedPhase phase)
        //{
        //    if (phase == ACPropertyChangedPhase.BeforeBroadcast)
        //        return;
        //    if (e.PropertyName == nameof(MeasureWaterOnNewBatch))
        //    {
        //        if (MeasureWaterOnNewBatch.ValueT == false)
        //        {
        //        }
        //    }
        //}

        #endregion

        #region Points
        PAPoint _PAPointMatIn2;
        /// <summary>
        /// City water Point
        /// </summary>
        /// <value>
        /// City water Point
        /// </value>
        [ACPropertyConnectionPoint(9999, "PointMaterial", "en{'ONLY for city water'}de{'NUR für Stadtwasser'}")]
        public PAPoint PAPointMatIn2
        {
            get
            {
                return _PAPointMatIn2;
            }
        }

        PAPoint _PAPointMatIn3;
        /// <summary>
        /// Cold water Point
        /// </summary>
        /// <value>
        /// Cold water Point
        /// </value>
        [ACPropertyConnectionPoint(9999, "PointMaterial", "en{'ONLY for Cold water'}de{'NUR für kaltes Wasser'}")]
        public PAPoint PAPointMatIn3
        {
            get
            {
                return _PAPointMatIn3;
            }
        }

        PAPoint _PAPointMatIn4;
        /// <summary>
        /// Warm water Point
        /// </summary>
        /// <value>
        /// Warm water Point
        /// </value>
        [ACPropertyConnectionPoint(9999, "PointMaterial", "en{'ONLY for warm water'}de{'NUR für Warmwasser'}")]
        public PAPoint PAPointMatIn4
        {
            get
            {
                return _PAPointMatIn4;
            }
        }

        PAPoint _PAPointMatIn5;
        /// <summary>
        /// Manual Weighing
        /// </summary>
        /// <value>
        /// Manual Weighing
        /// </value>
        [ACPropertyConnectionPoint(9999, "PointMaterial")]
        public PAPoint PAPointMatIn5
        {
            get
            {
                return _PAPointMatIn5;
            }
        }

        PAPoint _PAPointMatIn6;
        /// <summary>
        /// Other liquids
        /// </summary>
        /// <value>
        /// Other liquids
        /// </value>
        [ACPropertyConnectionPoint(9999, "PointMaterial")]
        public PAPoint PAPointMatIn6
        {
            get
            {
                return _PAPointMatIn6;
            }
        }

        #endregion

        #region Execute-Helper-Handlers
        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            switch (acMethodName)
            {
                case "GetComponentTemperatures":
                    result = GetComponentTemperatures();
                    return true;
                case "GetWaterComponentsFromTempService":
                    result = GetWaterComponentsFromTempService();
                    return true;
                case "IsCoverDownPropertyBounded":
                    result = IsCoverDownPropertyBounded();
                    return true;
                case "IsTemperatureServiceInitialized":
                    result = IsTemperatureServiceInitialized();
                    return true;
            }
            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        public static bool HandleExecuteACMethod_BakeryReceivingPoint(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PAMPlatformscale(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
        #endregion
    }
}
