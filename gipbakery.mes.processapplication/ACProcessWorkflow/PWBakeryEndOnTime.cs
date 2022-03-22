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

        public override bool ACPostInit()
        {
            bool result = base.ACPostInit();

            EndOnTimeNew = EndOnTimeSafe;

            return result;
        }

        #endregion

        #region Properties

        private ACMonitorObject _65100_MembersLock = new ACMonitorObject(65100);

        [ACPropertyBindingSource(800, "ACConfig", "en{'End on time'}de{'Beenden bei Uhrzeit'}", "", false, true)]
        public IACContainerTNet<DateTime> EndOnTime { get; set; }

        public DateTime EndOnTimeSafe
        {
            get
            {
                using (ACMonitor.Lock(_65100_MembersLock))
                {
                    return EndOnTime.ValueT;
                }
            }
        }

        [ACPropertyInfo(801, "", "en{'End on time new'}de{'Beenden bei Uhrzeit neu'}", "", true)]
        public DateTime EndOnTimeNew
        {
            get;
            set;
        }

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
            var pwGroup = ParentPWGroup;
            if (pwGroup == null) // Is null when Service-Application is shutting down
            {
                if (this.InitState == ACInitState.Initialized)
                    Messages.LogError(this.GetACUrl(), "SMStarting()", "ParentPWGroup is null");
                return;
            }

            if (pwGroup.IsPWGroupOrRootPWInSkipMode)
            {
                UnSubscribeToProjectWorkCycle();
                // Falls durch tiefere Callstacks der Status schon weitergeschaltet worden ist, dann schalte Status nicht weiter
                if (CurrentACState == ACStateEnum.SMStarting)
                    CurrentACState = ACStateEnum.SMCompleted;
                return;
            }

            if (DurationMustExpire)
            {
                base.SMStarting();
            }
            else
            {
                RecalcTimeInfo();
                var newMethod = NewACMethodWithConfiguration();
                CreateNewProgramLog(newMethod, true);
                CurrentACState = ACStateEnum.SMRunning;
            }

            PWBakeryGroupFermentation fermentationGroup = ParentPWGroup as PWBakeryGroupFermentation;
            if (fermentationGroup != null)
            {
                fermentationGroup.OnChildPWBakeryEndOnTimeStart(this);
            }

            CheckIsStartTooLate();
        }

        public override void SMRunning()
        {
            if (ACOperationMode == ACOperationModes.Live)
            {
                if (ParentPWGroup != null)
                {
                    var processModule = ParentPWGroup.AccessedProcessModule;
                    if (processModule != null)
                        processModule.RefreshPWNodeInfo();
                }
            }

            base.SMRunning();
        }

        public override void SMCompleted()
        {
            PWBakeryGroupFermentation fermentationGroup = ParentPWGroup as PWBakeryGroupFermentation;
            if (fermentationGroup != null)
            {
                fermentationGroup.OnChildPWBakeryEndOnTimeCompleted(this);
            }

            base.SMCompleted();
        }

        private void CheckIsStartTooLate()
        {
            DateTime? start = TimeInfo.ValueT.ActualTimes?.StartTime;
            if (!start.HasValue)
            {
                start = DateTime.Now;
            }

            DateTime endTime = EndOnTimeSafe;
            TimeSpan waitingTime = Duration;

            if (endTime > DateTime.MinValue)
            {
                DateTime targetStartTime = endTime - waitingTime;
                TimeSpan diff = start.Value - targetStartTime;

                if (diff.TotalMinutes > 1)
                {
                    //Error50486 : The production is late. The node was supposed to start at {0} to ensure waiting time of {1}.
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CheckIsStartToLate", 148, "Error50486", targetStartTime, waitingTime);
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ProcessAlarm, msg.Message);
                        Messages.LogMessageMsg(msg);
                    }
                }
            }

        }

        public void SetEndOnTime(DateTime dateTime)
        {
            using (ACMonitor.Lock(_65100_MembersLock))
            {
                EndOnTime.ValueT = dateTime;
                EndOnTimeNew = dateTime;
            }
        }

        [ACMethodInfo("", "en{'Apply new time'}de{'Neue Zeit anwenden'}", 800, true)]
        public void ApplyEndOnTimeNew()
        {
            PWBakeryGroupFermentation fermentation = ParentPWGroup as PWBakeryGroupFermentation;
            if (fermentation != null)
            {
                DateTime dt, dtNew;
                using (ACMonitor.Lock(_65100_MembersLock))
                {
                    dt = EndOnTime.ValueT;
                    dtNew = EndOnTimeNew;
                    EndOnTime.ValueT = EndOnTimeNew;
                }

                fermentation.ChangeStartNextFermentationStageTime(dt, dtNew);
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

                DateTime dt = EndOnTimeSafe;

                if ((dt < DateTime.Now || dt == DateTime.MinValue)
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

        protected override void DumpPropertyList(XmlDocument doc, XmlElement xmlACPropertyList)
        {
            base.DumpPropertyList(doc, xmlACPropertyList);
        }

        #endregion
    }
}
