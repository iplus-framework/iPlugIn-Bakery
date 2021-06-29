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
    [ACClassInfo(Const.PackName_VarioManufacturing, "en{'Temperatures'}de{'Temperaturen'}", Global.ACKinds.TACBSO, Global.ACStorableTypes.NotStorable, true, true, SortIndex = 700)]
    public class BakeryBSOTemperature : BSOWorkCenterChild
    {
        #region c'tors

        public BakeryBSOTemperature(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACInit(Global.ACStartTypes startChildMode = Global.ACStartTypes.Automatic)
        {
            return base.ACInit(startChildMode);
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            DeActivate();
            return base.ACDeInit(deleteACClassTask);
        }

        public const string ClassName = "BakeryBSOTemperature";

        #endregion

        #region Properties

        [ACPropertySelected(700, "MaterialTemperature")]
        public MaterialTemperature SelectedMaterialTemperature
        {
            get;
            set;
        }

        private IEnumerable<MaterialTemperature> _MaterialTemperatures;
        [ACPropertyList(700, "MaterialTemperature")]
        public IEnumerable<MaterialTemperature> MaterialTemperatures
        {
            get => _MaterialTemperatures;
            set
            {
                _MaterialTemperatures = value;
                OnPropertyChanged("MaterialTemperatures");
            }
        }

        public ACRef<ACComponent> TemperatureServiceProxy
        {
            get;
            set;
        }

        private IACContainerTNet<short> _TemperatureServiceInfo;
        
        private ACRef<ACComponent> _TempMeasuringFunc;
        public ACComponent TempMeasuringFunc
        {
            get => _TempMeasuringFunc?.ValueT;
        }

        private Type _BakeryTempMeasuringType = typeof(PAFBakeryTempMeasuring);

        IACContainerTNet<MaterialTempMeasureList> _ChangedItemsProp;

        private MaterialTempMeasureItem _SelectedTempMeasureItem;
        [ACPropertySelected(750, "CyclicMeasurement")]
        public MaterialTempMeasureItem SelectedTempMeasureItem
        {
            get => _SelectedTempMeasureItem;
            set
            {
                _SelectedTempMeasureItem = value;
                OnPropertyChanged("SelectedTempMeasureItem");
            }
        }

        private List<MaterialTempMeasureItem> _TempMeasureItemsList;
        [ACPropertyList(750, "CyclicMeasurement")]
        public List<MaterialTempMeasureItem> TempMeasureItemsList
        {
            get => _TempMeasureItemsList;
            set
            {
                _TempMeasureItemsList = value;
                OnPropertyChanged("TempMeasureItemsList");
            }
        }

        public double? _TemperatureMeasurementOverride;
        [ACPropertyInfo(701, "", "en{'Sensor temp. override'}de{'Sensor Temp. übersteuern'}")]
        public double? TemperatureMeasurementOverride
        {
            get => _TemperatureMeasurementOverride;
            set
            {
                _TemperatureMeasurementOverride = value;
                OnPropertyChanged("TemperatureMeasurementOverride");
            }
        }

        private ACRef<ACComponent> _ManualTempMeasurementSensor;

        [ACPropertyInfo(790)]
        public ACComponent ManualTempMeasurementSensor
        {
            get => _ManualTempMeasurementSensor?.ValueT;
        }

        #endregion

        #region Methods

        public override void Activate(ACComponent selectedProcessModule)
        {
            BakeryBSOWorkCenterSelector workCenter = ParentBSOWCS as BakeryBSOWorkCenterSelector;
            if (workCenter != null)
            {
                if (string.IsNullOrEmpty(workCenter.BakeryTemperatureServiceACUrl))
                {
                    //TODO: message
                    return;
                }

                ACComponent tempService = Root.ACUrlCommand(workCenter.BakeryTemperatureServiceACUrl) as ACComponent;
                if (tempService == null && Root.IsProxy)
                {
                    //TODO message
                    return;
                }

                TemperatureServiceProxy = new ACRef<ACComponent>(tempService, this);

                _TemperatureServiceInfo = TemperatureServiceProxy.ValueT.GetPropertyNet(PABakeryTempService.PN_TemperatureServiceInfo) as IACContainerTNet<short>;
                if (_TemperatureServiceInfo != null)
                {
                    (_TemperatureServiceInfo as IACPropertyNetBase).PropertyChanged += TempServiceInfo_PropertyChanged;
                }


                ACValueList result = TemperatureServiceProxy.ValueT.ExecuteMethod(PABakeryTempService.MN_GetTemperaturesInfo, selectedProcessModule.ComponentClass.ACClassID) as ACValueList;
                if (result != null && result.Any())
                {
                    var materialTempList = result.Select(c => c.Value as MaterialTemperature).ToArray();

                    foreach(var materialTemp in materialTempList)
                    {
                        materialTemp.BuildBakeryThermometersInfo(DatabaseApp);
                    }

                    MaterialTemperatures = materialTempList;

                }
            }

            ACComponent tempMeasureFunc = ParentBSOWCS.CurrentProcessModule.ACComponentChildsOnServer.FirstOrDefault(c => _BakeryTempMeasuringType.IsAssignableFrom(c.ComponentClass.ObjectType)) as ACComponent;
            if (tempMeasureFunc == null)
            {
                // error;
                return;
            }

            _TempMeasuringFunc = new ACRef<ACComponent>(tempMeasureFunc, this);

            string sensorACUrl = TempMeasuringFunc.ExecuteMethod("GetManualTempMeasurementACUrl") as string;

            var sensor = Root.ACUrlCommand(sensorACUrl) as ACComponent;
            if (sensor != null)
            {
                _ManualTempMeasurementSensor = new ACRef<ACComponent>(sensor, this);
            }

            MaterialTempMeasureList measureItems = TempMeasuringFunc.ACUrlCommand("!GetTempMeasureItems") as MaterialTempMeasureList;
            if (measureItems == null)
            {
                //TODO: error
                return;
            }

            _ChangedItemsProp = TempMeasuringFunc.GetPropertyNet("ChangedTemperatureMeasureItems") as IACContainerTNet<MaterialTempMeasureList>;
            if (_ChangedItemsProp == null)
            {
                //TODO: error
                return;
            }

            _ChangedItemsProp.PropertyChanged += ChangedItemsProp_PropertyChanged;

            foreach (var measureItem in measureItems)
            {
                measureItem.AttachToDatabase(DatabaseApp);
            }

            TempMeasureItemsList = measureItems.OrderBy(c => c.NextMeasureTerm).ToList();
        }

        private void ChangedItemsProp_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                var temp = _ChangedItemsProp.ValueT.ToArray();
                Task.Run(() => RefreshChangedItems(temp));
            }
        }

        private void RefreshChangedItems(IEnumerable<MaterialTempMeasureItem> changedItems)
        {
            if (changedItems == null || !changedItems.Any())
                return;

            bool isAnyAddedOrRemoved = false;

            foreach (MaterialTempMeasureItem item in changedItems)
            {
                MaterialTempMeasureItem tempItem = TempMeasureItemsList.FirstOrDefault(c => c.MaterialConfigID == item.MaterialConfigID);
                if (tempItem == null)
                {
                    item.AttachToDatabase(DatabaseApp);
                    TempMeasureItemsList.Add(item);
                    isAnyAddedOrRemoved = true;
                }
                else
                {
                    if (item.IsMeasurementOff)
                    {
                        TempMeasureItemsList.Remove(tempItem);
                        isAnyAddedOrRemoved = true;
                        continue;
                    }

                    tempItem.IsTempMeasureNeeded = item.IsTempMeasureNeeded;
                    tempItem.NextMeasureTerm = item.NextMeasureTerm;
                    tempItem.Temperature = item.Temperature;
                }
            }

            if (isAnyAddedOrRemoved)
                TempMeasureItemsList = TempMeasureItemsList.OrderBy(c => c.MaterialConfig.Material.MaterialNo).ToList();
            
        }

        //TODO: try/catch
        private void TempServiceInfo_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                // 1 Refresh average temperatures
                // 2 Refresh temperatures info
                if (_TemperatureServiceInfo.ValueT == 1)
                {
                    Task.Run(() =>
                    {
                        if (MaterialTemperatures == null)
                            return;

                        ACValueList result = TemperatureServiceProxy.ValueT.ExecuteMethod(PABakeryTempService.MN_GetAverageTemperatures, ParentBSOWCS.CurrentProcessModule.ComponentClass.ACClassID) as ACValueList;
                        if (result != null && result.Any())
                        {
                            foreach (ACValue acValue in result)
                            {
                                MaterialTemperature mt = MaterialTemperatures.FirstOrDefault(c => c.MaterialNo == acValue.ACIdentifier);
                                if (mt != null)
                                {
                                    mt.AverageTemperature = acValue.ParamAsDouble;
                                }
                            }
                        }
                    });
                }
                else if (_TemperatureServiceInfo.ValueT == 2)
                {
                    Task.Run(() =>
                    {
                        ACValueList result = TemperatureServiceProxy.ValueT.ExecuteMethod(PABakeryTempService.MN_GetTemperaturesInfo, ParentBSOWCS.CurrentProcessModule.ComponentClass.ACClassID) as ACValueList;
                        if (result != null && result.Any())
                        {
                            var materialTempList = result.Select(c => c.Value as MaterialTemperature).ToArray();

                            foreach (var materialTemp in materialTempList)
                            {
                                materialTemp.BuildBakeryThermometersInfo(DatabaseApp);
                            }

                            MaterialTemperatures = materialTempList;
                        }
                    });
                }
            }
        }

        public override void DeActivate()
        {
            if (TemperatureServiceProxy != null)
            {
                TemperatureServiceProxy.Detach();
                TemperatureServiceProxy = null;
            }

            if (_TemperatureServiceInfo != null)
            {
                (_TemperatureServiceInfo as IACPropertyNetBase).PropertyChanged -= TempServiceInfo_PropertyChanged;
                _TemperatureServiceInfo = null;
            }

            if (_ManualTempMeasurementSensor != null)
            {
                _ManualTempMeasurementSensor.Detach();
                _ManualTempMeasurementSensor = null;
            }

            if (_ChangedItemsProp != null)
            {
                _ChangedItemsProp.PropertyChanged -= ChangedItemsProp_PropertyChanged;
                _ChangedItemsProp = null;
            }
        }

        [ACMethodInfo("", "en{'Measure component temperature'}de{'Komponententemperatur messen'}", 701)]
        public void MeasureComponentTemp()
        {
            TempMeasuringFunc.ExecuteMethod(PAFBakeryTempMeasuring.MN_MeasureMaterialTemperature, SelectedTempMeasureItem.MaterialConfigID, TemperatureMeasurementOverride);
        }

        public bool IsEnabledMeasureComponentTemp()
        {
            return ParentBSOWCS != null && ParentBSOWCS.CurrentProcessModule != null && SelectedTempMeasureItem != null;
        }

        [ACMethodInfo("", "en{'Delete temperature mesurement'}de{'Temperaturmessung löschen'}", 702)]
        public void DeleteComponentTempMeasurement()
        {
            TempMeasuringFunc.ExecuteMethod(PAFBakeryTempMeasuring.MN_DeleteMeasurement, SelectedTempMeasureItem.MaterialConfigID);
        }

        public bool IsEnabledDeleteComponentTempMeasurement()
        {
            return ParentBSOWCS != null && ParentBSOWCS.CurrentProcessModule != null && SelectedTempMeasureItem != null;
        }

        [ACMethodInfo("", "en{'Copy temperature measurement'}de{'Temperaturmessung kopieren'}", 703)]
        public void CopyComponentTempMeasurement()
        {

        }

        public bool IsEnabledCopyComponentTempMeasurement()
        {
            return SelectedTempMeasureItem != null;
        }

        [ACMethodInfo("", "en{'Turn on measurement hint'}de{'Hinweis zur Messung einschalten'}", 703)]
        public void TurnOnMeasurementHint()
        {
            TempMeasuringFunc.ExecuteMethod(PAFBakeryTempMeasuring.MN_ChangeHintSetting, false);
        }

        public bool IsEnabledTurnOnMeasurementHint()
        {
            return TempMeasuringFunc != null;
        }

        [ACMethodInfo("", "en{'Turn off measurement hint'}de{'Hinweis zur Messung ausschalten'}", 704)]
        public void TurnOffMeasurementHint()
        {
            TempMeasuringFunc.ExecuteMethod(PAFBakeryTempMeasuring.MN_ChangeHintSetting, true);
        }

        public bool IsEnabledTurnOffMeasurementHint()
        {
            return TempMeasuringFunc != null;
        }

        #endregion
    }
}
