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

        public const string ClassName = nameof(BakeryBSOTemperature);

        #endregion

        #region Properties

        private ACMonitorObject _70100_CurrProcessModLock = new ACMonitorObject(70100);

        private ACComponent _CurrentProcessModule;
        public override ACComponent CurrentProcessModule 
        {
            get
            {
                using (ACMonitor.Lock(_70100_CurrProcessModLock))
                {
                    return _CurrentProcessModule;
                }
            }
            protected set
            {
                using (ACMonitor.Lock(_70100_CurrProcessModLock))
                {
                    _CurrentProcessModule = value;
                }
            }
        }

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
                OnPropertyChanged();
            }
        }

        public ACRef<ACComponent> TemperatureServiceProxy
        {
            get;
            set;
        }

        private IACContainerTNet<short> _TemperatureServiceInfo;
        
        private ACRef<ACComponent> _TempMeasuringFunc;
        [ACPropertyInfo(9999)]
        public ACComponent TempMeasuringFunc
        {
            get => _TempMeasuringFunc?.ValueT;
        }

        IACPropertyNetBase _ShowOnlyOrderItemsProp;

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
                OnPropertyChanged();
            }
        }

        private List<MaterialTempMeasureItem> _TemperatureMeasureItemsList;


        private List<MaterialTempMeasureItem> _TempMeasureItemsList;
        [ACPropertyList(750, "CyclicMeasurement")]
        public List<MaterialTempMeasureItem> TempMeasureItemsList
        {
            get => _TempMeasureItemsList;
            set
            {
                _TempMeasureItemsList = value;
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }

        private ACRef<ACComponent> _ManualTempMeasurementSensor;

        [ACPropertyInfo(790)]
        public ACComponent ManualTempMeasurementSensor
        {
            get => _ManualTempMeasurementSensor?.ValueT;
        }


        private bool _ShowOnlyOrderItems;
        [ACPropertyInfo(9999, "", "en{'Show only order measure items'}de{'Nur Auftragsmessungspositionen anzeigen'}")]
        public bool ShowOnlyOrderItems
        {
            get
            {
                return _ShowOnlyOrderItems;
            }
            set
            {
                _ShowOnlyOrderItems = value;
                if (_ShowOnlyOrderItemsProp != null)
                    _ShowOnlyOrderItemsProp.Value = _ShowOnlyOrderItems;

                OnFilterTempMeasurementList();

                OnPropertyChanged();
            }
        }

        #endregion

        #region Methods

        public override void Activate(ACComponent selectedProcessModule)
        {
            CurrentProcessModule = selectedProcessModule;

            BakeryBSOWorkCenterSelector workCenter = ParentBSOWCS as BakeryBSOWorkCenterSelector;
            if (workCenter != null)
            {
                if (string.IsNullOrEmpty(workCenter.BakeryTemperatureServiceACUrl))
                {
                    //Error50459: The bakery temperature service ACUrl is not configured. Please configure temperature service ACUrl on the BakeryBSOWorkCenterSelector.
                    Messages.Error(this, "Error50459");
                    return;
                }

                ACComponent tempService = Root.ACUrlCommand(workCenter.BakeryTemperatureServiceACUrl) as ACComponent;
                if (tempService == null)
                {
                    //Error50460: The bakery temperature service can not be found!
                    Messages.Error(this, "Error50460");
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

            ACComponent tempMeasureFunc = selectedProcessModule.ACComponentChildsOnServer.FirstOrDefault(c => _BakeryTempMeasuringType.IsAssignableFrom(c.ComponentClass.ObjectType)) as ACComponent;
            if (tempMeasureFunc == null)
            {
                return;
            }

            _TempMeasuringFunc = new ACRef<ACComponent>(tempMeasureFunc, this);

            _ShowOnlyOrderItemsProp = tempMeasureFunc.GetPropertyNet(nameof(PAFBakeryTempMeasuring.ShowOnlyOrderMeasureItems));
            if (_ShowOnlyOrderItemsProp != null)
            {
                ShowOnlyOrderItems = (bool)_ShowOnlyOrderItemsProp.Value;
            }

            if (ParentBSOWCS != null)
                ParentBSOWCS.PropertyChanged += ParentBSOWCS_PropertyChanged;

            string sensorACUrl = TempMeasuringFunc.ExecuteMethod(nameof(PAFBakeryTempMeasuring.GetManualTempMeasurementACUrl)) as string;

            var sensor = Root.ACUrlCommand(sensorACUrl) as ACComponent;
            if (sensor != null)
            {
                _ManualTempMeasurementSensor = new ACRef<ACComponent>(sensor, this);
            }

            MaterialTempMeasureList measureItems = TempMeasuringFunc.ExecuteMethod(nameof(PAFBakeryTempMeasuring.GetTempMeasureItems)) as MaterialTempMeasureList;
            if (measureItems == null)
            {
                return;
            }

            _ChangedItemsProp = TempMeasuringFunc.GetPropertyNet(nameof(PAFBakeryTempMeasuring.ChangedTemperatureMeasureItems)) as IACContainerTNet<MaterialTempMeasureList>;
            if (_ChangedItemsProp == null)
            {
                //Error50461: The property ChangedTemperatureMeasureItems can not be found!
                Messages.Error(this, "Error50461");
                return;
            }

            _ChangedItemsProp.PropertyChanged += ChangedItemsProp_PropertyChanged;

            foreach (var measureItem in measureItems)
            {
                measureItem.AttachToDatabase(DatabaseApp);
            }

            _TemperatureMeasureItemsList = measureItems.OrderBy(c => c.NextMeasureTerm).ToList();
            OnFilterTempMeasurementList();
        }

        public override void DeActivate()
        {
            if (TemperatureServiceProxy != null)
            {
                TemperatureServiceProxy.Detach();
                TemperatureServiceProxy = null;
            }

            if (ParentBSOWCS != null)
                ParentBSOWCS.PropertyChanged -= ParentBSOWCS_PropertyChanged;

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

            if (_ShowOnlyOrderItemsProp != null)
                _ShowOnlyOrderItemsProp = null;

            if (_ChangedItemsProp != null)
            {
                _ChangedItemsProp.PropertyChanged -= ChangedItemsProp_PropertyChanged;
                _ChangedItemsProp = null;
            }

            base.DeActivate();
        }

        private void ParentBSOWCS_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BSOWorkCenterSelector.InputComponentList))
            {
                OnFilterTempMeasurementList();
            }
        }

        private void ChangedItemsProp_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<MaterialTempMeasureList> senderProp = sender as IACContainerTNet<MaterialTempMeasureList>;
                if (senderProp != null)
                {
                    var temp = senderProp.ValueT.ToArray();
                    ParentBSOWCS.ApplicationQueue.Add(() => RefreshChangedItems(temp));
                }
            }
        }

        private void RefreshChangedItems(IEnumerable<MaterialTempMeasureItem> changedItems)
        {
            if (changedItems == null || !changedItems.Any())
                return;

            bool isAnyAddedOrRemoved = false;

            foreach (MaterialTempMeasureItem item in changedItems)
            {
                MaterialTempMeasureItem tempItem = _TemperatureMeasureItemsList.FirstOrDefault(c => c.MaterialConfigID == item.MaterialConfigID);
                if (tempItem == null)
                {
                    item.AttachToDatabase(DatabaseApp);
                    _TemperatureMeasureItemsList.Add(item);
                    isAnyAddedOrRemoved = true;
                }
                else
                {
                    if (item.IsMeasurementOff)
                    {
                        _TemperatureMeasureItemsList.Remove(tempItem);
                        isAnyAddedOrRemoved = true;
                        continue;
                    }

                    tempItem.IsTempMeasureNeeded = item.IsTempMeasureNeeded;
                    tempItem.NextMeasureTerm = item.NextMeasureTerm;
                    tempItem.Temperature = item.Temperature;
                }
            }

            if (isAnyAddedOrRemoved)
                TempMeasureItemsList = _TemperatureMeasureItemsList.OrderBy(c => c.MaterialConfig.Material.MaterialNo).ToList();
        }

        private void TempServiceInfo_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                // 1 Refresh average temperatures
                // 2 Refresh temperatures info
                if (_TemperatureServiceInfo.ValueT == 1)
                {
                    ParentBSOWCS?.ApplicationQueue.Add(() =>
                    {
                        if (MaterialTemperatures == null || TemperatureServiceProxy == null || TemperatureServiceProxy.ValueT == null)
                            return;

                        Guid? acClassID = CurrentProcessModule?.ComponentClass.ACClassID;
                        if (!acClassID.HasValue)
                            return;

                        MaterialTempBaseList result = TemperatureServiceProxy.ValueT.ExecuteMethod(nameof(PABakeryTempService.GetAverageTemperatures), acClassID.Value) as MaterialTempBaseList;
                        if (result != null && result.Any())
                        {
                            foreach (MaterialTemperatureBase tempBase in result)
                            {
                                MaterialTemperature mt = MaterialTemperatures.FirstOrDefault(c => c.MaterialNo == tempBase.MaterialNo);
                                if (mt != null)
                                {
                                    mt.AverageTemperature = tempBase.AverageTemperature;
                                    mt.AverageTemperatureWithOffset = tempBase.AverageTemperatureWithOffset;
                                }
                            }
                        }
                    });
                }
                else if (_TemperatureServiceInfo.ValueT == 2)
                {
                    ParentBSOWCS?.ApplicationQueue.Add(() =>
                    {
                        if (TemperatureServiceProxy == null || TemperatureServiceProxy.ValueT == null)
                            return;

                        Guid? acClassID = CurrentProcessModule?.ComponentClass.ACClassID;
                        if (!acClassID.HasValue)
                            return;

                        ACValueList result = TemperatureServiceProxy.ValueT.ExecuteMethod(nameof(PABakeryTempService.GetTemperaturesInfo), acClassID.Value) as ACValueList;
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



        [ACMethodInfo("", "en{'Measure component temperature'}de{'Komponententemperatur messen'}", 701)]
        public void MeasureComponentTemp()
        {
            TempMeasuringFunc.ExecuteMethod(nameof(PAFBakeryTempMeasuring.MeasureMaterialTemperature), SelectedTempMeasureItem.MaterialConfigID, TemperatureMeasurementOverride);
        }

        public bool IsEnabledMeasureComponentTemp()
        {
            return ParentBSOWCS != null && TempMeasuringFunc != null && SelectedTempMeasureItem != null;
        }

        [ACMethodInfo("", "en{'Delete temperature mesurement'}de{'Temperaturmessung löschen'}", 702)]
        public void DeleteComponentTempMeasurement()
        {
            TempMeasuringFunc.ExecuteMethod(nameof(PAFBakeryTempMeasuring.DeleteMeasurement), SelectedTempMeasureItem.MaterialConfigID);
        }

        public bool IsEnabledDeleteComponentTempMeasurement()
        {
            return ParentBSOWCS != null && TempMeasuringFunc != null && SelectedTempMeasureItem != null;
        }

        //TODO
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
            TempMeasuringFunc.ExecuteMethod(nameof(PAFBakeryTempMeasuring.ChangeHintSetting), false);
        }

        public bool IsEnabledTurnOnMeasurementHint()
        {
            return TempMeasuringFunc != null;
        }

        [ACMethodInfo("", "en{'Turn off measurement hint'}de{'Hinweis zur Messung ausschalten'}", 704)]
        public void TurnOffMeasurementHint()
        {
            TempMeasuringFunc.ExecuteMethod(nameof(PAFBakeryTempMeasuring.ChangeHintSetting), true);
        }

        public bool IsEnabledTurnOffMeasurementHint()
        {
            return TempMeasuringFunc != null;
        }

        private void OnFilterTempMeasurementList()
        {
            if (_TemperatureMeasureItemsList == null)
                return;

            if (ShowOnlyOrderItems)
            {
                if (ParentBSOWCS != null && ParentBSOWCS.InputComponentList != null && ParentBSOWCS.InputComponentList.Any())
                {
                    var tempComps = ParentBSOWCS.InputComponentList;
                    TempMeasureItemsList = _TemperatureMeasureItemsList.Where(c => tempComps.Any(x => c.MaterialConfig.Material.MaterialNo == x.MaterialNo)).OrderBy(c => c.NextMeasureTerm).ToList();
                }
                else
                {
                    TempMeasureItemsList = _TemperatureMeasureItemsList.OrderBy(c => c.NextMeasureTerm).ToList();
                }
            }
            else
            {
                if (ParentBSOWCS != null && ParentBSOWCS.InputComponentList != null && ParentBSOWCS.InputComponentList.Any())
                {
                    List<MaterialTempMeasureItem> tempList = new List<MaterialTempMeasureItem>();

                    var tempComps = ParentBSOWCS.InputComponentList;
                    var tempItemsOnStart = _TemperatureMeasureItemsList.Where(c => tempComps.Any(x => c.MaterialConfig.Material.MaterialNo == x.MaterialNo)).OrderBy(c => c.NextMeasureTerm);
                    var tempItemsOnEnd = _TemperatureMeasureItemsList.Except(tempItemsOnStart).OrderBy(c => c.NextMeasureTerm);

                    tempList.AddRange(tempItemsOnStart);
                    tempList.AddRange(tempItemsOnEnd);

                    TempMeasureItemsList = tempList;
                }
                else
                {
                    TempMeasureItemsList = _TemperatureMeasureItemsList.OrderBy(c => c.NextMeasureTerm).ToList();
                }
            }
        }

        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;

            switch (acMethodName)
            {
                case nameof(MeasureComponentTemp):
                    MeasureComponentTemp();
                    return true;

                case nameof(IsEnabledMeasureComponentTemp):
                    result = IsEnabledMeasureComponentTemp();
                    return true;

                case nameof(DeleteComponentTempMeasurement):
                    DeleteComponentTempMeasurement();
                    return true;

                case nameof(IsEnabledDeleteComponentTempMeasurement):
                    result = IsEnabledDeleteComponentTempMeasurement();
                    return true;

                case nameof(CopyComponentTempMeasurement):
                    CopyComponentTempMeasurement();
                    return true;

                case nameof(IsEnabledCopyComponentTempMeasurement):
                    result = IsEnabledCopyComponentTempMeasurement();
                    return true;

                case nameof(TurnOnMeasurementHint):
                    TurnOnMeasurementHint();
                    return true;

                case nameof(IsEnabledTurnOnMeasurementHint):
                    result = IsEnabledTurnOnMeasurementHint();
                    return true;

                case nameof(TurnOffMeasurementHint):
                    TurnOffMeasurementHint();
                    return true;

                case nameof(IsEnabledTurnOffMeasurementHint):
                    result = IsEnabledTurnOffMeasurementHint();
                    return true;
            }

            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        #endregion
    }
}
