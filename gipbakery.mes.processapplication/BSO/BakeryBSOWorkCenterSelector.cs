using gip.bso.manufacturing;
using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using gip.mes.datamodel;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioManufacturing, "en{'Bakery workcenter'}de{'Bäckerei Arbeitsplatz'}", Global.ACKinds.TACBSO, Global.ACStorableTypes.NotStorable, true, true)]
    public class BakeryBSOWorkCenterSelector : BSOWorkCenterSelector
    {
        #region c'tors

        public BakeryBSOWorkCenterSelector(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACPostInit()
        {
            bool result = base.ACPostInit();

            if (string.IsNullOrEmpty(BakeryTemperatureServiceACUrl))
                BakeryTemperatureServiceACUrl = "\\Service\\BakeryTempService";

            return result;
        }

        #endregion

        #region Properties

        private Type _PAFBakeryYeastProdType = typeof(PAFBakeryYeastProducing);
        private Type _PAFBakeryPumpingType = typeof(PAFBakeryPumping);

        [ACPropertyInfo(true, 800)]
        public string BakeryTemperatureServiceACUrl
        {
            get;
            set;
        }

        #endregion

        #region Methods

        protected override void OnInputComponentCreated(InputComponentItem item, ProdOrderPartslistPosRelation relation, DatabaseApp dbApp)
        {
            using (ACMonitor.Lock(dbApp.QueryLock_1X000))
            {
                relation?.SourceProdOrderPartslistPos?.Material.MaterialConfig_Material.AutoRefresh();
            }

            MaterialConfig temp = relation?.SourceProdOrderPartslistPos?.Material
                                           .MaterialConfig_Material.FirstOrDefault(c => c.VBiACClassID == CurrentProcessModule.ComponentClass.ACClassID
                                                                                     && c.KeyACUrl == PABakeryTempService.MaterialTempertureConfigKeyACUrl);

            if (temp != null && temp.Value != null)
            {
                item.AdditionalParam1 = temp.Value.ToString() + "°C";
            }
        }

        public static object GetConfigValue(gip.core.datamodel.ACClass acClass, string configName)
        {
            if (acClass == null || string.IsNullOrEmpty(configName))
                return null;

            var config = acClass.ConfigurationEntries.FirstOrDefault(c => c.KeyACUrl == acClass.ACConfigKeyACUrl && c.LocalConfigACUrl == configName);
            if (config == null)
                return null;

            return config.Value;
        }

        public override WorkCenterItem CreateWorkCenterItem(ACComponent processModule, BSOWorkCenterSelector workCenterSelector)
        {
            return new BakeryWorkCenterItem(processModule, workCenterSelector);
        }

        public override ACComposition[] OnAddFunctionBSOs(gip.core.datamodel.ACClass pafACClass, ACComposition[] bsos, WorkCenterItem workCenterItem)
        {
            if (_PAFBakeryYeastProdType.IsAssignableFrom(pafACClass.ObjectType))
            {
                string pumpOverModuleACUrl = GetConfigValue(pafACClass, nameof(PAFBakeryYeastProducing.PumpOverProcessModuleACUrl)) as string;
                if (!string.IsNullOrEmpty(pumpOverModuleACUrl))
                {
                    ACComponent pumpOverProcessModule = Root.ACUrlCommand(pumpOverModuleACUrl) as ACComponent;
                    if (pumpOverProcessModule != null)
                    {
                        //PumpOverProcessModule = pumpOverProcessModule;

                        var pumpOverChildInstances = pumpOverProcessModule.GetChildInstanceInfo(1, false);

                        ACChildInstanceInfo func = pumpOverChildInstances.FirstOrDefault(c => _PAFBakeryPumpingType.IsAssignableFrom(c.ACType.ValueT.ObjectType));
                        if (func != null)
                        {
                            ACComponent funcComp = pumpOverProcessModule.ACUrlCommand(func.ACIdentifier) as ACComponent;
                            if (funcComp != null)
                            {
                                BakeryWorkCenterItem bwc = workCenterItem as BakeryWorkCenterItem;
                                if (bwc != null)
                                {
                                    bwc.PAFPumping = funcComp;
                                }

                            }
                        }
                    }
                    else
                    {
                        Messages.LogError(this.GetACUrl(), "InitBSO(30)", "The process module for pumping is null. ACUrl: " + pumpOverModuleACUrl);
                    }
                }


            }

            return base.OnAddFunctionBSOs(pafACClass, bsos, workCenterItem);
        }

        #endregion
    }
}
