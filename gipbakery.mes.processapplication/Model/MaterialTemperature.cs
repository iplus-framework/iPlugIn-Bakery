using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
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
    [DataContract]
    [ACSerializeableInfo()]
    [ACClassInfo(Const.PackName_VarioSystem, "en{'Material temperature'}de{'Material temperature'}", Global.ACKinds.TACSimpleClass)]
    public class MaterialTemperature : IACObject, INotifyPropertyChanged
    {
        public MaterialTemperature()
        {
            SilosWithPTC = new List<BakerySilo>();
            BakeryThermometers = new List<PAEBakeryThermometer>();
            BakeryThermometersACUrls = new List<string>();
        }

        [IgnoreDataMember]
        public bool IsRoomTemperature
        {
            get;
            set;
        }

        [DataMember]
        [ACPropertyInfo(9999)]
        public string MaterialNo
        {
            get;
            set;
        }

        [IgnoreDataMember]
        [ACPropertyInfo(9999)]
        public gip.mes.datamodel.Material Material
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public List<BakerySilo> SilosWithPTC
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public List<PAEBakeryThermometer> BakeryThermometers
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public double AverageTemperatureCalc
        {
            get
            {
                var temp = GetBakeryThermometers();
                if (temp.Any())
                {
                    return temp.Sum(c => c.ActualValueForCalculation) / temp.Count();
                }
                return 0.0;
            }
        }

        private double? _AverageTemperature;
        [DataMember]
        [ACPropertyInfo(9999)]
        public double? AverageTemperature
        {
            get => _AverageTemperature;
            set
            {
                _AverageTemperature = value;
                OnPropertyChanged("AverageTemperature");
            }
        }

        [DataMember]
        public List<string> BakeryThermometersACUrls
        {
            get;
            set;
        }

        [IgnoreDataMember]
        [ACPropertyInfo(9999)]
        public List<Tuple<string, string, string>> BakeryThermometersInfo
        {
            get;
            set;
        }

        [DataMember]
        public WaterType Water
        {
            get;
            set;
        }

        [DataMember]
        public double? WaterMinDosingQuantity
        {
            get;
            set;
        }

        [DataMember]
        public double? WaterDefaultTemperature
        {
            get;
            set;
        }

        #region IACObject

        [IgnoreDataMember]
        public IACObject ParentACObject => null;

        [IgnoreDataMember]
        public IACType ACType => this.ReflectACType();

        [IgnoreDataMember]
        public IEnumerable<IACObject> ACContentList => this.ReflectGetACContentList();

        [IgnoreDataMember]
        public string ACIdentifier => this.ReflectGetACIdentifier();

        [IgnoreDataMember]
        public string ACCaption => this.ACIdentifier;

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddSilo(BakerySilo bakerySilo)
        {
            SilosWithPTC.Add(bakerySilo);
            BakeryThermometersACUrls.AddRange(bakerySilo.Thermometers.Where(c => !c.DisabledForTempCalculation).Select(t => t.ACUrl));
        }

        public void RemoveSilo(BakerySilo bakerySilo)
        {
            SilosWithPTC.Remove(bakerySilo);
            foreach (string ptc in bakerySilo.Thermometers.Where(c => !c.DisabledForTempCalculation).Select(t => t.ACUrl))
            {
                BakeryThermometersACUrls.Remove(ptc);
            }
        }

        public void AddThermometer(PAEBakeryThermometer thermometer)
        {
            BakeryThermometers.Add(thermometer);
            BakeryThermometersACUrls.Add(thermometer.ACUrl);
        }

        public IEnumerable<PAEBakeryThermometer> GetBakeryThermometers()
        {
            return BakeryThermometers.Concat(SilosWithPTC.SelectMany(c => c.Thermometers.Where(t => !t.DisabledForTempCalculation)));
        }

        public void BuildBakeryThermometersInfo(DatabaseApp dbApp)
        {
            Material = dbApp.Material.FirstOrDefault(c => c.MaterialNo == MaterialNo);
            BakeryThermometersInfo = new List<Tuple<string, string, string>>();

            foreach (string tACUrl in BakeryThermometersACUrls)
            {
                ACComponent tProxy = dbApp.Root().ACUrlCommand(tACUrl) as ACComponent;
                if (tProxy != null)
                {
                    string siloACCaption = null;

                    if (typeof(PAMSilo).IsAssignableFrom(tProxy.ParentACComponent.ComponentClass.ObjectType))
                    {
                        siloACCaption += tProxy.ParentACComponent.ACCaption;
                    }

                    BakeryThermometersInfo.Add(new Tuple<string, string, string>(siloACCaption, tProxy.ACCaption, tACUrl));
                }
            }
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

        #endregion

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
