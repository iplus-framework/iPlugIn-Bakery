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
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Sour dough'}de{'Sauerteig'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, true, "", BSOConfig = BakeryBSOSourDoughProducing.ClassName, SortIndex = 50)]
    public class PAFBakerySourDoughProducing : PAProcessFunction
    {
        public PAFBakerySourDoughProducing(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
            _FermentationStarterScaleACUrl = new ACPropertyConfigValue<string>(this, "FermentationStarterScaleACUrl", "");
            _CleaningMode = new ACPropertyConfigValue<BakeryPreProdCleaningMode>(this, "CleaningMode", BakeryPreProdCleaningMode.OverBits);
        }

        public override bool ACPostInit()
        {
            bool result = base.ACPostInit();

            string temp = FermentationStarterScaleACUrl;
            BakeryPreProdCleaningMode mode = CleaningMode;

            FindSourDoughStore();

            return result;
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            SourDoughStore = null;
            return base.ACDeInit(deleteACClassTask);
        }

        public const string MN_GetFermentationStarterScaleACUrl = "GetFermentationStarterScaleACUrl";

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

        [ACMethodInfo("","", 800)]
        public string GetFermentationStarterScaleACUrl()
        {
            PAEScaleBase scale = GetFermentationStarterScale();
            return scale?.ACUrl;
        }



        public void FindSourDoughStore()
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

                SourDoughStore = rItem?.TargetACComponent as PAMSilo;

                if (SourDoughStore == null)
                {
                    //todo error
                }
            }
        }

        public PAMSilo SourDoughStore
        {
            get;
            set;
        }

        [ACMethodInfo("", "", 800)]
        public string GetSourDoughStoreACUrl()
        {
            return SourDoughStore?.ACUrl;
        }

        [ACMethodInfo("", "", 800)]
        public void SwitchSourDoughStoreOutwardEnabled()
        {
            if (SourDoughStore == null)
                return;

            Facility facility = SourDoughStore.Facility?.ValueT?.ValueT;

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

        [ACPropertyBindingTarget(820, "", "en{'Pre production cleaning program'}de{'Reinigungsprogramm für die Vorproduktion'}")]
        public IACContainerTNet<short> CleaningProgram
        {
            get;
            set;
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
