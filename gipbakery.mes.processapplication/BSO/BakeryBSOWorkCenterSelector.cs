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
    [ACClassInfo(Const.PackName_VarioManufacturing, "en{'Bakery workcenter'}de{'Bakerei Arbeitsplatz'}", Global.ACKinds.TACBSO, Global.ACStorableTypes.NotStorable, true, true)]
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

        #endregion
    }
}
