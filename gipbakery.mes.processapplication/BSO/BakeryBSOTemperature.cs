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
        }
    }
}
