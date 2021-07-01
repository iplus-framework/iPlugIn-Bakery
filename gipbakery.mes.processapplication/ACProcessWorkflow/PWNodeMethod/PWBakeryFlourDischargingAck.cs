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
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Flour discharging acknowledge'}de{'Mehlaustragsquittung'}", Global.ACKinds.TPWNodeStatic, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryFlourDischargingAck : PWNodeUserAck
    {
        public PWBakeryFlourDischargingAck(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        private IACContainerTNet<bool> _IsCoverDown;

        public override void SMStarting()
        {
            base.SMStarting();
        }

        public override void SMRunning()
        {
            BakeryReceivingPoint recvPoint = ParentPWGroup?.AccessedProcessModule as BakeryReceivingPoint;
            if (recvPoint == null)
            {
                base.SMRunning();
                UnSubscribeToProjectWorkCycle();
                return;
            }

            IACPropertyNetTarget isCoverDown = recvPoint.IsCoverDown as IACPropertyNetTarget;
            if (isCoverDown == null || isCoverDown.Source == null)
            {
                base.SMRunning();
                UnSubscribeToProjectWorkCycle();
                return;
            }

            _IsCoverDown = isCoverDown as IACContainerTNet<bool>;
            if (_IsCoverDown.ValueT)
            {
                AckStart();
                return;
            }

            _IsCoverDown.PropertyChanged += _IsCoverDown_PropertyChanged;
        }

        public override void SMIdle()
        {
            base.SMIdle();
            ResetMembers();
        }

        public override void Recycle(IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
        {
            ResetMembers();
            base.Recycle(content, parentACObject, parameter, acIdentifier);
        }

        private void _IsCoverDown_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                if (_IsCoverDown != null && _IsCoverDown.ValueT)
                {
                    AckStart();
                }
            }
        }

        public override void AckStart()
        {
            if (_IsCoverDown == null || _IsCoverDown.ValueT)
                base.AckStart();
        }

        public void ResetMembers()
        {
            if (_IsCoverDown != null)
            {
                _IsCoverDown.PropertyChanged -= _IsCoverDown_PropertyChanged;
                _IsCoverDown = null;
            }
        }
    }
}
