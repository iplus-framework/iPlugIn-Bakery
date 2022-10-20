using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioSystem, "en{'Wait until watertemp is measured'}de{'Warten bis Wassertemperaturen gemessen'}", Global.ACKinds.TPWNodeStatic, Global.ACStorableTypes.Optional, false, PWProcessFunction.PWClassName, true)]
    public class PWBakeryWaitWaterM : PWNodeWait
    {
        #region c'tors

        static PWBakeryWaitWaterM()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();
            method.ParameterValueList.Add(new ACValue("Duration", typeof(TimeSpan), TimeSpan.Zero, Global.ParamOption.Required));
            paramTranslation.Add("Duration", "en{'Waitingtime'}de{'Wartezeit'}");
            var wrapper = new ACMethodWrapper(method, "en{'Wait'}de{'Warten'}", typeof(PWBakeryWaitWaterM), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryWaitWaterM), ACStateConst.SMStarting, wrapper);

            RegisterExecuteHandler(typeof(PWBakeryWaitWaterM), HandleExecuteACMethod_PWBakeryWaitWaterM);
        }

        public PWBakeryWaitWaterM(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }
        #endregion

        #region Properties

        #endregion

        #region Methods

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            _CheckWaterReadyCounter = 0;
            BakeryReceivingPoint receivingPoint = ReceivingPoint;
            if (   receivingPoint != null 
                && Duration > TimeSpan.Zero)
            {
                if (receivingPoint.WaitIfMeasureWaterIsBound(this))
                {
                    base.SMStarting();
                    return;
                }
            }

            if (CurrentACState == ACStateEnum.SMStarting)
                CurrentACState = ACStateEnum.SMCompleted;
            return;

        }

        public BakeryReceivingPoint ReceivingPoint
        {
            get
            {
                if (ParentPWGroup == null || ParentPWGroup.AccessedProcessModule == null)
                    return null;
                return ParentPWGroup.AccessedProcessModule as BakeryReceivingPoint;
            }
        }

        int _CheckWaterReadyCounter = 0;
        protected override void objectManager_ProjectTimerCycle200ms(object sender, EventArgs e)
        {
            _CheckWaterReadyCounter++;
            if (_CheckWaterReadyCounter > 5)
            {
                _CheckWaterReadyCounter = 0;
                BakeryReceivingPoint receivingPoint = ReceivingPoint;
                if (   receivingPoint == null 
                    || receivingPoint.MeasureWaterOnNewBatch.ValueT == false)
                {
                    CancelWaiting();
                    return;
                }
            }
            base.objectManager_ProjectTimerCycle200ms(sender, e);
        }

        public static bool HandleExecuteACMethod_PWBakeryWaitWaterM(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PWNodeWait(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

        #endregion
    }
}
