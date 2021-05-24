using gip.core.datamodel;
using gip.core.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioSystem, "en{'Bakery Thermometer'}de{'Backerei Thermometer)'}", Global.ACKinds.TPAModule, Global.ACStorableTypes.Required, false, true, "", "", 9999)]
    public class PAEBakeryThermometer : PAEThermometer
    {
        #region c'tors

        public PAEBakeryThermometer(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        #endregion

        #region Properties

        [ACPropertyInfo(true, 700, "", "en{'Default temperature'}de{'Standard-Temperatur'}")]
        public double TemperatureDefault
        {
            get;
            set;
        }

        [ACPropertyInfo(true, 701, "", "en{'Disabled for temperature calculation'}de{'Deaktiviert für Temperaturberechnung'}")]
        public bool DisabledForTempCalculation
        {
            get;
            set;
        }

        public double ActualValueForCalculation
        {
            get
            {
                bool checkLower = LowerLimit1.ValueT > 0.00001 || LowerLimit1.ValueT < -0.00001;

                if (checkLower && ActualValue.ValueT < LowerLimit1.ValueT)
                    return TemperatureDefault;

                bool checkUpper = UpperLimit1.ValueT > 0.00001 || UpperLimit1.ValueT < -0.00001;

                if (checkUpper && ActualValue.ValueT > UpperLimit1.ValueT)
                    return TemperatureDefault;

                return ActualValue.ValueT;
            }
        }

        #endregion

        #region Methods

        #endregion
    }
}
