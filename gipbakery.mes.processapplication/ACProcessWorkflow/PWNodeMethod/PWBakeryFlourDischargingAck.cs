using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Flour discharging acknowledge'}de{'Mehlaustragsquittung'}", Global.ACKinds.TPWNodeStatic, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryFlourDischargingAck : PWNodeUserAck
    {
        #region c'tors
        static PWBakeryFlourDischargingAck()
        {
            RegisterExecuteHandler(typeof(PWBakeryFlourDischargingAck), HandleExecuteACMethod_PWBakeryFlourDischargingAck);
            
            List<ACMethodWrapper> wrappers = ACMethod.OverrideFromBase(typeof(PWBakeryFlourDischargingAck), ACStateConst.SMStarting);
            if (wrappers != null)
            {
                foreach (ACMethodWrapper wrapper in wrappers)
                {
                    wrapper.Method.ParameterValueList.Add(new ACValue("SkipIfNoComp", typeof(bool), false, Global.ParamOption.Required));
                    wrapper.ParameterTranslation.Add("SkipIfNoComp", "en{'Skip if no components dosed'}de{'Überspringe wenn keine Komponente dosiert'}");
                    wrapper.Method.ParameterValueList.Add(new ACValue("AutoAck", typeof(bool), false, Global.ParamOption.Optional));
                    wrapper.ParameterTranslation.Add("AutoAck", "en{'Auto acknowledge'}de{'Automatisch quittieren'}");
                }
            }
        }

        public new const string PWClassName = "PWBakeryFlourDischargingAck";

        public PWBakeryFlourDischargingAck(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }
        #endregion

        #region Properties
        protected bool SkipIfNoComp
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("SkipIfNoComp");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }

        protected bool AutoAck
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("AutoAck");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }

        public override bool IsSkippable
        {
            get
            {
                return false;
            }
        }


        /// <summary>
        /// This property is a reference to the BakeryReceivingPoint.CoverDown-Property if a Source-Value is bound
        /// </summary>
        private IACContainerTNet<bool> _BoundCoverDownProperty;
        public IACContainerTNet<bool> BoundCoverDownProperty
        {
            get
            {
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    return _BoundCoverDownProperty;
                }
            }
            set
            {
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    if (_BoundCoverDownProperty != null)
                        _BoundCoverDownProperty.PropertyChanged -= IsCoverDown_PropertyChanged;
                    _BoundCoverDownProperty = value;
                    if (_BoundCoverDownProperty != null)
                        _BoundCoverDownProperty.PropertyChanged += IsCoverDown_PropertyChanged;
                }
            }
        }

        bool? _HasRunSomeDosings;
        public bool? HasRunSomeDosings
        {
            get
            {
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    return _HasRunSomeDosings;
                }
            }
            set
            {
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    _HasRunSomeDosings = value;
                }
            }
        }

        #endregion

        #region Methods
        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        public override void SMRunning()
        {
            BakeryReceivingPoint recvPoint = ParentPWGroup?.AccessedProcessModule as BakeryReceivingPoint;
            if (recvPoint == null)
            {
                base.SMRunning();
                UnSubscribeToProjectWorkCycle();
                return;
            }

            if (!HasRunSomeDosings.HasValue && SkipIfNoComp)
            {
                List<PWDosing> previousDosings = FindPredecessors<PWDosing>(false,
                                                                    c => c is PWDosing,
                                                                    c => (c is PWNodeOr && (c as PWNodeOr).PWPointIn.ConnectionList.Where(d => d.ValueT is PWDosingLoop).Any()) || c is PWDosing,
                                                                    40);
                if (previousDosings == null || !previousDosings.Any())
                {
                    List<PWDischarging> dischargings = FindPredecessors<PWDischarging>(false,
                                                                    c => c is PWDischarging,
                                                                    c => (c is PWNodeOr && (c as PWNodeOr).PWPointIn.ConnectionList.Where(d => d.ValueT is PWDosingLoop).Any()) || c is PWDischarging,
                                                                    40);
                    if (dischargings != null && dischargings.Any())
                    {
                        PWDischarging predecessorDis = dischargings.FirstOrDefault();
                        if (predecessorDis != null)
                        {
                            previousDosings = PWDosing.FindPreviousDosingsInPWGroup<PWDosing>(predecessorDis);
                        }
                    }
                }
                HasRunSomeDosings = false;
                if (previousDosings != null)
                    HasRunSomeDosings = previousDosings.Where(c => c.HasRunSomeDosings 
                                                                    || c.CurrentACState == ACStateEnum.SMRunning).Any();
                if (!HasRunSomeDosings.Value)
                {
                    AckStart();
                    return;
                }
            }

            IACPropertyNetTarget isCoverDown = recvPoint.IsCoverDown as IACPropertyNetTarget;
            if (isCoverDown == null || isCoverDown.Source == null)
            {
                base.SMRunning();
                UnSubscribeToProjectWorkCycle();
                return;
            }

            bool resetCover = false;
            if (IsSimulationOn && BoundCoverDownProperty == null)
                resetCover = true;

            IACContainerTNet<bool> boundCoverDownProperty = isCoverDown as IACContainerTNet<bool>;
            BoundCoverDownProperty = boundCoverDownProperty;
            if (resetCover)
                boundCoverDownProperty.ValueT = false;
            else if (boundCoverDownProperty.ValueT || AutoAck)
            {
                AckStart();
                return;
            }

            base.SMRunning();
        }

        public override void SMIdle()
        {
            base.SMIdle();
            ResetMembers();
        }

        public override void Recycle(IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
        {
            ResetMembers();
            base.Recycle(content, parentACObject, parameter, acIdentifier);
        }

        private void IsCoverDown_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<bool> boundCoverDownProperty = BoundCoverDownProperty;
                if (boundCoverDownProperty != null && boundCoverDownProperty.ValueT)
                {
                    AckStart();
                }
            }
        }

        protected override void OnAckStart(bool skipped)
        {
            IACContainerTNet<bool> boundCoverDownProperty = BoundCoverDownProperty;
            if (boundCoverDownProperty != null && !boundCoverDownProperty.ValueT)
                boundCoverDownProperty.ValueT = true;
            base.OnAckStart(skipped);
        }

        public void ResetMembers()
        {
            BoundCoverDownProperty = null;
            HasRunSomeDosings = null;
        }

        public static bool HandleExecuteACMethod_PWBakeryFlourDischargingAck(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
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
        #endregion

        #region Diagnose
        protected override void DumpPropertyList(XmlDocument doc, XmlElement xmlACPropertyList, ref DumpStats dumpStats)
        {
            base.DumpPropertyList(doc, xmlACPropertyList, ref dumpStats);

            XmlElement xmlChild = xmlACPropertyList["HasRunSomeDosings"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("HasRunSomeDosings");
                if (xmlChild != null)
                    xmlChild.InnerText = HasRunSomeDosings.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["AutoAck"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("AutoAck");
                if (xmlChild != null)
                    xmlChild.InnerText = HasRunSomeDosings.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }
        }
        #endregion
    }
}
