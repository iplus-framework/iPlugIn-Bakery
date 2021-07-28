using gip.core.autocomponent;
using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioSystem, "en{'End on time'}de{'Beenden bei Uhrzeit'}", Global.ACKinds.TPWNodeStatic, Global.ACStorableTypes.Optional, false, PWProcessFunction.PWClassName, true)]
    public class PWBakeryEndOnTime : PWNodeWait
    {
        #region c'tors

        static PWBakeryEndOnTime()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("Duration", typeof(TimeSpan), TimeSpan.Zero, Global.ParamOption.Optional));
            paramTranslation.Add("Duration", "en{'Waitingtime'}de{'Wartezeit'}");

            method.ParameterValueList.Add(new ACValue("DurationMustExpire", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("DurationMustExpire", "en{'Waiting time must elapse'}de{'Wartezeit muss verstreichen'}");

            var wrapper = new ACMethodWrapper(method, "en{'End on time'}de{'Beenden bei Uhrzeit'}", typeof(PWNodeWait), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryEndOnTime), ACStateConst.SMStarting, wrapper);

            RegisterExecuteHandler(typeof(PWBakeryEndOnTime), HandleExecuteACMethod_PWNodeStartAtTimet);
        }

        public PWBakeryEndOnTime(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        #endregion

        #region Properties

        [ACPropertyBindingSource(800, "ACConfig", "en{'End on time'}de{'Beenden bei Uhrzeit'}", "", false, true)]
        public IACContainerTNet<DateTime> EndOnTime { get; set; }

        protected bool DurationMustExpire
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("DurationMustExpire");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }

        #endregion

        #region Methods

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            if (DurationMustExpire)
            {
                base.SMStarting();
            }
            else
            {
                var newMethod = NewACMethodWithConfiguration();
                CreateNewProgramLog(newMethod, true);
                CurrentACState = ACStateEnum.SMRunning;
            }

            PWBakeryGroupFermentation fermentationGroup = ParentPWGroup as PWBakeryGroupFermentation;
            if (fermentationGroup != null)
            {
                fermentationGroup.OnChildPWBakeryEndOnTimeStart(this);
            }
        }

        protected override void objectManager_ProjectTimerCycle200ms(object sender, EventArgs e)
        {
            if (DurationMustExpire)
            {
                base.objectManager_ProjectTimerCycle200ms(sender, e);
            }
            else
            {
                if (this.InitState == ACInitState.Destructed || this.InitState == ACInitState.DisposingToPool || this.InitState == ACInitState.DisposedToPool)
                {
                    gip.core.datamodel.Database.Root.Messages.LogError("PWNodeWait", "objectManager_ProjectTimerCycle200ms(1)", String.Format("Unsubcribed from Workcycle. Init-State is {0}, _SubscribedToTimerCycle is {1}, at Type {2}. Ensure that you unsubscribe from Work-Cycle in ACDeinit().", this.InitState, _SubscribedToTimerCycle, this.GetType().AssemblyQualifiedName));

                    using (ACMonitor.Lock(_20015_LockValue))
                    {
                        (sender as ApplicationManager).ProjectWorkCycleR1sec -= objectManager_ProjectTimerCycle200ms;
                        _SubscribedToTimerCycle = false;
                    }
                    return;
                }

                if ((EndOnTime.ValueT < DateTime.Now || EndOnTime.ValueT == DateTime.MinValue)
                     && CurrentACState >= ACStateEnum.SMStarting
                     && CurrentACState <= ACStateEnum.SMCompleted)
                {
                    CurrentACState = ACStateEnum.SMCompleted;
                }
            }
        }

        public static bool HandleExecuteACMethod_PWNodeStartAtTimet(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PWNodeWait(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

        #endregion
    }
}
