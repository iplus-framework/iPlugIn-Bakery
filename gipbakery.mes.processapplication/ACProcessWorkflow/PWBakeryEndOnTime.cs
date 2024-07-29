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
            List<ACMethodWrapper> wrappers = ACMethod.OverrideFromBase(typeof(PWBakeryEndOnTime), ACStateConst.SMStarting);
            if (wrappers != null)
            {
                foreach (ACMethodWrapper wrapper in wrappers)
                {
                    wrapper.Method.ParameterValueList.Add(new ACValue("DurationMustExpire", typeof(bool), false, Global.ParamOption.Optional));
                    wrapper.ParameterTranslation.Add("DurationMustExpire", "en{'Waiting time must elapse'}de{'Wartezeit muss verstreichen'}");
                    wrapper.CaptionTranslation = "en{'End on time'}de{'Beenden bei Uhrzeit'}";
                }
            }
            RegisterExecuteHandler(typeof(PWBakeryEndOnTime), HandleExecuteACMethod_PWBakeryEndOnTime);
        }

        public PWBakeryEndOnTime(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACPostInit()
        {
            bool result = base.ACPostInit();
            return result;
        }

        #endregion

        #region Properties

        /// <summary>
        /// New Time set from User-Interface (with daylight saving)
        /// </summary>
        [ACPropertyInfo(801, "", "en{'End on time new'}de{'Beenden bei Uhrzeit neu'}", "", true)]
        public DateTime EndOnTimeNew
        {
            get;
            set;
        }

        public bool DurationMustExpire
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

        public override bool ActsAsAlarmClock
        {
            get
            {
                return !DurationMustExpire;
            }
        }

        #endregion

        #region Methods

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            if (!CheckParentGroupAndHandleSkipMode())
                return;
            base.SMStarting();
            RefreshFermentationStageInfo(false);
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
            base.SMCompleted();
            EndTime.ValueT = DateTimeUtils.NowDST;
            RefreshFermentationStageInfo(true);
        }

        protected override void OnViewTimeChanged()
        {
            EndOnTimeNew = EndTimeView.ValueT;
            base.OnViewTimeChanged();
            RefreshFermentationStageInfo(false);
        }

        private void RefreshFermentationStageInfo(bool nodeStateChanged)
        {
            if (Root == null || !Root.Initialized)
                return;
            PWBakeryGroupFermentation fermentationGroup = ParentPWGroup as PWBakeryGroupFermentation;
            if (fermentationGroup != null)
                fermentationGroup.RefreshFermentationStageInfo(nodeStateChanged);
        }

        private void CheckIsStartTooLate()
        {
            DateTime? start = TimeInfo.ValueT.ActualTimes?.StartTime;
            if (!start.HasValue)
                start = DateTimeUtils.NowDST;

            DateTime endTime = EndTime.ValueT;
            TimeSpan waitingTime = Duration;

            if (endTime > DateTime.MinValue)
            {
                DateTime targetStartTime = endTime - waitingTime;
                TimeSpan diff = start.Value - targetStartTime;

                if (diff.TotalMinutes > 1)
                {
                    //Error50486 : The production is late. The node was supposed to start at {0} to ensure waiting time of {1}.
                    if (DateTime.Now.IsDaylightSavingTime())
                        targetStartTime = targetStartTime.AddHours(1);
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CheckIsStartToLate", 148, "Error50486", targetStartTime, waitingTime);
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ProcessAlarm, msg.Message);
                        Messages.LogMessageMsg(msg);
                    }
                }
            }

        }

        [ACMethodInfo("", "en{'Apply new time'}de{'Neue Zeit anwenden'}", 800, true)]
        public void ApplyEndOnTimeNew()
        {
            EndTime.ValueT = EndOnTimeNew.GetWinterTime();
            RefreshFermentationStageInfo(true);
        }

        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            switch (acMethodName)
            {
                case nameof(ApplyEndOnTimeNew):
                    ApplyEndOnTimeNew();
                    return true;
            }
            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        public static bool HandleExecuteACMethod_PWBakeryEndOnTime(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PWNodeWait(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

        protected override void DumpPropertyList(XmlDocument doc, XmlElement xmlACPropertyList, ref DumpStats dumpStats)
        {
            base.DumpPropertyList(doc, xmlACPropertyList, ref dumpStats);
        }

        #endregion
    }
}
