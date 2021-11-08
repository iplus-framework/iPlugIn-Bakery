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

            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("MessageText", typeof(string), "", Global.ParamOption.Required));
            paramTranslation.Add("MessageText", "en{'Question text'}de{'Abfragetext'}");

            method.ParameterValueList.Add(new ACValue("SkipIfNoComp", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("SkipIfNoComp", "en{'Skip if no components dosed'}de{'Überspringe wenn keine Komponente dosiert'}");

            var wrapper = new ACMethodWrapper(method, "en{'Flour discharging acknowledge'}de{'Mehlaustragsquittung'}", typeof(PWBakeryFlourDischargingAck), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryFlourDischargingAck), ACStateConst.SMStarting, wrapper);
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


        /// <summary>
        /// This property is a reference to the BakeryReceivingPoint.CoverDown-Property if a Source-Value is bound
        /// </summary>
        private IACContainerTNet<bool> _BoundCoverDownProperty;

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
            if (IsSimulationOn && _BoundCoverDownProperty == null)
                resetCover = true;

            _BoundCoverDownProperty = isCoverDown as IACContainerTNet<bool>;
            if (resetCover)
                _BoundCoverDownProperty.ValueT = false;
            else if (_BoundCoverDownProperty.ValueT)
            {
                AckStart();
                return;
            }

            _BoundCoverDownProperty.PropertyChanged += IsCoverDown_PropertyChanged;
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
                if (_BoundCoverDownProperty != null && _BoundCoverDownProperty.ValueT)
                {
                    AckStart();
                }
            }
        }

        public override void AckStart()
        {
            if (_BoundCoverDownProperty != null && !_BoundCoverDownProperty.ValueT)
                _BoundCoverDownProperty.ValueT = true;
            base.AckStart();
        }

        public void ResetMembers()
        {
            if (_BoundCoverDownProperty != null)
            {
                _BoundCoverDownProperty.PropertyChanged -= IsCoverDown_PropertyChanged;
                _BoundCoverDownProperty = null;
            }
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
        protected override void DumpPropertyList(XmlDocument doc, XmlElement xmlACPropertyList)
        {
            base.DumpPropertyList(doc, xmlACPropertyList);

            XmlElement xmlChild = xmlACPropertyList["HasRunSomeDosings"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("HasRunSomeDosings");
                if (xmlChild != null)
                    xmlChild.InnerText = HasRunSomeDosings.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }
        }
        #endregion
    }
}
