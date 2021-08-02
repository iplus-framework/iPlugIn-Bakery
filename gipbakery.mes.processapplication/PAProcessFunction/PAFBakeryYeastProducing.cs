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
    public class PAFBakeryYeastProducing : PAProcessFunction
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
        }

        public override bool ACPostInit()
        {
            bool result = base.ACPostInit();

            string temp = FermentationStarterScaleACUrl;
            temp = TemperatureSensorACUrl;
            temp = ContinueProdACClassWF;
            temp = PumpOverACClassWF;
            BakeryPreProdCleaningMode mode = CleaningMode;

            FindStore();

            return result;
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            VirtualStore = null;
            return base.ACDeInit(deleteACClassTask);
        }

        public const string MN_GetFermentationStarterScaleACUrl = "GetFermentationStarterScaleACUrl";
        public const string MN_GetVirtualStoreACUrl = "GetVirtualStoreACUrl";
        public const string MN_Clean = "Clean";
        public const string MN_SwitchVirtualStoreOutwardEnabled = "SwitchVirtualStoreOutwardEnabled";
        public const string MN_GetPumpOverTargets = "GetPumpOverTargets";

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


        #endregion


        public PAMSilo VirtualStore
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

        [ACMethodInfo("", "", 800)]
        public string GetFermentationStarterScaleACUrl()
        {
            PAEScaleBase scale = GetFermentationStarterScale();
            return scale?.ACUrl;
        }


        //TODO: get store from PWBakeryGroupFermentation
        public void FindStore()
        {
            using (Database db = new gip.core.datamodel.Database())
            {
                RoutingResult rr = ACRoutingService.FindSuccessors(RoutingService, db, false, this.ParentACComponent.ComponentClass, PAMSilo.SelRuleID_Silo, RouteDirections.Forwards,
                                                                   null, null, null, 1, true, true);

                if (rr == null)
                {
                    return;
                }

                if (rr.Message != null && rr.Message.MessageLevel > eMsgLevel.Info)
                {
                    //error
                    return;
                }

                if (!rr.Routes.Any())
                {

                }

                RouteItem rItem = rr.Routes.FirstOrDefault().GetRouteTarget();

                VirtualStore = rItem?.TargetACComponent as PAMSilo;

                if (VirtualStore == null)
                {
                    //todo error
                }
            }
        }



        [ACMethodInfo("", "", 800)]
        public string GetVirtualStoreACUrl()
        {
            return VirtualStore?.ACUrl;
        }

        [ACMethodInfo("", "", 800)]
        public void SwitchVirtualStoreOutwardEnabled()
        {
            if (VirtualStore == null)
                return;

            Facility facility = VirtualStore.Facility?.ValueT?.ValueT;

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

            //TODO: clean over workflow
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
                gip.core.datamodel.ACClass compClass = null;

                using (ACMonitor.Lock(gip.core.datamodel.Database.GlobalDatabase.QueryLock_1X000))
                {
                    compClass = ParentACComponent?.ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(db);
                }

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

