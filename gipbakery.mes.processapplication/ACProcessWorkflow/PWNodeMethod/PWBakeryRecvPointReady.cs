using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Receiving point ready'}de{'Abnahmestelle bereit'}", Global.ACKinds.TPWNodeStatic, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryRecvPointReady : PWNodeUserAck
    {
        static PWBakeryRecvPointReady()
        {
            RegisterExecuteHandler(typeof(PWBakeryRecvPointReady), HandleExecuteACMethod_PWBakeryRecvPointReady);

            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("MessageText", typeof(string), "", Global.ParamOption.Required));
            paramTranslation.Add("MessageText", "en{'Question text'}de{'Abfragetext'}");

            method.ParameterValueList.Add(new ACValue("AckOverScale", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("AckOverScale", "en{'Acknowledge over scale target weight'}de{'Quittieren über Waagenzielgewicht'}");

            method.ParameterValueList.Add(new ACValue("AckScaleWeight", typeof(double), 0, Global.ParamOption.Optional));
            paramTranslation.Add("AckScaleWeight", "en{'Scale weight for auto acknowledge [kg]'}de{'Waagengewicht für automatische Quittierung [kg]'}");

            var wrapper = new ACMethodWrapper(method, "en{'Receiving point ready'}de{'Abnahmestelle bereit'}", typeof(PWBakeryRecvPointReady), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryRecvPointReady), ACStateConst.SMStarting, wrapper);
        }

        public PWBakeryRecvPointReady(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        protected bool AckOverScale
        {
            get
            {
                var method = MyConfiguration;
                if (method == null)
                    return false;

                ACValue acValue = method.ParameterValueList.GetACValue("AckOverScale");
                if (acValue == null)
                    return false;

                return acValue.ParamAsBoolean;
            }
        }

        protected double AckScaleWeight
        {
            get
            {
                var method = MyConfiguration;
                if (method == null)
                    return 0;

                ACValue acValue = method.ParameterValueList.GetACValue("AckScaleWeight");
                if (acValue == null)
                    return 0;

                return acValue.ParamAsDouble;
            }
        }

        private PAEScaleBase _AckScale;

        public override void Start()
        {
            base.Start();
        }

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        public override void SMRunning()
        {
            if (AckOverScale && AckScaleWeight > 0.0000001 && _AckScale == null)
            {
                BakeryReceivingPoint recvPoint = ParentPWGroup?.AccessedProcessModule as BakeryReceivingPoint;
                if (recvPoint != null)
                {
                    PAEScaleBase scale = recvPoint.GetRecvPointReadyScale();
                    if (scale != null)
                    {
                        if ((scale.MaxScaleWeight.ValueT > 0.00001 && scale.MaxScaleWeight.ValueT < 0.00001) && scale.MaxScaleWeight.ValueT < AckScaleWeight)
                        {
                            //The maximum scale weight is too low. Acknowledge scale weight is {0} and maximum scale weight is {1}. 
                        }
                        else
                        {
                            _AckScale = scale;
                            _AckScale.ActualValue.PropertyChanged += ActualValue_PropertyChanged;
                        }
                    }
                }
                UnSubscribeToProjectWorkCycle();
            }

            base.SMRunning();
        }

        private void ActualValue_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                if (_AckScale.ActualValue.ValueT >= AckScaleWeight)
                {
                    AckStart(); //TODO:calming time
                }
            }
        }

        public override void SMIdle()
        {
            ResetMembers();
            base.SMIdle();
        }

        public override void Recycle(IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
        {
            base.Recycle(content, parentACObject, parameter, acIdentifier);
            ResetMembers();
        }

        private void ResetMembers()
        {
            if (_AckScale != null)
            {
                _AckScale.ActualValue.PropertyChanged -= ActualValue_PropertyChanged;
                _AckScale = null;
            }
        }

        public static bool HandleExecuteACMethod_PWBakeryRecvPointReady(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
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
