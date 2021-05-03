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
        }

        #endregion

        #region Properties

        private PAEBakeryThermometer[] _Thermometers;

        public IEnumerable<PAEBakeryThermometer> Thermometers
        {
            get
            {
                if(_Thermometers == null)
                {
                    _Thermometers = FindChildComponents<PAEBakeryThermometer>(c => c is PAEBakeryThermometer).ToArray();
                }

                return _Thermometers;
            }
        }

        #endregion


        #region Methods

        #endregion
    }
}
