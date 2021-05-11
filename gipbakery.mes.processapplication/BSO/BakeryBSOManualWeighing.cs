using gip.bso.manufacturing;
using gip.core.autocomponent;
using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioManufacturing, "en{'Manual weighing'}de{'Handverwiegung'}", Global.ACKinds.TACBSO, Global.ACStorableTypes.NotStorable, true, true, SortIndex = 100)]
    public class BakeryBSOManualWeighing : BSOManualWeighing
    {
        #region c'tors

        public BakeryBSOManualWeighing(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        #endregion

        #region Properties

        public ACRef<IACComponentPWNode> BakeryTempCalculator
        {
            get;
            set;
        }

        private IACContainerTNet<ACStateEnum> BakeryTempCalcACState
        {
            get;
            set;
        }


        private double _DoughTargetTemperature;
        [ACPropertyInfo(800)]
        public double DoughTargetTemperature
        {
            get => _DoughTargetTemperature;
            set
            {
                _DoughTargetTemperature = value;
                OnPropertyChanged("DoughTargetTemperature");
            }
        }

        private double _DoughCorrTemperature;
        [ACPropertyInfo(801)]
        public double DoughCorrTemperature
        {
            get => _DoughCorrTemperature;
            set
            {
                _DoughCorrTemperature = value;
                OnPropertyChanged("DoughCorrTemperature");
            }
        }

        private double _WaterTargetTemperature;
        [ACPropertyInfo(802)]
        public double WaterTargetTemperature
        {
            get => _WaterTargetTemperature;
            set
            {
                _WaterTargetTemperature = value;
                OnPropertyChanged("WaterTargetTemperature");
            }
        }

        private double _WaterCalcTemperature;
        [ACPropertyInfo(803)]
        public double WaterCalcTemperature
        {
            get => _WaterCalcTemperature;
            set
            {
                _WaterCalcTemperature = value;
                OnPropertyChanged("WaterCalcTemperature");
            }
        }

        #endregion

        #region Methods

        [ACMethodInfo("","",800)]
        public void ShowTemperaturesDialog()
        {
            ShowDialog(this, "TemperaturesDialog");
        }

        public bool IsShowTemperaturesDialog()
        {
            return true;
        }

        [ACMethodInfo("", "en{'Apply'}de{'Anwenden'}", 800)]
        public void ApplyTemperatures()
        {



            CloseTopDialog();
        }

        public override void OnGetPWGroup(IACComponentPWNode pwGroup)
        {
            IEnumerable<ACChildInstanceInfo> pwNodes;

            using (Database db = new Database())
            {
                var pwClass = db.ACClass.FirstOrDefault(c => c.ACProject.ACProjectTypeIndex == (short)Global.ACProjectTypes.ClassLibrary &&
                                                                                                c.ACIdentifier == PWBakeryTempCalc.PWClassName);
                ACRef<ACClass> refClass = new ACRef<ACClass>(pwClass, true);
                pwNodes = pwGroup.GetChildInstanceInfo(1, new ChildInstanceInfoSearchParam() { OnlyWorkflows = true, TypeOfRoots = refClass });
                refClass.Detach();
            }

            if (pwNodes == null || !pwNodes.Any())
                return;

            ACChildInstanceInfo node = pwNodes.FirstOrDefault();

            IACComponentPWNode pwNode = pwGroup.ACUrlCommand(node.ACUrlParent + "\\" + node.ACIdentifier) as IACComponentPWNode;
            if (pwNode == null)
            {
                //Error50290: The user does not have access rights for class PWManualWeighing ({0}).
                // Der Benutzer hat keine Zugriffsrechte auf Klasse PWManualWeighing ({0}).
                Messages.Error(this, "Error50290", false, node.ACUrlParent + "\\" + node.ACIdentifier);
                return;
            }
            BakeryTempCalculator = new ACRef<IACComponentPWNode>(pwNode, this);

            BakeryTempCalcACState = BakeryTempCalculator.ValueT.GetPropertyNet(Const.ACState) as IACContainerTNet<ACStateEnum>;

            if (BakeryTempCalcACState != null)
            {
                BakeryTempCalcACState.PropertyChanged += BakeryTempCalcACState_PropertyChanged;
            }

            GetTemperatures();

        }

        private void BakeryTempCalcACState_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                if( BakeryTempCalcACState.ValueT == ACStateEnum.SMRunning)
                {
                    
                }
            }
        }

        private void StartBakeryTempCalc()
        {

        }

        private void GetTemperatures()
        {
            ACMethod config = BakeryTempCalculator.ValueT.ACUrlCommand("MyConfiguration") as ACMethod;
            if (config != null)
            {
                ACValue dTemp = config.ParameterValueList.GetACValue("DoughTemp");
                if (dTemp != null)
                {
                    DoughTargetTemperature = dTemp.ParamAsDouble;
                }

                ACValue wTemp = config.ParameterValueList.GetACValue("WaterTemp");
                if (wTemp != null)
                {
                    WaterTargetTemperature = wTemp.ParamAsDouble;
                }
            }
        }


        #endregion
    }
}
