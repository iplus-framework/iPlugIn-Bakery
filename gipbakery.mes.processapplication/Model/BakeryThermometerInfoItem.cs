using gip.core.datamodel;
using System.ComponentModel;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioSystem, "en{'BakeryThermometerInfoItem'}de{'BakeryThermometerInfoItem'}", Global.ACKinds.TACSimpleClass)]
    public class BakeryThermometerInfoItem : EntityBase
    {
        public BakeryThermometerInfoItem(string siloACCaption, IACComponent tempSensor, bool siloOutwardEnabled)
        {
            SiloACCaption = siloACCaption;
            TempSensorACCaption = tempSensor.ACCaption;
            TempSensorACUrl = tempSensor.ACUrl;
            SiloOutwardEnabled = siloOutwardEnabled;
        }

        [ACPropertyInfo(9999)]
        public string SiloACCaption
        {
            get;
            set;
        }

        [ACPropertyInfo(9999)]
        public string TempSensorACCaption
        {
            get;
            set;
        }

        [ACPropertyInfo(9999)]
        public string TempSensorACUrl
        {
            get;
            set;
        }

        private bool _SiloOutwardEnabled;

        [ACPropertyInfo(9999)]
        public bool SiloOutwardEnabled
        {
            get => _SiloOutwardEnabled;
            set
            {
                _SiloOutwardEnabled = value;
                OnPropertyChanged("SiloOutwardEnabled");
            }
        }
    }
}
