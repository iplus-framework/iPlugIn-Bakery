using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
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
    public class BakeryRecvPointTemperature
    {
        public BakeryRecvPointTemperature(PABakeryTempService tempService)
        {
            MaterialTempInfos = new List<MaterialTemperature>();
            TemperatureService = tempService;
        }

        public PABakeryTempService TemperatureService
        {
            get;
            set;
        }

        public void AddSilo(BakerySilo silo)
        {
            if (silo == null || silo.MaterialNo == null)
                return;

            var materialNo = silo.MaterialNo.ValueT;
            if (string.IsNullOrEmpty(materialNo))
            {
                var silosWithoutMaterial = MaterialTempInfos.FirstOrDefault(c => string.IsNullOrEmpty(c.MaterialNo));
                if (silosWithoutMaterial == null)
                {
                    silosWithoutMaterial = new MaterialTemperature();
                    MaterialTempInfos.Add(silosWithoutMaterial);
                }

                silosWithoutMaterial.AddSilo(silo);
            }
            else
            {
                var targetMaterialInfo = MaterialTempInfos.FirstOrDefault(c => c.MaterialNo == materialNo);
                if (targetMaterialInfo == null)
                {
                    using (gip.mes.datamodel.DatabaseApp dbApp = new gip.mes.datamodel.DatabaseApp())
                    {
                        targetMaterialInfo = new MaterialTemperature();
                        targetMaterialInfo.MaterialNo = materialNo;
                        targetMaterialInfo.Material = dbApp.Material.Include(x => x.MaterialConfig_Material).FirstOrDefault(c => c.MaterialNo == materialNo);
                        targetMaterialInfo.Water = WaterType.NotWater;
                        MaterialTempInfos.Add(targetMaterialInfo);
                    }
                }

                targetMaterialInfo.AddSilo(silo);
            }
        }

        internal void SiloMaterialNoPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                OnSiloMaterialNoChanged(sender, e);

                ACPropertyNet<string> materialNoProp = sender as ACPropertyNet<string>;
                if (materialNoProp != null)
                {
                    TemperatureService.SetTempServiceInfo(0);

                    var silo = materialNoProp.ParentACComponent as BakerySilo;
                    if (silo != null)
                    {
                        var affectedMaterialTemperatures = MaterialTempInfos.Where(c => c.SilosWithPTC.Contains(silo)).ToList();

                        foreach (var item in affectedMaterialTemperatures)
                        {
                            string materialNo = silo.MaterialNo.ValueT;

                            if (item.MaterialNo == materialNo)
                                continue;

                            item.RemoveSilo(silo);

                            if (!item.BakeryThermometersACUrls.Any())
                            {
                                MaterialTempInfos.Remove(item);
                            }

                            var mt = MaterialTempInfos.FirstOrDefault(c => c.MaterialNo == materialNo);
                            if (mt == null)
                            {
                                using (DatabaseApp dbApp = new DatabaseApp())
                                {
                                    mt = new MaterialTemperature();
                                    mt.MaterialNo = materialNo;
                                    mt.Material = dbApp.Material.Include(c => c.MaterialConfig_Material).FirstOrDefault(c => c.MaterialNo == materialNo);
                                    mt.Water = WaterType.NotWater;
                                    MaterialTempInfos.Add(mt);
                                }
                            }

                            mt.AddSilo(silo);
                        }
                    }
                }
            }
        }

        public virtual void OnSiloMaterialNoChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

        }

        public List<MaterialTemperature> MaterialTempInfos
        {
            get;
            set;
        }

        public void RecalculateAverageTemperature(BakeryReceivingPoint recvPoint)
        {
            if (MaterialTempInfos == null)
                return;

            foreach (MaterialTemperature mt in MaterialTempInfos)
            {
                if (mt.IsRoomTemperature)
                {
                    mt.AverageTemperature = recvPoint.RoomTemperature.ValueT;
                }
                else
                {
                    mt.CalculateAverageTemperature(recvPoint.RoomTemperature.ValueT);
                }
            }
        }

        public void AddRoomTemperature(BakeryReceivingPoint recvPoint)
        {
            OnAddRoomTemperature(recvPoint);

            MaterialTemperature mt = new MaterialTemperature();
            mt.IsRoomTemperature = true;
            mt.Water = WaterType.NotWater;
            mt.MaterialNo = TemperatureService.Root.Environment.TranslateText(TemperatureService, "RoomTemp");
            mt.AverageTemperature = recvPoint.RoomTemperature.ValueT;
            MaterialTempInfos.Add(mt);
        }

        public virtual void OnAddRoomTemperature(BakeryReceivingPoint recvPoint)
        {

        }

        public void DeInit()
        {
            foreach (var mt in MaterialTempInfos)
            {
                foreach (var silo in mt.SilosWithPTC)
                {
                    silo.MaterialNo.PropertyChanged -= SiloMaterialNoPropertyChanged;
                }
            }
        }
    }
}
