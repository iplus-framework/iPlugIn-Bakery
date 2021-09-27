﻿using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using gip.mes.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

//TODO: time switch summer/winter
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

        #endregion

        #region Properties

        private ACMonitorObject _65100_MemebersLock = new ACMonitorObject(65100);

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
            DateTime dt;
            using (ACMonitor.Lock(_65100_MemebersLock))
            {
                dt = StartNextFermentationStageTime.ValueT;
            }

            if (dt > DateTime.MinValue)
            {
                if (dt < DateTime.Now)
                {
                    string orderInfo = AccessedProcessModule?.OrderInfo.ValueT;
                    orderInfo = orderInfo.Replace("\r", "").Replace("\n", " ");

                    //Warning50041: The production order {0} is planned to start at {1} but now is {2}. Please take a look.

                    Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "CheckIfStartIsTooLate(10)", 256, "Warning50041", 
                                      orderInfo, dt, DateTime.Now);

                    OnNewAlarmOccurred(ProcessAlarm, msg, true);
                    if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                    {
                        Messages.LogMessageMsg(msg);
                    }
                    
                    UnSubscribeToProjectWorkCycle();
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
                    //Error
                    return;
                }

                ProdOrderBatchPlan batchPlan = prodOrderBatch.ProdOrderBatchPlan;
                if (batchPlan == null)
                {
                    //Error;
                    return;
                }

                if (batchPlan.ScheduledEndDate != null)
                    plannedEndTime = batchPlan.ScheduledEndDate.Value;
            }

            if (plannedEndTime == DateTime.MinValue)
            {
                //Error

                Msg msg = new Msg(eMsgLevel.Error, "Scheduled end date is not configured!");
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
                //todo error
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

                        //change time only once, all predecessors will be calcualted on this changed node end time
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
                    //TODO:Error
                    return;
                }

                if (tFacility == null)
                {
                    //TODO:Error
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
                //todo error
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
            return HandleExecuteACMethod_PWGroupVB(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
    }
}
