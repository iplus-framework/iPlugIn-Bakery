using gip.core.datamodel;
using gip.mes.datamodel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACSerializeableInfo()]
    [DataContract]
    [ACClassInfo(Const.PackName_VarioSystem, "en{'Temperature item'}de{'TemperatureItem'}", Global.ACKinds.TACSimpleClass)]
    public class MaterialTempMeasureItem : INotifyPropertyChanged, IACObject
    {
        public MaterialTempMeasureItem()
        {

        }

        public MaterialTempMeasureItem(MaterialConfig materialConfig)
        {
            if (materialConfig == null)
                return;

            MaterialConfig = materialConfig;
            MaterialConfigID = materialConfig.MaterialConfigID;
            var temp = MaterialConfig?.Material;

            double? temperature = materialConfig.Value as double?;
            if (temperature.HasValue)
                Temperature = temperature.Value;

            Guid posID;
            if (materialConfig.Comment != null && Guid.TryParse(materialConfig.Comment, out posID))
            {
                PartslistPos pos = materialConfig.Material.PartslistPos_Material.FirstOrDefault(c => c.PartslistPosID == posID);
                if (pos != null)
                {
                    var prop = pos.ACProperties.GetOrCreateACPropertyExtByName(PAFBakeryTempMeasuring.PN_CyclicMeasurement, false);
                    if (prop != null)
                    {
                        MeasurePeriod = prop.Value as TimeSpan?;
                    }
                }
            }
            
            if (MeasurePeriod == null)
            {
                var prop = materialConfig.Material.ACProperties.GetOrCreateACPropertyExtByName(PAFBakeryTempMeasuring.PN_CyclicMeasurement, false);
                if (prop != null)
                {
                    MeasurePeriod = prop.Value as TimeSpan?;
                }
            }

            SetLastMeasureTime(materialConfig.UpdateDate);

            double? tempValue = materialConfig.Value as double?;

            if (tempValue.HasValue && tempValue == 0)
            {
                NextMeasureTerm = DateTime.Now;
                IsTempMeasureNeeded = true;
            }
        }

        [DataMember(Name = "A")]
        public Guid MaterialConfigID
        {
            get;
            set;
        }

        private MaterialConfig _MaterialConfig;

        [IgnoreDataMember]
        [ACPropertyInfo(100)]
        public MaterialConfig MaterialConfig
        {
            get => _MaterialConfig;
            set
            {
                _MaterialConfig = value;
                OnPropertyChanged("MaterialConfig");
            }
        }

        private bool _IsTempMeasureNeeded;
        [DataMember(Name = "B")]
        [ACPropertyInfo(101)]
        public bool IsTempMeasureNeeded
        {
            get => _IsTempMeasureNeeded;
            set
            {
                _IsTempMeasureNeeded = value;
                OnPropertyChanged("IsTempMeasureNeeded");
            }
        }

        [DataMember(Name = "C")]
        public bool IsMeasurementOff
        {
            get;
            set;
        }

        [IgnoreDataMember]
        [ACPropertyInfo(101)]
        public TimeSpan? MeasurePeriod
        {
            get;
            set;
        }

        private DateTime _NextMeasureTerm;
        [DataMember(Name = "D")]
        [ACPropertyInfo(102)]
        public DateTime NextMeasureTerm
        {
            get => _NextMeasureTerm;
            set
            {
                _NextMeasureTerm = value;
                OnPropertyChanged("NextMeasureTerm");
            }
        }

        [IgnoreDataMember]
        public DateTime LastMeasureTime
        {
            get;
            set;
        }

        private double _Temperature;
        [DataMember(Name = "E")]
        [ACPropertyInfo(103)]
        public double Temperature
        {
            get => _Temperature;
            set
            {
                _Temperature = value;
                OnPropertyChanged("Temperature");
            }
        }

        public IACObject ParentACObject => null;

        public IACType ACType => this.ReflectACType();

        public IEnumerable<IACObject> ACContentList => this.ReflectGetACContentList();

        public string ACIdentifier => this.ReflectGetACIdentifier();

        public string ACCaption => ACIdentifier;

        public void SetLastMeasureTime(DateTime dateTime)
        {
            LastMeasureTime = dateTime;
            if (MeasurePeriod.HasValue)
                NextMeasureTerm = dateTime + MeasurePeriod.Value;
        }

        public void AttachToDatabase(DatabaseApp dbApp)
        {
            MaterialConfig = dbApp.MaterialConfig.FirstOrDefault(c => c.MaterialConfigID == MaterialConfigID);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public object ACUrlCommand(string acUrl, params object[] acParameter)
        {
            return this.ReflectACUrlCommand(acUrl, acParameter);
        }

        public bool IsEnabledACUrlCommand(string acUrl, params object[] acParameter)
        {
            return this.IsEnabledACUrlCommand(acUrl, acParameter);
        }

        public string GetACUrl(IACObject rootACObject = null)
        {
            return this.ReflectGetACUrl(rootACObject);
        }

        public bool ACUrlBinding(string acUrl, ref IACType acTypeInfo, ref object source, ref string path, ref Global.ControlModes rightControlMode)
        {
            return this.ReflectACUrlBinding(acUrl, ref acTypeInfo, ref source, ref path, ref rightControlMode);
        }

        public bool ACUrlTypeInfo(string acUrl, ref ACUrlTypeInfo acUrlTypeInfo)
        {
            throw new NotImplementedException();
        }
    }

    [ACSerializeableInfo()]
    [CollectionDataContract]
    [ACClassInfo(Const.PackName_VarioSystem, "en{'Temperature items'}de{'Temperature items'}", Global.ACKinds.TACSimpleClass)]
    public class MaterialTempMeasureList : List<MaterialTempMeasureItem>
    {
        public MaterialTempMeasureList()
        {

        }

        public MaterialTempMeasureList(IEnumerable<MaterialTempMeasureItem> collection) : base(collection)
        {

        }
    }
}
