using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
using gip.mes.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Cyclic temperature measuring'}de{'Zyklische Temperaturmessung'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, true, "", BSOConfig = BakeryBSOTemperature.ClassName, SortIndex = 200)]
    public class PAFBakeryTempMeasuring : PAProcessFunction
    {
        #region c'tors

        public PAFBakeryTempMeasuring(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
            _ManualTempMeasurementSensorACUrl = new ACPropertyConfigValue<string>(this, "ManualTempMeasurementSensorACUrl", "");
        }

        public override bool ACInit(Global.ACStartTypes startChildMode = Global.ACStartTypes.Automatic)
        {
            return base.ACInit(startChildMode);
        }

        public override bool ACPostInit()
        {
            bool result = FindTemperatureMeasureSensor();
            if (!result)
                return result;

            result = base.ACPostInit();
            if (!result)
                return result;

            RefreshMeasureItems();

            return result;
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            return base.ACDeInit(deleteACClassTask);
        }

        #endregion

        #region Properties

        private ACPropertyConfigValue<string> _ManualTempMeasurementSensorACUrl;
        [ACPropertyConfig("en{'Sensor ACUrl for the manual temperature measurement'}de{'Sensor ACUrl für die manuelle Temperaturmessung'}")]
        public string ManualTempMeasurementSensorACUrl
        {
            get => _ManualTempMeasurementSensorACUrl.ValueT;
            set
            {
                _ManualTempMeasurementSensorACUrl.ValueT = value;
                OnPropertyChanged("ManualTempMeasurementSensorACUrl");
            }
        }

        private bool _IsTemperatureMeasurementActive = false;

        [ACPropertyInfo(true, 9999)]
        public bool NotificationsOff
        {
            get;
            set;
        }

        public PAEThermometer ManualTempMeasurementSensor
        {
            get;
            set;
        }

        [ACPropertyInfo(true, 9999)]
        public double? TempMeasurementValue
        {
            get;
            set;
        }

        [ACPropertyInfo(850)]
        public MaterialTempMeasureList MaterialTemperatureMeasureItems
        {
            get;
            set;
        }

        [ACPropertyBindingSource]
        public IACContainerTNet<MaterialTempMeasureList> ChangedTemperatureMeasureItems
        {
            get;
            set;
        }

        #endregion

        #region Methods

        #region Methods => ACState

        public override void SMStarting()
        {
        }

        public override void SMRunning()
        {
        }

        public override void SMCompleted()
        {
            base.SMCompleted();
        }

        #endregion

        [ACMethodInfo("", "", 801)]
        public MaterialTempMeasureList GetTempMeasureItems()
        {
            return MaterialTemperatureMeasureItems;
        }

        [ACMethodInfo("", "", 802)]
        public void MeasureMaterialTemperature(Guid materialConfig)
        {
            ApplicationManager?.ApplicationQueue.Add(() => MeasureMatTemp(materialConfig));
        }

        private void MeasureMatTemp(Guid materialConfigID)
        {
            MaterialTempMeasureItem measureItem = null;
            using (ACMonitor.Lock(_20015_LockValue))
            {
                measureItem = MaterialTemperatureMeasureItems.FirstOrDefault(c => c.MaterialConfigID == materialConfigID);
            }

            if (measureItem == null)
                return;

            using (DatabaseApp dbApp = new DatabaseApp())
            {
                MaterialConfig item = measureItem.MaterialConfig.FromAppContext<MaterialConfig>(dbApp);
                if (item == null)
                    return; //TODO:error

                double? temperature = TempMeasurementValue.HasValue ? TempMeasurementValue.Value : ManualTempMeasurementSensor?.ActualValue.ValueT;
                if (!temperature.HasValue)
                {
                    //TODO error
                    return;
                }

                item.Value = temperature;

                //TODO: error
                dbApp.ACSaveChanges();

                measureItem.IsTempMeasureNeeded = false;
                measureItem.SetLastMeasureTime(item.UpdateDate);
                measureItem.Temperature = temperature.Value;
            }

            MaterialTempMeasureList changedItems = new MaterialTempMeasureList();
            changedItems.Add(measureItem);

            bool notificationsOff = false;
            using (ACMonitor.Lock(_20015_LockValue))
            {
                ChangedTemperatureMeasureItems.ValueT = changedItems;
                notificationsOff = NotificationsOff;
            }

            if (!notificationsOff)
            {
                bool isActive = MaterialTemperatureMeasureItems.Any(c => c.IsTempMeasureNeeded);
                if (isActive)
                    CurrentACState = ACStateEnum.SMRunning;
                else
                    CurrentACState = ACStateEnum.SMIdle;
            }
            
        }

        [ACMethodInfo("", "", 803)]
        public void DeactivateMeasurement(Guid materialConfig)
        {
            ApplicationManager?.ApplicationQueue.Add(() => DeactivateMatMeasurement(materialConfig));
        }

        private void DeactivateMatMeasurement(Guid materialConfigID)
        {
            MaterialTempMeasureItem measureItem = null;
            using (ACMonitor.Lock(_20015_LockValue))
            {
                measureItem = MaterialTemperatureMeasureItems.FirstOrDefault(c => c.MaterialConfigID == materialConfigID);
            }

            if (measureItem == null)
                return;

            using (DatabaseApp dbApp = new DatabaseApp())
            {
                MaterialConfig item = measureItem.MaterialConfig.FromAppContext<MaterialConfig>(dbApp);
                if (item == null)
                    return; //TODO:error

                item.Expression = TempMeasurementModeEnum.Off.ToString();
                Msg msg = dbApp.ACSaveChanges(); //TODO: error

                measureItem.IsMeasurementOff = true;
            }

            MaterialTempMeasureList changedItems = new MaterialTempMeasureList();
            changedItems.Add(measureItem);

            using (ACMonitor.Lock(_20015_LockValue))
            {
                MaterialTemperatureMeasureItems.Remove(measureItem);
                ChangedTemperatureMeasureItems.ValueT = changedItems;
            }
        }

        [ACMethodInfo("", "", 804)]
        public string GetManualTempMeasurementACUrl()
        {
            return ManualTempMeasurementSensor?.ACUrl;
        }

        [ACMethodInfo("", "", 805)]
        public void ReactivateMeasurements()
        {
            ApplicationManager?.ApplicationQueue.Add(() => ReactivateMatMeasurement());
        }

        private void ReactivateMatMeasurement()
        {
            if (ParentACComponent == null)
                return;

            using(DatabaseApp dbApp = new DatabaseApp())
            {
                string offMode = TempMeasurementModeEnum.Off.ToString();
                IEnumerable<MaterialConfig> materialConfigs = dbApp.MaterialConfig.Where(c => c.VBiACClassID == ParentACComponent.ComponentClass.ACClassID
                                                                                           && c.Expression == offMode);

                foreach(MaterialConfig matConf in materialConfigs)
                {
                    matConf.Expression = TempMeasurementModeEnum.On.ToString();
                }

                //TODO:error
                Msg msg = dbApp.ACSaveChanges();
            }
        }

        [ACMethodInfo("", "", 806)]
        public void TurnOffNotifications()
        {
            using (ACMonitor.Lock(_20015_LockValue))
            {
                NotificationsOff = true;
            }
        }

        //TODO: reset off
        internal void RefreshMeasureItems()
        {
            using (DatabaseApp dbApp = new DatabaseApp())
            {
                var items = dbApp.MaterialConfig.Where(c => c.VBiACClassID == ParentACComponent.ComponentClass.ACClassID && c.KeyACUrl == PABakeryTempService.MaterialTempertureConfigKeyACUrl
                                                         && c.Expression != null)
                                                .ToArray()
                                                .Where(k => k.Expression != TempMeasurementModeEnum.Off.ToString())
                                                .Select(x => new MaterialTempMeasureItem(x));

                MaterialTempMeasureList measureItems = new MaterialTempMeasureList(items);

                using (ACMonitor.Lock(_20015_LockValue))
                {
                    MaterialTemperatureMeasureItems = measureItems;
                }

                if (measureItems != null && measureItems.Any() && !_IsTemperatureMeasurementActive)
                {
                    if (ApplicationManager != null)
                    {
                        PeriodicalTempMeasureCheck();
                        ApplicationManager.ProjectWorkCycleR1min += ApplicationManager_ProjectWorkCycleR1min;
                        _IsTemperatureMeasurementActive = true;
                    }
                }
                else if (measureItems == null || !measureItems.Any())
                {
                    if (ApplicationManager != null)
                    {
                        ApplicationManager.ProjectWorkCycleR1min -= ApplicationManager_ProjectWorkCycleR1min;
                        _IsTemperatureMeasurementActive = false;
                    }
                }
            }
        }

        private void ApplicationManager_ProjectWorkCycleR1min(object sender, EventArgs e)
        {
            ApplicationManager.ApplicationQueue.Add(() => PeriodicalTempMeasureCheck());
        }

        private void PeriodicalTempMeasureCheck()
        {
            IEnumerable<MaterialTempMeasureItem> measurableItems = null;
            using (ACMonitor.Lock(_20015_LockValue))
            {
                measurableItems = MaterialTemperatureMeasureItems.ToArray();
            }

            if (measurableItems == null || !measurableItems.Any())
                return;

            DateTime now = DateTime.Now;

            MaterialTempMeasureList changedItems = new MaterialTempMeasureList();

            foreach (MaterialTempMeasureItem measureItem in measurableItems)
            {
                if (measureItem.NextMeasureTerm < now)
                {
                    if (!measureItem.IsTempMeasureNeeded)
                    {
                        measureItem.IsTempMeasureNeeded = true;
                        changedItems.Add(measureItem);
                    }
                }
                else
                {
                    if (measureItem.IsTempMeasureNeeded)
                    {
                        measureItem.IsTempMeasureNeeded = false;
                        changedItems.Add(measureItem);
                    }
                }
            }

            if (changedItems.Any())
            {
                bool notificationsOff = false;

                using (ACMonitor.Lock(_20015_LockValue))
                {
                    MaterialTemperatureMeasureItems = new MaterialTempMeasureList(measurableItems);
                    ChangedTemperatureMeasureItems.ValueT = changedItems;
                    notificationsOff = NotificationsOff;
                }

                if (!notificationsOff)
                {
                    bool isActive = MaterialTemperatureMeasureItems.Any(c => c.IsTempMeasureNeeded);
                    if (isActive)
                        CurrentACState = ACStateEnum.SMRunning;
                    else
                        CurrentACState = ACStateEnum.SMIdle;
                }
            }
        }

        private bool FindTemperatureMeasureSensor()
        {
            string temp = ManualTempMeasurementSensorACUrl;
            if (!string.IsNullOrEmpty(temp))
                ManualTempMeasurementSensor = ACUrlCommand(temp) as PAEThermometer;

            if (ManualTempMeasurementSensor == null)
            {
                ManualTempMeasurementSensor = FindChildComponents<PAEThermometer>().FirstOrDefault();
            }

            if (ManualTempMeasurementSensor == null)
                return false;

            return true;
        }

        #endregion
    }
}
