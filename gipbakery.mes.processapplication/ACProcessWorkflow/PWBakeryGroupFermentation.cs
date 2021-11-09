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

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Workflow group'}de{'Workflow Gruppe'}", Global.ACKinds.TPWGroup, Global.ACStorableTypes.Optional, false, PWProcessFunction.PWClassName, true)]
    public class PWBakeryGroupFermentation : PWGroupVB
    {
        #region c'tors

        static PWBakeryGroupFermentation()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("IgnoreFIFO", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("IgnoreFIFO", "en{'Ignore FIFO for Processmodule-Mapping'}de{'Ignoriere FIFO-Prinzip für Prozessmodul-Belegung'}");
            method.ParameterValueList.Add(new ACValue("RoutingCheck", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("RoutingCheck", "en{'Only routeable modules from predecessor'}de{'Nur erreichbare Module vom Vorgänger'}");
            method.ParameterValueList.Add(new ACValue("WithoutPM", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("WithoutPM", "en{'Ignore Processmodule-Mapping'}de{'Ohne Prozessmodul-Belegung'}");
            method.ParameterValueList.Add(new ACValue("OccupationByScan", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("OccupationByScan", "en{'Processmodule-Mapping manually by user'}de{'Prozessmodulbelegung manuell vom Anwender'}");
            method.ParameterValueList.Add(new ACValue("Priority", typeof(ushort), 0, Global.ParamOption.Required));
            paramTranslation.Add("Priority", "en{'Priorization'}de{'Priorisierung'}");
            method.ParameterValueList.Add(new ACValue("FIFOCheckFirstPM", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("FIFOCheckFirstPM", "en{'FIFO check only for WF-Groups which competes for the same process module'}de{'FIFO-Prüfung nur bei WF-Gruppen die das selbe Prozessmodul konkurrieren.'}");
            method.ParameterValueList.Add(new ACValue("SkipIfNoComp", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("SkipIfNoComp", "en{'Skip if no one child has material to process'}de{'Skip if no one child has material to process'}");
            method.ParameterValueList.Add(new ACValue("MaxBatchWeight", typeof(double), false, Global.ParamOption.Optional));
            paramTranslation.Add("MaxBatchWeight", "en{'Max. batch weight [kg]'}de{'Maximales Batchgewicht [kg]'}");

            method.ParameterValueList.Add(new ACValue("DoseInSourProdSimultaneously", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("DoseInSourProdSimultaneously", "en{'Dose in sour dough production simultaneously'}de{'Dosiere in der Sauerteigproduktion gleichzeitig'}");

            method.ParameterValueList.Add(new ACValue("DSTSwitchInTimeCalculation", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("DSTSwitchInTimeCalculation", "en{'Include daylight savings time switch in time calculation'}de{'Sommerzeitumstellung in die Zeitberechnung einbeziehen'}");
            //method.ParameterValueList.Add(new ACValue("SourProdDosingUnit", typeof(double), 10.0, Global.ParamOption.Optional));
            //paramTranslation.Add("SourProdDosingUnit", "en{'Sour dough production dosing unit [kg]'}de{'SauerteigDosiereinheit [kg]'}");
            //method.ParameterValueList.Add(new ACValue("SourProdDosingPause", typeof(int), 2, Global.ParamOption.Optional));
            //paramTranslation.Add("SourProdDosingPause", "en{'Sour dough production dosing pause [sec]'}de{'SauerteigDospause [sec]'}");

            var wrapper = new ACMethodWrapper(method, "en{'Configuration'}de{'Konfiguration'}", typeof(PWBakeryGroupFermentation), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryGroupFermentation), ACStateConst.SMStarting, wrapper);

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
            EndOnTimeNodes = null;
            return base.ACDeInit(deleteACClassTask);
        }

        public override void Recycle(IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
        {
            EndOnTimeNodes = null;
            base.Recycle(content, parentACObject, parameter, acIdentifier);
        }

        #endregion

        #region Properties

        private ACMonitorObject _65100_MemebersLock = new ACMonitorObject(65100);

        [ACPropertyBindingSource(800, "Info", "en{'IsTimeCalculated'}de{'IsTimeCalculated'}", "", false, true)]
        public IACContainerTNet<bool> IsTimeCalculated
        {
            get;
            set;
        }

        private PWBakeryEndOnTime[] EndOnTimeNodes
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

        public bool UseDSTSwitch
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("DSTSwitchInTimeCalculation");
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

        private Facility SourceFacility
        {
            get;
            set;
        }

        private Facility TargetFacility
        {
            get;
            set;
        }

        #endregion

        #endregion

        #region Methods

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        public override void SMRunning()
        {
            base.SMRunning();

            bool calculated = false;
            //using(ACMonitor.Lock(_20015_LockValue))
            {
                calculated = IsTimeCalculated.ValueT;
            }

            if (!calculated)
            {
                FindVirtualStores();

                CalculateDuration();

                //ActivatePreProdFunctions();

                //using (ACMonitor.Lock(_20015_LockValue))
                {
                    IsTimeCalculated.ValueT = true;
                }
            }

            SubscribeToProjectWorkCycle();

            CheckIfStartIsTooLate();
        }

        public override void SMCompleted()
        {
            DeactivatePreProdFunctions();
            base.SMCompleted();
        }

        public override void SMIdle()
        {
            base.SMIdle();

            //using(ACMonitor.Lock(_20015_LockValue))
            {
                IsTimeCalculated.ValueT = false;
            }

            NextFermentationStage.ValueT = 0;

            DeactivatePreProdFunctions();
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

        public void CheckIfStartIsTooLate()
        {
            if (EndOnTimeNodes == null)
            {
                EndOnTimeNodes = FindChildComponents<PWBakeryEndOnTime>(1).OrderBy(c => c.EndOnTimeSafe).ToArray();
            }

            PWBakeryEndOnTime endOnTime = EndOnTimeNodes.FirstOrDefault();

            if (endOnTime != null)
            {
                if (endOnTime.IterationCount.ValueT >= 1)
                {
                    UnSubscribeToProjectWorkCycle();
                    return;
                }

                DateTime dt = endOnTime.EndOnTimeSafe;
                TimeSpan waitingTime = endOnTime.WaitingTime.ValueT;

                dt = dt - waitingTime;

                if (dt > DateTime.MinValue)
                {
                    if (dt < DateTime.Now)
                    {
                        string orderInfo = AccessedProcessModule?.OrderInfo.ValueT;
                        orderInfo = orderInfo.Replace("\r", "").Replace("\n", " ");

                        //Warning50041: The production order {0} is planned to start at {1} but now is {2}. Please take a look.

                        Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CheckIfStartIsTooLate(10)", 256, "Warning50041",
                                          orderInfo, dt, DateTime.Now);

                        if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                        {
                            Messages.LogMessageMsg(msg);
                        }

                        OnNewAlarmOccurred(ProcessAlarm, msg, true);
                        ProcessAlarm.ValueT = PANotifyState.AlarmOrFault;

                        UnSubscribeToProjectWorkCycle();
                    }
                }
            }    
        }

        public void RunCalcAgain()
        {
            //using(ACMonitor.Lock(_20015_LockValue))
            {
                IsTimeCalculated.ValueT = false;
            }
            SubscribeToProjectWorkCycle();
        }

        public void ChangeStartNextFermentationStageTime(DateTime oldDateTime, DateTime newDateTime)
        {
            using (ACMonitor.Lock(_65100_MemebersLock))
            {
                if (StartNextFermentationStageTime.ValueT == oldDateTime)
                {
                    StartNextFermentationStageTime.ValueT = newDateTime;
                }
            }
        }


        [ACMethodInteractionClient("", "en{'Recalculate prod times'}de{'Neuberechnung der Produktionszeiten'}", 800, true)]
        public static void RunCalculationAgain(IACComponent acComponent)
        {
            PWBakeryGroupFermentation group = acComponent as PWBakeryGroupFermentation;
            if (group != null)
                group.RunCalcAgain();
        }

        public static bool IsEnabledRunCalculationAgain(IACComponent acComponent)
        {
            if (acComponent == null)
                return false;

            return true;
        }

        #region Methods => TimeCalculation

        private void CalculateDuration()
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

            List<PWBakeryEndOnTime> endOnTimeNodes = FindChildComponents<PWBakeryEndOnTime>(1);

            if (endOnTimeNodes == null || !endOnTimeNodes.Any())
                return;

            int stages = endOnTimeNodes.Count;

            PWBakeryEndOnTime lastNode = endOnTimeNodes.FirstOrDefault(x => x.FindSuccessors<PWDischarging>(true, c => c is PWDischarging, d => d is PWDosing, 0).Any());

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

            lastNode.SetEndOnTime(plannedEndTime);

            bool isEndTimeDST = TimeZoneInfo.Local.IsDaylightSavingTime(plannedEndTime);
            bool useDSTSwitch = UseDSTSwitch;

            endOnTimeNodes.Remove(lastNode);

            ReadyForDosingTime.ValueT = plannedEndTime;

            PWBakeryEndOnTime currentNode = lastNode;

            while (true)
            {
                PWBakeryEndOnTime prevEndOnTime = FindPrevEndOnTimeNode(currentNode);
                if (prevEndOnTime == null)
                {
                    DateTime dt = currentNode.EndOnTimeSafe;
                    using (ACMonitor.Lock(_65100_MemebersLock))
                    {
                        StartNextFermentationStageTime.ValueT = dt;
                    }
                    break;
                }

                IEnumerable<ACComponent> parallelNodes = FindParallelNodes(prevEndOnTime);

                List<PWBakeryDosingPreProd> pwDosings = FindVariableDurationNodes(currentNode, prevEndOnTime, parallelNodes);
                List<PWBaseNodeProcess> fixDurationNodes = FindFixedDurationNodes(currentNode, prevEndOnTime, parallelNodes);
                if (currentNode == lastNode)
                {
                    fixDurationNodes.Add(currentNode);
                }

                TimeSpan fixedDuration = CalculateFixedDuration(fixDurationNodes, stages);
                TimeSpan variableDuration = CalculateVariableDuration(pwDosings, stages);

                TimeSpan durationPerStage = fixedDuration + variableDuration;

                DateTime prevTime = currentNode.EndOnTimeSafe - durationPerStage;

                if (useDSTSwitch)
                {
                    bool isCurrentTimeDST = TimeZoneInfo.Local.IsDaylightSavingTime(prevTime);

                    if (isEndTimeDST != isCurrentTimeDST)
                    {
                        if (isEndTimeDST)
                        {
                            // switch summer to winter
                            prevTime = prevTime.AddHours(-1);
                        }
                        else
                        {
                            // switch winter to summer
                            prevTime = prevTime.AddHours(1);
                        }

                        //change time only once, all predecessors will be calculated on this changed node end time
                        useDSTSwitch = false;
                    }
                }

                prevEndOnTime.SetEndOnTime(prevTime);

                currentNode = prevEndOnTime;

                stages--;
            }
        }

        public IEnumerable<ACComponent> FindParallelNodes(PWBaseInOut pwNode)
        {
            var prevEndSources = pwNode.PWPointIn.ConnectionList.Select(c => c.ValueT).Cast<PWBaseInOut>();

            if (prevEndSources != null && prevEndSources.Any())
                return prevEndSources.SelectMany(c => c.PWPointOut.ConnectionList.Select(x => x.ValueT)).Distinct();

            return new List<ACComponent>();
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

        public virtual List<PWBakeryDosingPreProd> FindVariableDurationNodes(PWBakeryEndOnTime currentEndNode, PWBakeryEndOnTime prevEndNode, IEnumerable<ACComponent> parallelNodes)
        {
            // PWNodeEndOnTime has parallel workflow nodes
            if (parallelNodes.Count() > 1)
            {
                return currentEndNode.FindPredecessors<PWBakeryDosingPreProd>(true, c => c is PWBakeryDosingPreProd, d => d == prevEndNode || parallelNodes.Contains(d), 0);
            }

            return currentEndNode.FindPredecessors<PWBakeryDosingPreProd>(true, c => c is PWBakeryDosingPreProd, d => d == prevEndNode, 0);

        }

        public virtual List<PWBaseNodeProcess> FindFixedDurationNodes(PWBakeryEndOnTime currentEndNode, PWBakeryEndOnTime prevEndNode, IEnumerable<ACComponent> parallelNodes)
        {
            // PWNodeEndOnTime has parallel workflow nodes
            if (parallelNodes.Count() > 1)
            {
                return currentEndNode.FindPredecessors<PWBaseNodeProcess>(true, c => c is PWMixing || c is PWNodeWait, d => d == prevEndNode || parallelNodes.Contains(d), 0);
            }

            return currentEndNode.FindPredecessors<PWBaseNodeProcess>(true, c => c is PWMixing || c is PWNodeWait, d => d is PWBakeryEndOnTime, 0);
        }

        public virtual TimeSpan CalculateVariableDuration(List<PWBakeryDosingPreProd> dosingNodes, int stage)
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

        public virtual TimeSpan CalculateFixedDuration(List<PWBaseNodeProcess> pwNodes, int stage)
        {
            TimeSpan result = TimeSpan.Zero;

            result = TimeSpan.FromSeconds(pwNodes.OfType<PWNodeWait>().Sum(c => c.Duration.TotalSeconds));

            var pwMixingTimes = pwNodes.Where(c => c.ContentACClassWF != null && c.ContentACClassWF.ACIdentifierPrefix == "MixingTime");

            foreach (PWBaseNodeProcess pwNode in pwMixingTimes)
            {
                ACValue duration = pwNode.ContentACClassWF.RefPAACClassMethod?.ACMethod?.ParameterValueList?.GetACValue("Duration");
                if (duration == null)
                    continue;

                TimeSpan ts = duration.ParamAsTimeSpan;
                result += ts;
            }

            return result;
        }

        public virtual void OnChildPWBakeryEndOnTimeStart(PWBakeryEndOnTime pwNode)
        {
            if (pwNode == null)
                return;

            DateTime dt = pwNode.EndOnTimeSafe;
            if (dt != ReadyForDosingTime.ValueT)
            {
                using (ACMonitor.Lock(_65100_MemebersLock))
                {
                    StartNextFermentationStageTime.ValueT = dt;
                }
                if (NextFermentationStage.ValueT == 0)
                {
                    NextFermentationStage.ValueT = 1;
                }
                else
                {
                    NextFermentationStage.ValueT++;
                }
            }
        }

        public virtual void OnChildPWBakeryEndOnTimeCompleted(PWBakeryEndOnTime pwNode)
        {
            if (pwNode == null)
                return;

            IEnumerable<PWBakeryEndOnTime> nodes = pwNode.FindSuccessors<PWBakeryEndOnTime>(true, c => c is PWBakeryEndOnTime).OrderBy(x => x.EndOnTimeSafe);
            if (nodes != null && nodes.Any())
            {
                PWBakeryEndOnTime nextNode = nodes.FirstOrDefault();

                if (nextNode != null)
                {
                    DateTime dt = nextNode.EndOnTimeSafe;

                    using (ACMonitor.Lock(_65100_MemebersLock))
                    {
                        StartNextFermentationStageTime.ValueT = dt;
                    }
                }
            }

        }

        public override void AcknowledgeAlarms()
        {
            base.AcknowledgeAlarms();
        }

        #endregion

        #region Methods => VirutalStores

        public void FindVirtualStores()
        {
            PAMParkingspace source;
            PAMSilo target;

            Msg msg = FindSourceAndTargetStore(out source, out target);

            if (msg != null)
            {
                if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    Messages.LogMessageMsg(msg);
                OnNewAlarmOccurred(ProcessAlarm, msg, true);
                return;
            }

            using (DatabaseApp dbApp = new DatabaseApp())
            {
                Facility sFacility, tFacility;

                FindFacilityForSourceAndTargetStore(dbApp, source, target, out sFacility, out tFacility);

                if (sFacility == null)
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

                if (tFacility == null)
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

                SourceFacility = sFacility;
                TargetFacility = tFacility;
            }
        }

        public virtual Msg FindSourceAndTargetStore(out PAMParkingspace source, out PAMSilo target)
        {
            source = null;
            target = null;

            Msg msg = null;

            PAProcessModule module = AccessedProcessModule;
            PAFBakeryYeastProducing.FindVirtualStores(module, out source, out target);

            if (source == null || target == null)
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
            {
                targetFacility = temp.FromAppContext<Facility>(dbApp);
            }
        }

        [ACMethodInfo("","",9999, true)]
        public Guid? GetSourceFacilityID()
        {
            Guid? result = null;
            using(ACMonitor.Lock(_20015_LockValue))
            {
                result = SourceFacility?.FacilityID;
            }

            return result;
        }

        public Facility GetSourceFacility()
        {
            Facility result = null;
            using (ACMonitor.Lock(_20015_LockValue))
            {
                result = SourceFacility;
            }

            return result;
        }

        public Facility GetTargetFacility()
        {
            Facility result = null;
            using (ACMonitor.Lock(_20015_LockValue))
            {
                result = TargetFacility;
            }

            return result;
        }


        #endregion

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

            xmlChild = xmlACPropertyList["UseDSTSwitch"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("UseDSTSwitch");
                if (xmlChild != null)
                    xmlChild.InnerText = UseDSTSwitch.ToString();
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
                    xmlChild.InnerText = TargetFacility.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }
        }

        #endregion

        public static bool HandleExecuteACMethod_PWBakeryGroupFermentation(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            switch (acMethodName)
            {
                case "RunCalculationAgain":
                    RunCalculationAgain(acComponent);
                    return true;
                case Const.IsEnabledPrefix + "RunCalculationAgain":
                    result = IsEnabledRunCalculationAgain(acComponent);
                    return true;
            }

            return HandleExecuteACMethod_PWGroupVB(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
    }
}
