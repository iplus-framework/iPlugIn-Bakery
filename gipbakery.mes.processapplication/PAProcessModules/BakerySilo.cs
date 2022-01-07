using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Bakery Silo'}de{'Backerei Silo'}", Global.ACKinds.TPAProcessModule, Global.ACStorableTypes.Required, false, PWGroupVB.PWClassName, true)]
    public class BakerySilo : PAMSilo
    {
        #region c'tors

        public BakerySilo(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
            _WarmingOffset = new ACPropertyConfigValue<short>(this, "WarmingOffset", 0);
        }

        public override bool ACPostInit()
        {
            short temp = WarmingOffset;
            return base.ACPostInit();
        }

        #endregion

        #region Properties

        private PAEBakeryThermometer[] _Thermometers;

        public IEnumerable<PAEBakeryThermometer> Thermometers
        {
            get
            {
                if (_Thermometers == null)
                {
                    _Thermometers = FindChildComponents<PAEBakeryThermometer>(c => c is PAEBakeryThermometer).ToArray();
                }

                return _Thermometers;
            }
        }

        private ACPropertyConfigValue<short> _WarmingOffset;
        [ACPropertyConfig("en{'Warming offset according room temperature [%]'}de{'Wärmeausgleich nach Raumtemperatur [%]'}")]
        public short WarmingOffset
        {
            get => _WarmingOffset.ValueT;
            set
            {
                _WarmingOffset.ValueT = value;
                OnPropertyChanged("WarmingOffset");
            }
        }

        #endregion

        #region Methods

        #endregion
    }
}
