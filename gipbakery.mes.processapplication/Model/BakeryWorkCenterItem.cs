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
    [ACClassInfo(Const.PackName_VarioManufacturing, "en{'WorkCenterItem Bakery'}de{'WorkCenterItem Bakery'}", Global.ACKinds.TACSimpleClass, Global.ACStorableTypes.NotStorable, true, true)]
    public class BakeryWorkCenterItem : WorkCenterItem
    {
        public BakeryWorkCenterItem(ACComponent processModule, BSOWorkCenterSelector bso) : base(processModule, bso)
        {
        }

        public override void DeInit()
        {
            if (_RefPAFPumping != null)
            {
                _RefPAFPumping.Detach();
                _RefPAFPumping = null;
            }

            base.DeInit();
        }

        private ACRef<ACComponent> _RefPAFPumping;

        [ACPropertyInfo(9999)]
        public ACComponent PAFPumping
        {
            get
            {
                if (_RefPAFPumping != null)
                    return _RefPAFPumping.ValueT;
                return null;
            }
            set
            {
                if (_RefPAFPumping != null)
                    _RefPAFPumping.Detach();

                _RefPAFPumping = new ACRef<ACComponent>(value, this);
            }
        }

        [ACPropertyInfo(100)]
        public IACPropertyNetBase PAFPumpingACState
        {
            get
            {
                if (PAFPumping != null)
                {
                    return PAFPumping.GetPropertyNet(nameof(PAProcessFunction.ACState));
                }
                return null;
            }
        }
    }
}
