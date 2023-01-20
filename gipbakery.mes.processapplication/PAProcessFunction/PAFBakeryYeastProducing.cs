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
        static PAFBakeryYeastProducing()
        {
            RegisterExecuteHandler(typeof(PAFBakeryYeastProducing), HandleExecuteACMethod_PAFBakeryYeastProducing);
        }

        #region c'tors

        public PAFBakeryYeastProducing(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
            _FermentationStarterScaleACUrl = new ACPropertyConfigValue<string>(this, nameof(FermentationStarterScaleACUrl), "");
            _TemperatureSensorACUrl = new ACPropertyConfigValue<string>(this, nameof(TemperatureSensorACUrl), "");
            _CleaningMode = new ACPropertyConfigValue<BakeryPreProdCleaningMode>(this, nameof(CleaningMode), BakeryPreProdCleaningMode.OverBits);
            _ContinueProdACClassWF = new ACPropertyConfigValue<string>(this, nameof(ContinueProdACClassWF), "");
            _PumpOverACClassWF = new ACPropertyConfigValue<string>(this, nameof(PumpOverACClassWF), "");
            _CleaningProdACClassWF = new ACPropertyConfigValue<string>(this, nameof(CleaningProdACClassWF), "");
            _PumpOverProcessModuleACUrl = new ACPropertyConfigValue<string>(this, nameof(PumpOverProcessModuleACUrl), "");
            _FinishOrderOnPumping = new ACPropertyConfigValue<bool>(this, nameof(FinishOrderOnPumping), true);
            _VirtualStoreMode = new ACPropertyConfigValue<ushort>(this, nameof(VirtualStoreMode), (ushort)VirtualStoreModeEnum.SourceAndTarget);
        }

        public override bool ACPostInit()
        {
            bool result = base.ACPostInit();

            _ = FermentationStarterScaleACUrl;
            _ = TemperatureSensorACUrl;
            _ = ContinueProdACClassWF;
            _ = PumpOverACClassWF;
            _ = CleaningProdACClassWF;
            _ = PumpOverProcessModuleACUrl;
            _ = FinishOrderOnPumping;
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

            if (_VirtualSourceStore != null)
            {
                _VirtualSourceStore.Detach();
                _VirtualSourceStore = null;
            }
            if (_VirtualTargetStore != null)
            {
                _VirtualTargetStore.Detach();
                _VirtualTargetStore = null;
            }
            return base.ACDeInit(deleteACClassTask);
        }

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

        protected ACPropertyConfigValue<bool> _FinishOrderOnPumping;
        [ACPropertyConfig("en{'Finish order for pumping first'}de{'Auftrag beednen zum Umpumpen'}")]
        public bool FinishOrderOnPumping
        {
            get
            {
                return _FinishOrderOnPumping.ValueT;
            }
            set
            {
                _FinishOrderOnPumping.ValueT = value;
            }
        }

        protected ACPropertyConfigValue<ushort> _VirtualStoreMode;
        [ACPropertyConfig("en{'Mode virtual Stores'}de{'Modus virtuelle Lagerplätze'}")]
        public ushort VirtualStoreMode
        {
            get
            {
                return _VirtualStoreMode.ValueT;
            }
            set
            {
                _VirtualStoreMode.ValueT = value;
            }
        }

        public enum VirtualStoreModeEnum
        {
            SourceAndTarget = 0,
            OnlySource = 1,
            OnlyTarget = 2,
            None = 3,
        }

        public VirtualStoreModeEnum VirtualStoreModeE
        {
            get
            {
                return (VirtualStoreModeEnum)VirtualStoreMode;
            }
        }

        #endregion

        public virtual bool IsVirtSourceStoreNecessary
        {
            get
            {
                return VirtualStoreModeE == VirtualStoreModeEnum.OnlySource || VirtualStoreModeE == VirtualStoreModeEnum.SourceAndTarget;
            }
        }

        public virtual bool IsVirtTargetStoreNecessary
        {
            get
            {
                return VirtualStoreModeE == VirtualStoreModeEnum.OnlyTarget || VirtualStoreModeE == VirtualStoreModeEnum.SourceAndTarget;
            }
        }

        private ACRef<PAMParkingspace> _VirtualSourceStore;
        public PAMParkingspace VirtualSourceStore
        {
            get
            {
                if (_VirtualSourceStore == null)
                    FindStores();
                return _VirtualSourceStore?.ValueT;
            }
        }

        private ACRef<PAMSilo> _VirtualTargetStore;
        public PAMSilo VirtualTargetStore
        {
            get
            {
                if (_VirtualTargetStore == null)
                    FindStores();
                return _VirtualTargetStore?.ValueT;
            }
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

            return scale;
        }

        private void FindStores()
        {
            PAMParkingspace source;
            PAMSilo target;
            FindVirtualStores(out source, out target);
            if (_VirtualSourceStore == null)
                _VirtualSourceStore = new ACRef<PAMParkingspace>(source, this);
            if (_VirtualTargetStore == null)
                _VirtualTargetStore = new ACRef<PAMSilo>(target, this);

            if (source == null && IsVirtSourceStoreNecessary)
            {
                //Error50484: The virtual source store can not be found!
                Msg msg = new Msg(this, eMsgLevel.Error, nameof(PAFBakeryYeastProducing), "FindStores(10)", 252, "Error50484");
                if (IsAlarmActive(FunctionError, msg.Message) == null)
                {
                    OnNewAlarmOccurred(FunctionError, msg);
                    Messages.LogMessageMsg(msg);
                }
            }

            if (target == null && IsVirtTargetStoreNecessary)
            {
                //Error50485: The virtual target store can not be found!
                Msg msg = new Msg(this, eMsgLevel.Error, nameof(PAFBakeryYeastProducing), "FindStores(20)", 263, "Error50485");
                if (IsAlarmActive(FunctionError, msg.Message) == null)
                {
                    OnNewAlarmOccurred(FunctionError, msg);
                    Messages.LogMessageMsg(msg);
                }
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

        public void FindVirtualStores(out PAMParkingspace source, out PAMSilo target)
        {
            PAProcessModule module = ParentACComponent as PAProcessModule;
            source = null;
            target = null;
            if (module == null)
                return;
            PAPoint pointIn = module.GetPoint(nameof(PAMMixer.PAPointMatIn3)) as PAPoint;
            if (pointIn != null)
            {
                source = pointIn.ConnectionList.FirstOrDefault(c => c.SourceParentComponent is PAMParkingspace)?.SourceParentComponent as PAMParkingspace;
                if (source == null)
                {
                    pointIn = module.GetPoint(nameof(PAMMixer.PAPointMatIn1)) as PAPoint;
                    if (pointIn != null)
                        source = pointIn.ConnectionList.FirstOrDefault(c => c.SourceParentComponent is PAMParkingspace)?.SourceParentComponent as PAMParkingspace;
                }
            }

            PAPoint pointOut = module.GetPoint(nameof(PAMMixer.PAPointMatOut1)) as PAPoint;
            if (pointOut == null)
                return;
            target = pointOut.ConnectionList.FirstOrDefault(c => c.TargetParentComponent is PAMSilo)?.TargetParentComponent as PAMSilo;
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
        public Msg SwitchVirtualStoreOutwardEnabled()
        {
            PAMSilo virtualTargetStore = null;

            using (ACMonitor.Lock(_20015_LockValue))
            {
                virtualTargetStore = VirtualTargetStore;
            }

            if (virtualTargetStore == null)
                return null;

            Facility facility = virtualTargetStore.Facility?.ValueT?.ValueT;

            if (facility == null)
                return null;

            using (DatabaseApp dbApp = new DatabaseApp())
            {
                Facility store = facility.FromAppContext<Facility>(dbApp);
                if (store != null)
                {
                    store.OutwardEnabled = !store.OutwardEnabled;
                    Msg msg = dbApp.ACSaveChanges();
                    return msg;
                }
            }
            return null;
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

        #region Handle Excute
        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            switch (acMethodName)
            {
                case "GetVirtualStoreACUrl":
                    result = GetVirtualStoreACUrl();
                    return true;
                case "SwitchVirtualStoreOutwardEnabled":
                    result = SwitchVirtualStoreOutwardEnabled();
                    return true;
                case "Clean":
                    result = Clean((short) acParameter[0]);
                    return true;
                case "GetPumpOverTargets":
                    result = GetPumpOverTargets();
                    return true;
                case "GetSourceVirtualStoreID":
                    result = GetSourceVirtualStoreID();
                    return true;
            }
            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        public static bool HandleExecuteACMethod_PAFBakeryYeastProducing(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PAFWorkCenterSelItemBase(out result, acComponent, acMethodName, acClassMethod, acParameter);
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

