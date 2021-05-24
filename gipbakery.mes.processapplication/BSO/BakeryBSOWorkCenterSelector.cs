using gip.bso.manufacturing;
using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioManufacturing, "en{'Bakery workcenter'}de{'Bakerei Arbeitsplatz'}", Global.ACKinds.TACBSO, Global.ACStorableTypes.NotStorable, true, true)]
    public class BakeryBSOWorkCenterSelector : BSOWorkCenterSelector
    {
        public BakeryBSOWorkCenterSelector(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
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

        //private Type _PAFManualWeighingType = typeof(PAFManualWeighing);

        [ACPropertyInfo(true, 800)]
        public string BakeryTemperatureServiceACUrl
        {
            get;
            set;
        }

        public override void OnWorkcenterItemSelected(WorkCenterItem item, ref string dynamicContent)
        {
            if (typeof(BakeryReceivingPoint).IsAssignableFrom(item.ProcessModule.ComponentClass.ObjectType))
            {
                ACBSO acBSO = ACComponentChilds.FirstOrDefault(c => c.ACIdentifier.StartsWith(BakeryBSOTemperature.ClassName)) as ACBSO;
                if (acBSO == null)
                {
                    ACClass bsoBakeryTemp = DatabaseApp.ContextIPlus.ACClass.FirstOrDefault(c => c.ACIdentifier == BakeryBSOTemperature.ClassName);
                    if (bsoBakeryTemp != null)
                        acBSO = StartComponent(bsoBakeryTemp, null, null) as ACBSO;
                }

                BSOWorkCenterChild selectorChild = acBSO as BSOWorkCenterChild;
                if (selectorChild == null)
                    return;

                if (item.DefaultTabItemLayout != null)
                    dynamicContent += item.DefaultTabItemLayout.Replace("[childBSO]", acBSO.ACIdentifier).Replace("[tabItemHeader]", acBSO.ACCaption);
            }
        }

        //public override ACComposition[] OnAddFunctionBSOs(ACClass pafACClass, ACComposition[] bsos)
        //{
        //    //if (_PAFManualWeighingType.IsAssignableFrom(pafACClass.ObjectType))
        //    //{
        //    //    var manWeighBso = bsos.FirstOrDefault(c => (c.ValueT as ACClass).ObjectType == BSOManualWeighingType);
        //    //    if(manWeighBso != null)
        //    //    {
        //    //        DatabaseApp.ContextIPlus.ACClass.FirstOrDefault(c => c.ACIdentifier == "BakeryBSOManualWeighing");
        //    //    }
        //    //}
        //    return base.OnAddFunctionBSOs(pafACClass, bsos);
        //}
    }
}
