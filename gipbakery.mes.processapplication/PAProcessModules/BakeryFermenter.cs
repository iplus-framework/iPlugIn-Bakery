using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Fermenter'}de{'Fermenter'}", Global.ACKinds.TPAProcessModule, Global.ACStorableTypes.Required, false, PWGroupVB.PWClassName, true)]
    public class BakeryFermenter : PAMMixer
    {
        #region c'tors
        static BakeryFermenter()
        {
            RegisterExecuteHandler(typeof(BakeryFermenter), HandleExecuteACMethod_BakeryFermenter);
        }

        public BakeryFermenter(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACPostInit()
        {
            return base.ACPostInit();
        }

        #endregion

        #region Properties
        [ACPropertyBindingSource(9999, "", "", "", false, true)]
        public IACContainerTNet<FermentationStageInfos> FermentationInfo { get; set; }

        [ACPropertyBindingSource(810, "Error", "en{'Waiting alarm'}de{'Wartealarm'}", "", false, false)]
        public IACContainerTNet<PANotifyState> WaitingAlarm
        {
            get;
            set;
        }

        #endregion

        #region Methods

        public void RefreshFermentationInfo(IEnumerable<PWBakeryEndOnTime> nodes)
        {
            if (nodes == null)
            {
                FermentationInfo.ValueT = new FermentationStageInfos();
                return;
            }

            try
            {
                ushort stage = 0;
                FermentationStageInfos infos = new FermentationStageInfos();
                foreach (var node in nodes)
                {
                    stage++;
                    ushort state = 0;
                    if (node.CurrentACState >= ACStateEnum.SMStarting)
                        state = 1;
                    else if (node.IterationCount.ValueT >= 1)
                        state = 2;
                    infos.Add(new FermentationStageInfo(node.EndTimeView.ValueT, state, stage));
                }
                FermentationInfo.ValueT = infos;
            }
            catch (Exception ex)
            {
                Messages.LogException(this.ACUrl, nameof(FermentationInfo), ex);
            }
        }

        public void RaiseWatingAlarm(string waitingTime)
        {
            Msg msg = new Msg(this, eMsgLevel.Error, nameof(PWBakeryMixingPreProd), nameof(RaiseWatingAlarm), 81, "Error50717", waitingTime);
            OnNewAlarmOccurred(WaitingAlarm, msg);
        }

        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        private static bool HandleExecuteACMethod_BakeryFermenter(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, object[] acParameter)
        {
            return HandleExecuteACMethod_PAMMixer(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
        #endregion
    }
}
