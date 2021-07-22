using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using gip.mes.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Workflow group'}de{'Workflow Gruppe'}", Global.ACKinds.TPWGroup, Global.ACStorableTypes.Optional, false, PWProcessFunction.PWClassName, true)]
    public class PWBakeryGroupFermentation : PWGroupVB
    {
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

            method.ParameterValueList.Add(new ACValue("DoseInSourProdSimultaneously ", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("DoseInSourProdSimultaneously", "en{'Dose in sour dough production simultaneously'}de{'Dosiere in der Sauerteigproduktion gleichzeitig'}");
            method.ParameterValueList.Add(new ACValue("SourProdDosingUnit", typeof(double), 10.0, Global.ParamOption.Optional));
            paramTranslation.Add("SourProdDosingUnit", "en{'Sour dough production dosing unit [kg]'}de{'SauerteigDosiereinheit [kg]'}");
            method.ParameterValueList.Add(new ACValue("SourProdDosingPause", typeof(int), 2, Global.ParamOption.Optional));
            paramTranslation.Add("SourProdDosingPause", "en{'Sour dough production dosing pause [sec]'}de{'SauerteigDospause [sec]'}");

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

        public double SourProdDosingUnit
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("SourProdDosingUnit");
                    if (acValue != null)
                    {
                        return acValue.ParamAsDouble;
                    }
                }
                return 10;
            }
        }

        public int SourProdDosingPause
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("SourProdDosingPause");
                    if (acValue != null)
                    {
                        return acValue.ParamAsInt32;
                    }
                }
                return 2;
            }
        }


        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        public override void SMRunning()
        {
            base.SMRunning();
            CalculateDuration();
        }

        public override void SMIdle()
        {
            base.SMIdle();

            NextFermentationStage.ValueT = 0;
        }

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
                //return;
            }

            List<PWBakeryEndOnTime> endOnTimeNodes = FindChildComponents<PWBakeryEndOnTime>(1);

            int stages = endOnTimeNodes.Count;

            PWBakeryEndOnTime lastNode = endOnTimeNodes.FirstOrDefault(x => x.FindSuccessors<PWDischarging>(true, c => c is PWDischarging, d => d is PWDosing, 0).Any());

            if (lastNode == null)
            {
                //todo error
                return;
            }

            endOnTimeNodes.Remove(lastNode);

            //TODO:write planned end date
            lastNode.EndOnTime.ValueT = DateTime.Now.AddHours(5);

            ReadyForDosingTime.ValueT = lastNode.EndOnTime.ValueT;

            PWBakeryEndOnTime currentNode = lastNode;

            while (true)
            {
                PWBakeryEndOnTime prevEndOnTime = FindPrevEndOnTimeNode(currentNode);
                if (prevEndOnTime == null)
                    break;

                IEnumerable<ACComponent> parallelNodes = FindParallelNodes(prevEndOnTime);

                List<PWBakeryDosing> pwDosings = FindVariableDurationNodes(currentNode, prevEndOnTime, parallelNodes);
                List<PWBaseNodeProcess> fixDurationNodes = FindFixedDurationNodes(currentNode, prevEndOnTime, parallelNodes);

                TimeSpan fixedDuration = CalcualteFixedDuration(fixDurationNodes, stages);
                TimeSpan variableDuration = CalculateVariableDuration(pwDosings, stages);

                TimeSpan durationPerStage = fixedDuration + variableDuration;

                prevEndOnTime.EndOnTime.ValueT = currentNode.EndOnTime.ValueT - durationPerStage;

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

        public virtual List<PWBakeryDosing> FindVariableDurationNodes(PWBakeryEndOnTime currentEndNode, PWBakeryEndOnTime prevEndNode, IEnumerable<ACComponent> parallelNodes)
        {
            // PWNodeEndOnTime has parallel workflow nodes
            if (parallelNodes.Count() > 1)
            {
                return currentEndNode.FindPredecessors<PWBakeryDosing>(true, c => c is PWBakeryDosing, d => d == prevEndNode || parallelNodes.Contains(d), 0);
            }

            return currentEndNode.FindPredecessors<PWBakeryDosing>(true, c => c is PWBakeryDosing, d => d == prevEndNode, 0);

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

        public virtual TimeSpan CalculateVariableDuration(List<PWBakeryDosing> dosingNodes, int stage)
        {
            PAProcessModule processModule = AccessedProcessModule;
            bool doseSim = DoseInSourProdSimultaneously;

            List<TimeSpan> ts = new List<TimeSpan>();

            PAFBakeryDosingWater waterFunc = null;

            foreach (PWBakeryDosing dosingNode in dosingNodes)
            {
                TimeSpan result = dosingNode.CalculateDuration(doseSim, SourProdDosingUnit, SourProdDosingPause, processModule, out waterFunc);
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

        public virtual TimeSpan CalcualteFixedDuration(List<PWBaseNodeProcess> pwNodes, int stage)
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
            if (pwNode.EndOnTime.ValueT != ReadyForDosingTime.ValueT)
            {
                StartNextFermentationStageTime.ValueT = pwNode.EndOnTime.ValueT;
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



        public static bool HandleExecuteACMethod_PWBakeryGroupFermentation(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PWGroupVB(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

    }
}
