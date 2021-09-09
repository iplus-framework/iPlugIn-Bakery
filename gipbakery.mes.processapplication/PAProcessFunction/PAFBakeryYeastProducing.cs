using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
using gip.mes.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Yeast'}de{'Hefe'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, true, "", BSOConfig = BakeryBSOYeastProducing.ClassName, SortIndex = 50)]
    public class PAFBakeryYeastProducing : PAFWorkCenterSelItemBase
    {
        #region c'tors

        public PAFBakeryYeastProducing(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
            _FermentationStarterScaleACUrl = new ACPropertyConfigValue<string>(this, "FermentationStarterScaleACUrl", "");
            _TemperatureSensorACUrl = new ACPropertyConfigValue<string>(this, "TemperatureSensorACUrl", "");
            _CleaningMode = new ACPropertyConfigValue<BakeryPreProdCleaningMode>(this, "CleaningMode", BakeryPreProdCleaningMode.OverBits);
            _ContinueProdACClassWF = new ACPropertyConfigValue<string>(this, "ContinueProdACClassWF", "");
            _PumpOverACClassWF = new ACPropertyConfigValue<string>(this, "PumpOverACClassWF", "");
            _CleaningProdACClassWF = new ACPropertyConfigValue<string>(this, "CleaningProdACClassWF", "");
            _PumpOverProcessModuleACUrl = new ACPropertyConfigValue<string>(this, "PumpOverProcessModuleACUrl", "");
        }

        public override bool ACPostInit()
        {
            bool result = base.ACPostInit();

            string temp = FermentationStarterScaleACUrl;
            temp = TemperatureSensorACUrl;
            temp = ContinueProdACClassWF;
            temp = PumpOverACClassWF;
            temp = CleaningProdACClassWF;
            temp = PumpOverProcessModuleACUrl;
            BakeryPreProdCleaningMode mode = CleaningMode;

            PAProcessModule module = ParentACComponent as PAProcessModule;
            if (module != null)
            {
                NeedWork.ValueT = !string.IsNullOrEmpty(module.OrderInfo.ValueT);
                module.OrderInfo.PropertyChanged += OrderInfo_PropertyChanged;
            }

            return result;
        }

        private void OrderInfo_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IACContainerTNet<string> senderProp = sender as IACContainerTNet<string>;
            if (senderProp != null)
            {
                NeedWork.ValueT = !string.IsNullOrEmpty(senderProp.ValueT);
            }
        }

        public override void SMIdle()
        {
            base.SMIdle();
            FindStores();
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            PAProcessModule module = ParentACComponent as PAProcessModule;
            if (module != null)
            {
                module.OrderInfo.PropertyChanged -= OrderInfo_PropertyChanged;
            }

            using (ACMonitor.Lock(_20015_LockValue))
            {
                VirtualSourceStore = null;
                VirtualTargetStore = null;
            }
            return base.ACDeInit(deleteACClassTask);
        }

        public const string MN_GetVirtualStoreACUrl = "GetVirtualStoreACUrl";
        public const string MN_Clean = "Clean";
        public const string MN_SwitchVirtualStoreOutwardEnabled = "SwitchVirtualStoreOutwardEnabled";
        public const string MN_GetPumpOverTargets = "GetPumpOverTargets";
        public const string MN_GetSourceVirtualStoreID = "GetSourceVirtualStoreID";

        public const string PN_CleaningMode = "CleaningMode";
        public const string PN_CleaningProdACClassWF = "CleaningProdACClassWF";
        public const string PN_FermentationStarterScaleACUrl = "FermentationStarterScaleACUrl";
        public const string PN_PumpOverProcessModuleACUrl = "PumpOverProcessModuleACUrl";

        #endregion

        #region Properties

        #region Properties => Configuration

        private ACPropertyConfigValue<string> _FermentationStarterScaleACUrl;
        [ACPropertyConfig("en{'Scale ACUrl for fermentation starter'}de{'Waage ACUrl für Anstellgut'}")]
        public string FermentationStarterScaleACUrl
        {
            get => _FermentationStarterScaleACUrl.ValueT;
            set
            {
                _FermentationStarterScaleACUrl.ValueT = value;
                OnPropertyChanged("FermentationStarterScaleACUrl");
            }
        }

        private ACPropertyConfigValue<string> _TemperatureSensorACUrl;
        [ACPropertyConfig("en{'Temperature sensor ACUrl for display in BSO'}de{'Temperatursensor ACUrl zur Anzeige in BSO '}")]
        public string TemperatureSensorACUrl
        {
            get => _TemperatureSensorACUrl.ValueT;
            set
            {
                _TemperatureSensorACUrl.ValueT = value;
                OnPropertyChanged("TemperatureSensorACUrl");
            }
        }

        private ACPropertyConfigValue<BakeryPreProdCleaningMode> _CleaningMode;
        [ACPropertyConfig("en{'Cleaning mode for pre production (sour, yeast...)'}de{'Reinigungsmodus für die Vorproduktion (sauer, Hefe...)'}")]
        public BakeryPreProdCleaningMode CleaningMode
        {
            get => _CleaningMode.ValueT;
            set
            {
                _CleaningMode.ValueT = value;
                OnPropertyChanged("CleaningMode");
            }
        }

        private ACPropertyConfigValue<string> _PumpOverACClassWF;
        [ACPropertyConfig("en{'Pump over planning ACClassWF'}de{'Umpumpen Planung ACClassWF'}")]
        public string PumpOverACClassWF
        {
            get => _PumpOverACClassWF.ValueT;
            set
            {
                _PumpOverACClassWF.ValueT = value;
                OnPropertyChanged("PumpOverACClassWF");
            }
        }

        private ACPropertyConfigValue<string> _ContinueProdACClassWF;
        [ACPropertyConfig("en{'Continue production planning ACClassWF'}de{'Produktion weiterführen Planung ACClassWF'}")]
        public string ContinueProdACClassWF
        {
            get => _ContinueProdACClassWF.ValueT;
            set
            {
                _ContinueProdACClassWF.ValueT = value;
                OnPropertyChanged("ContinueProdACClassWF");
            }
        }

        private ACPropertyConfigValue<string> _CleaningProdACClassWF;
        [ACPropertyConfig("en{'Cleaning planning ACClassWF'}de{'Reinigen Planung ACClassWF'}")]
        public string CleaningProdACClassWF
        {
            get => _CleaningProdACClassWF.ValueT;
            set
            {
                _CleaningProdACClassWF.ValueT = value;
                OnPropertyChanged("CleaningProdACClassWF");
            }
        }

        private ACPropertyConfigValue<string> _PumpOverProcessModuleACUrl;
        [ACPropertyConfig("en{'Pump over process module ACUrl'}de{'Umpumpen Prozessmodul ACUrl'}")]
        public string PumpOverProcessModuleACUrl
        {
            get => _PumpOverProcessModuleACUrl.ValueT;
            set
            {
                _PumpOverProcessModuleACUrl.ValueT = value;
                OnPropertyChanged("PumpOverProcessModuleACUrl");
            }
        }

        #endregion

        public PAMParkingspace VirtualSourceStore
        {
            get;
            set;
        }

        public PAMSilo VirtualTargetStore
        {
            get;
            set;
        }

        [ACPropertyBindingTarget(820, "", "en{'Pre production cleaning program'}de{'Reinigungsprogramm für die Vorproduktion'}")]
        public IACContainerTNet<short> CleaningProgram
        {
            get;
            set;
        }

        #endregion

        #region Methods

        public override void SMRunning()
        {
            UnSubscribeToProjectWorkCycle();
        }

        public PAEScaleBase GetFermentationStarterScale()
        {
            PAEScaleBase scale = null;

            if (!string.IsNullOrEmpty(FermentationStarterScaleACUrl))
            {
                scale = ACUrlCommand(FermentationStarterScaleACUrl) as PAEScaleBase;
            }

            if (scale == null)
            {
                //TODO:Alarm
            }

            return scale;
        }

        private void FindStores()
        {
            PAMParkingspace source;
            PAMSilo target;

            FindVirtualStores(ParentACComponent as PAProcessModule, out source, out target);

            using (ACMonitor.Lock(_20015_LockValue))
            {
                VirtualSourceStore = source;
                VirtualTargetStore = target;
            }

            if (source == null)
            {
                //TODO:error
            }

            if (target == null)
            {
                //TODO:error
            }
        }

        [ACMethodInfo("","",9999)]
        public Guid? GetSourceVirtualStoreID()
        {
            PAMParkingspace parkingSpace = null;
            using (ACMonitor.Lock(_20015_LockValue))
            {
                parkingSpace = VirtualSourceStore;
            }

            return parkingSpace?.ComponentClass.ACClassID;
        }

        public static void FindVirtualStores(PAProcessModule module, out PAMParkingspace source, out PAMSilo target)
        {
            source = null;
            target = null;

            if (module != null)
            {
                PAPoint pointIn = module.GetPoint(Const.PAPointMatIn1) as PAPoint;
                PAPoint pointOut = module.GetPoint(Const.PAPointMatOut1) as PAPoint;

                if (pointIn == null || pointOut == null)
                {
                    return;
                }

                source = pointIn.ConnectionList.FirstOrDefault(c => c.SourceParentComponent is PAMParkingspace)?.SourceParentComponent as PAMParkingspace;
                target = pointOut.ConnectionList.FirstOrDefault(c => c.TargetParentComponent is PAMSilo)?.TargetParentComponent as PAMSilo;
            }
        }


        [ACMethodInfo("", "", 800)]
        public string GetVirtualStoreACUrl()
        {
            PAMSilo silo = null;
            using (ACMonitor.Lock(_20015_LockValue))
            {
                silo = VirtualTargetStore;
            }

            return silo?.ACUrl;
        }

        [ACMethodInfo("", "", 800)]
        public void SwitchVirtualStoreOutwardEnabled()
        {
            PAMSilo virtualTargetStore = null;

            using (ACMonitor.Lock(_20015_LockValue))
            {
                virtualTargetStore = VirtualTargetStore;
            }

            if (virtualTargetStore == null)
                return;

            Facility facility = virtualTargetStore.Facility?.ValueT?.ValueT;

            if (facility == null)
                return;

            using (DatabaseApp dbApp = new DatabaseApp())
            {
                Facility store = facility.FromAppContext<Facility>(dbApp);
                if (store != null)
                {
                    store.OutwardEnabled = !store.OutwardEnabled;
                    Msg msg = dbApp.ACSaveChanges();

                    //TODO alarm 
                }
            }
        }

        [ACMethodInfo("", "en{'Clean pre prod container'}de{'Reinigen Vorproduktionsbehälter'}", 821)]
        public Msg Clean(short program)
        {
            PAProcessModuleVB parentModule = ParentACComponent as PAProcessModuleVB;
            if (parentModule != null && parentModule.IsOccupied)
            {
                return new Msg(eMsgLevel.Error, "The cleaning process can not be started because process module is occupied!");
            }

            if (CleaningMode == BakeryPreProdCleaningMode.OverBits)
                return CleanOverBits(program);

            return null;

        }

        private Msg CleanOverBits(short program)
        {
            if (CleaningProgram.ValueT > 999)
            {
                return new Msg(eMsgLevel.Error, "The cleaning process can not be started!");
            }
            else if (CleaningProgram.ValueT > 0)
            {
                if (CleaningProgram.ValueT != 10)
                {
                    //todo: check if line available
                    if (CleaningProgram.ValueT == program)
                    {
                        CleaningProgram.ValueT = 0; //turn off
                    }
                    else
                    {
                        CleaningProgram.ValueT = program;
                    }
                }
                else
                {
                    if (CleaningProgram.ValueT == program)
                    {
                        CleaningProgram.ValueT = 0; //turn off
                    }
                    else
                    {
                        CleaningProgram.ValueT = program;
                    }
                }
            }
            else
            {
                CleaningProgram.ValueT = program;
            }

            return null;
        }

        [ACMethodInfo("", "", 9999)]
        public virtual ACValueList GetPumpOverTargets()
        {
            using (Database db = new gip.core.datamodel.Database())
            {
                gip.core.datamodel.ACClass compClass = ParentACComponent?.ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(db);

                if (compClass == null)
                    return null;

                RoutingResult rResult = ACRoutingService.FindSuccessors(RoutingService, db, false, compClass, PAMIntermediatebin.SelRuleID_Intermediatebin, RouteDirections.Forwards,
                                                                        null, null, null, 0, true, true);

                if (rResult == null)
                {
                    return null;
                }

                if (rResult.Message != null && rResult.Message.MessageLevel == eMsgLevel.Error)
                {
                    return null;
                }

                ACComponent pumpModule = rResult.Routes?.FirstOrDefault().GetRouteTarget()?.TargetACComponent as ACComponent;

                if (pumpModule == null)
                {
                    return null;
                }

                rResult = ACRoutingService.FindSuccessors(RoutingService, db, false, pumpModule.ComponentClass, PAMParkingspace.SelRuleID_ParkingSpace, RouteDirections.Forwards,
                                                                        null, null, null, 0, true, true);

                if (rResult == null || rResult.Routes == null)
                {
                    return null;
                }

                ACValueList result = new ACValueList();

                using (DatabaseApp dbApp = new DatabaseApp())
                {
                    foreach (Route route in rResult.Routes)
                    {
                        RouteItem rItem = route.GetRouteTarget();
                        if (rItem == null)
                            continue;

                        PAMParkingspace ps = rItem.TargetACComponent as PAMParkingspace;
                        if (ps == null)
                            continue;

                        Facility facility = dbApp.Facility.FirstOrDefault(c => c.VBiFacilityACClassID == ps.ComponentClass.ACClassID);
                        if (facility == null)
                            continue;

                        ACValue acValue = new ACValue(ps.ACCaption, facility.FacilityID);
                        result.Add(acValue);
                    }
                }

                return result;
            }
        }

        #endregion
    }

    [DataContract]
    [ACSerializeableInfo]
    [ACClassInfo(ACKind = Global.ACKinds.TACEnum)]
    public enum BakeryPreProdCleaningMode : short
    {
        [EnumMember]
        OverBits = 0,
        [EnumMember]
        OverWorkflow = 10
    }
}

