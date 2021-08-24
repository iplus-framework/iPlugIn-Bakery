using gip.core.datamodel;
using System.ComponentModel;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioSystem, "en{'BakeryThermometerInfoItem'}de{'BakeryThermometerInfoItem'}", Global.ACKinds.TACSimpleClass)]
    public class BakeryThermometerInfoItem : INotifyPropertyChanged
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

        [ACPropertyInfo(9999)]
        public bool SiloOutwardEnabled
        {
            get;
            set;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
