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
        static PWBakeryFlourDischargingAck()
        {
            RegisterExecuteHandler(typeof(PWBakeryFlourDischargingAck), HandleExecuteACMethod_PWBakeryFlourDischargingAck);

            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("MessageText", typeof(string), "", Global.ParamOption.Required));
            paramTranslation.Add("MessageText", "en{'Question text'}de{'Abfragetext'}");

            var wrapper = new ACMethodWrapper(method, "en{'Flour discharging acknowledge'}de{'Mehlaustragsquittung'}", typeof(PWBakeryFlourDischargingAck), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryFlourDischargingAck), ACStateConst.SMStarting, wrapper);
        }

        public new const string PWClassName = "PWBakeryFlourDischargingAck";

        public PWBakeryFlourDischargingAck(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        private IACContainerTNet<bool> _IsCoverDown;

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
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
            base.SMRunning();
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

        public static bool HandleExecuteACMethod_PWBakeryFlourDischargingAck(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            //switch (acMethodName)
            //{
            //    case MN_AckStartClient:
            //        AckStartClient(acComponent);
            //        return true;
            //    case Const.IsEnabledPrefix + MN_AckStartClient:
            //        result = IsEnabledAckStartClient(acComponent);
            //        return true;
            //}
            return HandleExecuteACMethod_PWNodeUserAck(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

    }
}
