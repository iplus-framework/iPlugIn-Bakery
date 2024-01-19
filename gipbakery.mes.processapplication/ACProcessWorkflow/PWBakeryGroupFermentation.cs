using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using gip.mes.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static gip.core.communication.ISOonTCP.PLC;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Workflow group'}de{'Workflow Gruppe'}", Global.ACKinds.TPWGroup, Global.ACStorableTypes.Optional, false, PWProcessFunction.PWClassName, true)]
    public class PWBakeryGroupFermentation : PWGroupVB
    {
        #region c'tors

        static PWBakeryGroupFermentation()
        {
            List<ACMethodWrapper> wrappers = ACMethod.OverrideFromBase(typeof(PWBakeryGroupFermentation), ACStateConst.SMStarting);
            if (wrappers != null)
            {
                foreach (ACMethodWrapper wrapper in wrappers)
                {
                    wrapper.Method.ParameterValueList.Add(new ACValue("DoseInSourProdSimultaneously", typeof(bool), false, Global.ParamOption.Optional));
                    wrapper.ParameterTranslation.Add("DoseInSourProdSimultaneously", "en{'Dose in sour dough production simultaneously'}de{'Dosiere in der Sauerteigproduktion gleichzeitig'}");
                }
            }
            RegisterExecuteHandler(typeof(PWBakeryGroupFermentation), HandleExecuteACMethod_PWBakeryGroupFermentation);
        }

        public PWBakeryGroupFermentation(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public const string PN_NextFermentationStage = "NextFermentationStage";
        public const string PN_StartNextFermentationStageTime = "StartNextFermentationStageTime";
        public const string PN_ReadyForDosingTime = "ReadyForDosingTime";

        public new const string PWClassName = "PWBakeryGroupFermentation";

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            return base.ACDeInit(deleteACClassTask);
        }

        public override void Recycle(IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
        {
            TargetFacility = null;
            SourceFacility = null;
            base.Recycle(content, parentACObject, parameter, acIdentifier);
        }

        #endregion


        #region Properties


        [ACPropertyBindingSource(800, "Info", "en{'IsTimeCalculated'}de{'IsTimeCalculated'}", "", false, true)]
        public IACContainerTNet<bool> IsTimeCalculated
        {
            get;
            set;
        }

        #region Properties => TimeCalculation

        [ACPropertyBindingSource(800, "Info", "en{'Next fermentation stage'}de{'Nächste Fermentationsstufe'}", "", false, true)]
        public IACContainerTNet<short> NextFermentationStage
        {
            get;
            set;
        }

        [ACPropertyBindingSource(800, "Info", "en{'Next fermentation stage start'}de{'Nächster Fermentationsstufenstart'}", "", false, true)]
        public IACContainerTNet<DateTime> StartNextFermentationStageTime
        {
            get;
            set;
        }

        [ACPropertyBindingSource(800, "Info", "en{'Ready for dosing'}de{'Bereit für Dosierung'}", "", false, true)]
        public IACContainerTNet<DateTime> ReadyForDosingTime
        {
            get;
            set;
        }

        public bool DoseInSourProdSimultaneously
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("DoseInSourProdSimultaneously");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }

        #endregion

        #region Properties => Virtual stores

        private Facility _SourceFacility;
        public Facility SourceFacility
        {
            get
            {
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    return _SourceFacility;
                }
            }
            set
            {
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    _SourceFacility = value;
                }
            }
        }

        [ACMethodInfo("", "", 9999, true)]
        public Guid? GetSourceFacilityID()
        {
            return SourceFacility?.FacilityID;
        }

        private Facility _TargetFacility;
        public Facility TargetFacility
        {
            get
            {
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    return _TargetFacility;
                }
            }
            set
            {
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    _TargetFacility = value;
                }
            }
        }

        [ACMethodInfo("", "", 9999, true)]
        public Guid? GetTargetFacilityID()
        {
            return TargetFacility?.FacilityID;
        }

        #endregion

        #endregion


        #region Methods

        #region State

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }


        public override void SMRunning()
        {
            if (Root == null || !Root.Initialized)
            {
                SubscribeToProjectWorkCycle();
                return;
            }

            base.SMRunning();

            bool findStores = false;
            using (ACMonitor.Lock(_20015_LockValue))
            {
                findStores = SourceFacility == null || TargetFacility == null;
            }

            if (findStores)
                FindVirtualStores();

            if (!IsTimeCalculated.ValueT)
            {
                short stage = 0;
                bool hasStarted = false;
                IEnumerable<PWBakeryEndOnTime> nodes = null;
                try
                {
                    PWBakeryEndOnTime nextActiveNode = GetNextActiveEndOnTimeNode(out stage, out hasStarted, out nodes);
                    if (hasStarted && nextActiveNode != null && nextActiveNode.DurationMustExpire)
                        CalculateDurationForewardFromCurrentPosition();
                    else
                        CalculateDurationBackwardFromEnd();
                }
                catch (Exception ex)
                {
                    Messages.LogException(this.GetACUrl(), nameof(SMRunning), ex);
                }
                finally
                {
                    IsTimeCalculated.ValueT = true;
                }
                CheckIfStartIsTooLate();
                BakeryFermenter fermenter = AccessedProcessModule as BakeryFermenter;
                if (fermenter != null)
                    fermenter.RefreshFermentationInfo(nodes);
            }
            UnSubscribeToProjectWorkCycle();
        }


        public override void SMCompleted()
        {
            DeactivatePreProdFunctions();
            base.SMCompleted();
        }


        public override void SMIdle()
        {
            base.SMIdle();

            IsTimeCalculated.ValueT = false;
            NextFermentationStage.ValueT = 0;

            DeactivatePreProdFunctions();
        }


        protected override void OnProcessModuleReleased(PAProcessModule module)
        {
            BakeryFermenter fermenter = module as BakeryFermenter;
            if (fermenter != null)
                fermenter.RefreshFermentationInfo(null);
            base.OnProcessModuleReleased(module);
        }


        public virtual void ActivatePreProdFunctions()
        {
            if (AccessedProcessModule == null)
                return;

            PAFBakeryYeastProducing yeast = AccessedProcessModule.FindChildComponents<PAFBakeryYeastProducing>(c => c is PAFBakeryYeastProducing).FirstOrDefault();
            if (yeast != null)
            {
                yeast.NeedWork.ValueT = true;
            }
        }


        public virtual void DeactivatePreProdFunctions()
        {
            if (AccessedProcessModule == null)
                return;

            PAFBakeryYeastProducing yeast = AccessedProcessModule.FindChildComponents<PAFBakeryYeastProducing>(c => c is PAFBakeryYeastProducing).FirstOrDefault();
            if (yeast != null)
            {
                yeast.NeedWork.ValueT = false;
            }
        }

        #endregion


        #region User Interaction

        [ACMethodInteraction("", "en{'Recalculate start-times'}de{'Neuberechnung der Startzeiten'}", 800, true)]
        public void RunCalculationAgain()
        {
            IsTimeCalculated.ValueT = false;
            SubscribeToProjectWorkCycle();
        }

        public bool IsEnabledRunCalculationAgain()
        {
            return true;
        }

        #endregion


        #region Methods => TimeCalculation

        public bool CheckIfStartIsTooLate()
        {
            IEnumerable<PWBakeryEndOnTime> endOnTimeNodes = GetSortedEndOnTimes();

            PWBakeryEndOnTime firstEndOnTimeNode = endOnTimeNodes.FirstOrDefault();
            // Inf not waiting times in workflow or first waiting time is completed, then switch off monitoring
            if (firstEndOnTimeNode == null
                || firstEndOnTimeNode.IterationCount.ValueT >= 1)
                return true;

            DateTime endTime = firstEndOnTimeNode.EndTime.ValueT;
            TimeSpan waitingTime = firstEndOnTimeNode.WaitingTime.ValueT;
            DateTime startTime = endTime - waitingTime;
            if (startTime > DateTime.MinValue && startTime < DateTimeUtils.NowDST)
            {
                string orderInfo = AccessedProcessModule?.OrderInfo.ValueT;
                orderInfo = orderInfo.Replace("\r", "").Replace("\n", " ");
                if (DateTime.Now.IsDaylightSavingTime())
                    startTime = startTime.AddHours(1);

                //Warning50041: The production order {0} is planned to start at {1} but now is {2}. Please take a look.
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CheckIfStartIsTooLate(10)", 256, "Warning50041",
                                    orderInfo, startTime, DateTime.Now);

                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                {
                    Messages.LogMessageMsg(msg);
                }

                OnNewAlarmOccurred(ProcessAlarm, msg, true);
                ProcessAlarm.ValueT = PANotifyState.AlarmOrFault;
                return true;
            }
            return true;
        }


        private void CalculateDurationBackwardFromEnd()
        {
            PWMethodProduction production = ParentPWMethod<PWMethodProduction>();
            if (production == null)
                return;

            ProdOrderBatch batch = production.CurrentProdOrderBatch;
            if (batch == null)
                return;

            DateTime plannedEndTime = DateTime.MinValue;

            using (DatabaseApp dbApp = new DatabaseApp())
            {
                ProdOrderBatch prodOrderBatch = batch.FromAppContext<ProdOrderBatch>(dbApp);
                if (prodOrderBatch == null)
                {
                    //Error50479: The entity {0} is null!
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CalculateDuration(5)", 364, "Error50479", ProdOrderBatch.ClassName);
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        Messages.LogMessageMsg(msg);
                    }
                    OnNewAlarmOccurred(ProcessAlarm, msg, true);
                    return;
                }

                ProdOrderBatchPlan batchPlan = prodOrderBatch.ProdOrderBatchPlan;
                if (batchPlan == null)
                {
                    //Error50479: The entity {0} is null!
                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CalculateDuration(6)", 377, "Error50479", ProdOrderBatchPlan.ClassName);
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        Messages.LogMessageMsg(msg);
                    }
                    OnNewAlarmOccurred(ProcessAlarm, msg, true);
                    return;
                }

                if (batchPlan.ScheduledEndDate != null)
                    plannedEndTime = batchPlan.ScheduledEndDate.Value;
            }

            if (plannedEndTime == DateTime.MinValue)
            {
                //Error50478: Scheduled end date is not configured!
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CalculateDuration(10)", 393, "Error50478");
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                {
                    Messages.LogMessageMsg(msg);
                }
                OnNewAlarmOccurred(ProcessAlarm, msg, true);
                return;
            }

            List<PWBakeryEndOnTime> endOnTimeNodes = GetSortedEndOnTimes();
            if (endOnTimeNodes == null || !endOnTimeNodes.Any())
                return;
            PWBakeryEndOnTime lastNode = endOnTimeNodes.LastOrDefault();

            if (lastNode == null)
            {
                //Error50477 : The last node PWBakeryEndOnTime can not be found!
                Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CalculateDuration(20)", 414, "Error50477");
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                {
                    OnNewAlarmOccurred(ProcessAlarm, msg);
                    Messages.LogMessageMsg(msg);
                }
                return;
            }

            DateTime plannedEndTimeDST = plannedEndTime.GetWinterTime();
            lastNode.EndTime.ValueT = plannedEndTimeDST;

            bool isEndTimeDST = TimeZoneInfo.Local.IsDaylightSavingTime(plannedEndTime);
            bool useDSTSwitch = TimeZoneInfo.Local.SupportsDaylightSavingTime;
            ReadyForDosingTime.ValueT = plannedEndTime; // Time with DST

            lastNode = null;
            for (int i = endOnTimeNodes.Count - 1; i >= 0; i--)
            {
                if (lastNode == null)
                {
                    lastNode = endOnTimeNodes[i];
                    continue;
                }
                PWBakeryEndOnTime currentNode = endOnTimeNodes[i];

                IEnumerable<IACComponentPWNode> parallelNodes = FindParallelNodes(currentNode);
                List<PWBakeryDosingPreProd> pwDosings = FindVariableDurationNodes(lastNode, currentNode, parallelNodes);
                List<PWBaseNodeProcess> fixDurationNodes = FindFixedDurationNodes(lastNode, currentNode, parallelNodes);
                fixDurationNodes.Add(lastNode);

                TimeSpan fixedDuration = CalculateFixedDuration(fixDurationNodes);
                TimeSpan variableDuration = CalculateVariableDuration(pwDosings);
                TimeSpan durationPerStage = fixedDuration + variableDuration;

                DateTime prevTime = lastNode.EndTime.ValueT - durationPerStage;

                currentNode.EndTime.ValueT = prevTime;
                lastNode = currentNode;
            }

            if (lastNode != null)
                StartNextFermentationStageTime.ValueT = lastNode.EndTimeView.ValueT;
        }


        private void CalculateDurationForewardFromCurrentPosition()
        {
            IEnumerable<PWBakeryEndOnTime> nodes = GetSortedEndOnTimes();
            if (nodes == null && !nodes.Any())
                return;

            PWBakeryEndOnTime lastActiveNode = null;
            PWBakeryEndOnTime lastNode = null;
            foreach (var currentNode in nodes)
            {
                if (lastActiveNode == null)
                {
                    if (currentNode.CurrentACState >= ACStateEnum.SMStarting)
                        lastActiveNode = currentNode;
                    else if (currentNode.IterationCount.ValueT <= 0)
                        lastActiveNode = lastNode;
                }

                if (lastActiveNode != null && currentNode != lastActiveNode && lastNode != null)
                {
                    IEnumerable<IACComponentPWNode> parallelNodes = FindParallelNodes(lastNode);
                    List<PWBakeryDosingPreProd> pwDosings = FindVariableDurationNodes(currentNode, lastNode, parallelNodes);
                    List<PWBaseNodeProcess> fixDurationNodes = FindFixedDurationNodes(currentNode, lastNode, parallelNodes);
                    fixDurationNodes.Add(currentNode);

                    TimeSpan fixedDuration = CalculateFixedDuration(fixDurationNodes);
                    TimeSpan variableDuration = CalculateVariableDuration(pwDosings);
                    TimeSpan durationPerStage = fixedDuration + variableDuration;

                    DateTime nextTime = lastNode.EndTime.ValueT + durationPerStage;
                    currentNode.EndTime.ValueT = nextTime;
                }

                lastNode = currentNode;
            }
            if (lastNode != null)
                ReadyForDosingTime.ValueT = lastNode.EndTimeView.ValueT;
        }


        public IEnumerable<IACComponentPWNode> FindParallelNodes(PWBaseInOut pwNode)
        {
            var prevEndSources = pwNode.PWPointIn.ConnectionList.Select(c => c.ValueT).Cast<PWBaseInOut>();

            if (prevEndSources != null && prevEndSources.Any())
                return prevEndSources.SelectMany(c => c.PWPointOut.ConnectionList.Select(x => x.ValueT as IACComponentPWNode)).Distinct();

            return new List<IACComponentPWNode>();
        }


        public virtual PWBakeryEndOnTime FindPrevEndOnTimeNode(PWBase currentNode)
        {
            var query = currentNode.FindPredecessors<PWBase>(true, c => c is PWBase, null, 1);
            if (query.Any())
            {
                PWBakeryEndOnTime endOnTime = query.FirstOrDefault(c => c is PWBakeryEndOnTime) as PWBakeryEndOnTime;
                if (endOnTime != null)
                    return endOnTime;

                var startNode = query.FirstOrDefault(c => c is PWNodeStart);
                if (startNode != null)
                    return null;

                foreach (var item in query)
                {
                    return FindPrevEndOnTimeNode(item);
                }
            }
        
            return null;
        }


        public virtual List<PWBakeryDosingPreProd> FindVariableDurationNodes(PWBakeryEndOnTime currentEndNode, PWBakeryEndOnTime prevEndNode, IEnumerable<IACComponentPWNode> parallelNodes)
        {
            // PWNodeEndOnTime has parallel workflow nodes
            if (parallelNodes.Count() > 1)
            {
                return currentEndNode.FindPredecessors<PWBakeryDosingPreProd>(true, c => c is PWBakeryDosingPreProd, d => d == prevEndNode || parallelNodes.Contains(d), 0);
            }

            return currentEndNode.FindPredecessors<PWBakeryDosingPreProd>(true, c => c is PWBakeryDosingPreProd, d => d == prevEndNode, 0);

        }


        public virtual List<PWBaseNodeProcess> FindFixedDurationNodes(PWBakeryEndOnTime currentEndNode, PWBakeryEndOnTime prevEndNode, IEnumerable<IACComponentPWNode> parallelNodes)
        {
            // PWNodeEndOnTime has parallel workflow nodes
            if (parallelNodes.Count() > 1)
            {
                return currentEndNode.FindPredecessors<PWBaseNodeProcess>(true, c => c is PWMixing || (c is PWNodeWait && !(c is PWBakeryEndOnTime)), d => d == prevEndNode || parallelNodes.Contains(d), 0);
            }

            return currentEndNode.FindPredecessors<PWBaseNodeProcess>(true, c => c is PWMixing || (c is PWNodeWait && !(c is PWBakeryEndOnTime)), d => d is PWBakeryEndOnTime, 0);
        }


        public virtual TimeSpan CalculateVariableDuration(List<PWBakeryDosingPreProd> dosingNodes)
        {
            PAProcessModule processModule = AccessedProcessModule;
            bool doseSim = DoseInSourProdSimultaneously;

            List<TimeSpan> ts = new List<TimeSpan>();

            PAFBakeryDosingWater waterFunc = null;


            //SourProdDosingUnit(Auslastung) => FlowRate1
            foreach (PWBakeryDosingPreProd dosingNode in dosingNodes)
            {
                TimeSpan result = dosingNode.CalculateDuration(doseSim, processModule, out waterFunc);
                if (result > TimeSpan.Zero)
                    ts.Add(result);
            }

            //TODO: check if nodes parallel

            if (!ts.Any())
                return TimeSpan.Zero;

            if (doseSim)
            {
                TimeSpan dosTimeWaterControl = TimeSpan.Zero;
                if (waterFunc != null)
                    dosTimeWaterControl = TimeSpan.FromSeconds(waterFunc.DosTimeWaterControl.ValueT);
                return ts.Max() + dosTimeWaterControl;
            }
            else
            {
                
                return TimeSpan.FromSeconds(ts.Sum(c => c.TotalSeconds));
            }
        }


        public virtual TimeSpan CalculateFixedDuration(List<PWBaseNodeProcess> pwNodes)
        {
            TimeSpan result = TimeSpan.Zero;

            result = TimeSpan.FromSeconds(pwNodes.OfType<PWNodeWait>().Sum(c => c.Duration.TotalSeconds));

            var pwMixingTimes = pwNodes.Where(c => c is PWMixing && c.ContentACClassWF != null && c.ContentACClassWF.ACIdentifierPrefix == "MixingTime");
            foreach (PWMixing pwNode in pwMixingTimes)
            {
                result += pwNode.MixingTime;
            }

            return result;
        }


        public virtual void RefreshFermentationStageInfo(bool recalcDurations)
        {
            short stage = 0;
            bool hasStarted = false;
            IEnumerable<PWBakeryEndOnTime> nodes = null;
            PWBakeryEndOnTime nextActiveNode = GetNextActiveEndOnTimeNode(out stage, out hasStarted, out nodes);
            if (nextActiveNode != null)
            {
                if (hasStarted && nextActiveNode.DurationMustExpire && recalcDurations)
                    CalculateDurationForewardFromCurrentPosition();
                 StartNextFermentationStageTime.ValueT = nextActiveNode.EndTimeView.ValueT;
            }
            else
                StartNextFermentationStageTime.ValueT = ReadyForDosingTime.ValueT;
            NextFermentationStage.ValueT = stage;
            BakeryFermenter fermenter = AccessedProcessModule as BakeryFermenter;
            if (fermenter != null)
                fermenter.RefreshFermentationInfo(nodes);
        }


        protected List<PWBakeryEndOnTime> GetSortedEndOnTimes()
        {
            var startNode = PWNodeStart;
            if (startNode != null)
                return startNode.FindSuccessors<PWBakeryEndOnTime>(true, c => c is PWBakeryEndOnTime, null, 200);
            else
                return FindChildComponents<PWBakeryEndOnTime>(c => c is PWBakeryEndOnTime)
                    .OrderBy(x => ACIdentifier)
                    .ToList();
                    //.OrderBy(x => x.EndTime.ValueT)
                    //.ThenBy(c => c.ACIdentifier);
        }


        protected PWBakeryEndOnTime GetNextActiveEndOnTimeNode(out short stage, out bool hasStarted, out IEnumerable<PWBakeryEndOnTime> nodes)
        {
            hasStarted = false;
            stage = 0;
            PWBakeryEndOnTime nextActiveNode = null;
            nodes = GetSortedEndOnTimes();
            if (nodes != null && nodes.Any())
            {
                foreach (var node in nodes)
                {
                    if (node.CurrentACState >= ACStateEnum.SMStarting
                        || node.IterationCount.ValueT <= 0)
                    {
                        nextActiveNode = node;
                        break;
                    }
                    stage++;
                    hasStarted = true;
                }
            }
            return nextActiveNode;
        }


        protected PWBakeryEndOnTime GetLastActiveEndOnTimeNode(out short stage, out IEnumerable<PWBakeryEndOnTime> nodes)
        {
            stage = 0;
            PWBakeryEndOnTime lastActiveNode = null;
            nodes = GetSortedEndOnTimes();
            if (nodes != null && nodes.Any())
            {
                foreach (var node in nodes)
                {
                    stage++;
                    if (node.CurrentACState >= ACStateEnum.SMStarting)
                    {
                        lastActiveNode = node;
                        break;
                    }
                    else if (node.IterationCount.ValueT <= 0)
                        break;
                    lastActiveNode = node;
                }
            }
            return lastActiveNode;
        }


        public override void AcknowledgeAlarms()
        {
            base.AcknowledgeAlarms();
        }

        #endregion


        #region Methods => VirtualStores

        public void FindVirtualStores()
        {
            PAMParkingspace sourceComponent;
            PAMSilo targetComponent;
            bool isVirtSourceStoreNecessary = false;
            bool isVirtTargetStoreNecessary = false;
            Msg msg = FindSourceAndTargetStore(out sourceComponent, out isVirtSourceStoreNecessary, out targetComponent, out isVirtTargetStoreNecessary);
            if (msg != null)
            {
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    Messages.LogMessageMsg(msg);
                OnNewAlarmOccurred(ProcessAlarm, msg, true);
                return;
            }

            using (DatabaseApp dbApp = new DatabaseApp())
            {
                Facility sourceFacility, targetFacility;
                FindFacilityForSourceAndTargetStore(dbApp, sourceComponent, targetComponent, out sourceFacility, out targetFacility);

                if (sourceFacility == null && sourceComponent != null)
                {
                    //Error50480: The virtual source store can not be found!
                    msg = new Msg(this, eMsgLevel.Error, PWClassName, "FindVirtualStores(10)", 683, "Error50480");
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ProcessAlarm, msg);
                        Messages.LogMessageMsg(msg);
                    }
                    return;
                }

                if (targetFacility == null && targetComponent != null)
                {
                    //Error50481: The virtual target store can not be found!
                    msg = new Msg(this, eMsgLevel.Error, PWClassName, "FindVirtualStores(20)", 399, "Error50481");
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        OnNewAlarmOccurred(ProcessAlarm, msg);
                        Messages.LogMessageMsg(msg);
                    }
                    return;
                }

                SourceFacility = sourceFacility;
                TargetFacility = targetFacility;
            }
        }

        public virtual Msg FindSourceAndTargetStore(out PAMParkingspace source, out bool isVirtSourceStoreNecessary, out PAMSilo target, out bool isVirtTargetStoreNecessary)
        {
            source = null;
            target = null;
            isVirtSourceStoreNecessary = false;
            isVirtTargetStoreNecessary = false;
            Msg msg = null;
            PAProcessModule module = AccessedProcessModule;
            if (module == null)
                return null;
            PAFBakeryYeastProducing function = module.FindChildComponents<PAFBakeryYeastProducing>(c => c is PAFBakeryYeastProducing).FirstOrDefault();
            if (function == null)
                return null;
            isVirtSourceStoreNecessary = function.IsVirtSourceStoreNecessary;
            isVirtTargetStoreNecessary = function.IsVirtTargetStoreNecessary;
            source = function.VirtualSourceStore;
            target = function.VirtualTargetStore;
            if (   (source == null && isVirtSourceStoreNecessary) 
                || (target == null && isVirtTargetStoreNecessary))
            {
                //Error50482: The source or/and target store can not be found!
                msg = new Msg(this, eMsgLevel.Error, PWClassName, "FindSourceAndTargetStore(10)", 722, "Error50482");
            }

            return msg;
        }

        public void FindFacilityForSourceAndTargetStore(DatabaseApp dbApp, PAMParkingspace source, PAMSilo target, out Facility sourceFacility,
                                                out Facility targetFacility)
        {
            sourceFacility = null;
            targetFacility = null;
            if (source != null)
                sourceFacility = dbApp.Facility.FirstOrDefault(c => c.VBiFacilityACClassID == source.ComponentClass.ACClassID);
            Facility temp = target?.Facility?.ValueT?.ValueT;
            if (temp != null)
                targetFacility = temp.FromAppContext<Facility>(dbApp);
        }
        #endregion


        #region Dump and HandleExecute
        protected override void DumpPropertyList(XmlDocument doc, XmlElement xmlACPropertyList)
        {
            base.DumpPropertyList(doc, xmlACPropertyList);

            XmlElement xmlChild = xmlACPropertyList["IsTimeCalculated"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("IsTimeCalculated");
                if (xmlChild != null)
                    xmlChild.InnerText = IsTimeCalculated.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["DoseInSourProdSimultaneously"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("DoseInSourProdSimultaneously");
                if (xmlChild != null)
                    xmlChild.InnerText = DoseInSourProdSimultaneously.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["SourceFacility"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("SourceFacility");
                if (xmlChild != null)
                    xmlChild.InnerText = SourceFacility?.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["TargetFacility"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("TargetFacility");
                if (xmlChild != null)
                    xmlChild.InnerText = TargetFacility?.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }
        }


        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            switch (acMethodName)
            {
                case nameof(RunCalculationAgain):
                    RunCalculationAgain();
                    return true;
                case nameof(IsEnabledRunCalculationAgain):
                    result = IsEnabledRunCalculationAgain();
                    return true;
                case nameof(GetSourceFacilityID):
                    result = GetSourceFacilityID();
                    return true;
                case nameof(GetTargetFacilityID):
                    result = GetTargetFacilityID();
                    return true;
                default:
                    break;
            }

            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }


        public static bool HandleExecuteACMethod_PWBakeryGroupFermentation(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PWGroupVB(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
        #endregion

        #endregion
    }
}
