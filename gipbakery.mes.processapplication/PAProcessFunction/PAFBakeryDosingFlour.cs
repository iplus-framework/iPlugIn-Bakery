using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.facility;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    /// <summary>
    /// Represents the dosing function for flour in a sour dough production
    /// </summary>
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Dosing flour in sour dough'}de{'Dosierung von Mehl in Sauerteig'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, PWBakeryDosing.PWClassName, true)]
    public class PAFBakeryDosingFlour : PAFDosing
    {
        #region c'tors

        public PAFBakeryDosingFlour(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
            _HandleStoppOrAbort = new ACPropertyConfigValue<bool>(this, "HandleStoppOrAbort", false);
        }

        public override bool ACInit(Global.ACStartTypes startChildMode = Global.ACStartTypes.Automatic)
        {
            bool result = base.ACInit(startChildMode);
            _ = HandleStoppOrAbort;
            return result;
        }

        public override bool ACPostInit()
        {
            bool result = base.ACPostInit();

            (DosTimeFlour as IACPropertyNetServer).ValueUpdatedOnReceival += DosTimeFlour_ValueUpdatedOnReceival;
            StateLackOfMaterial.PropertyChanged += StateLackOfMaterial_PropertyChanged; 

            if (CurrentScaleForWeighing != null)
            {
                ActualWeightProp = CurrentScaleForWeighing.ActualWeight;
                ActualWeightProp.PropertyChanged += ActualWeight_PropertyChanged;
            }

            return result;
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            StateLackOfMaterial.PropertyChanged -= StateLackOfMaterial_PropertyChanged;
            (DosTimeFlour as IACPropertyNetServer).ValueUpdatedOnReceival -= DosTimeFlour_ValueUpdatedOnReceival;
            if (ActualWeightProp != null)
            {
                ActualWeightProp.PropertyChanged -= ActualWeight_PropertyChanged;
                ActualWeightProp = null;
            }

            return base.ACDeInit(deleteACClassTask);
        }

        #endregion

        #region Properties

        private IACContainerTNet<double> ActualWeightProp
        {
            get;
            set;
        }

        private double? TargetQuantity
        {
            get;
            set;
        }

        [ACPropertyBindingTarget(800, "", "en{'Dosing time flour [s/kg]'}de{'Dosierzeit Mehl [s/kg]'}", "", true, true)]
        public IACContainerTNet<double> DosTimeFlour
        {
            get;
            set;
        }

        [ACPropertyBindingTarget]
        public IACContainerTNet<double> FlourDiffQuantity
        {
            get;
            set;
        }

        [ACPropertyInfo(801, "", "en{'Last read value [s/kg]'}de{'Zuletzt gelesener Wert [s/kg]'}", IsPersistable = true)]
        public double LastReadDosTime
        {
            get;
            set;
        }

        [ACPropertyInfo(802, "", "en{'Dosing time flour [s/kg]'}de{'Dosierzeit Mehl [s/kg]'}", IsPersistable = true)]
        public double DosTimeFlourCorrected
        {
            get;
            set;
        }

        [ACPropertyInfo(803, "", "en{'Max dosing time flour [s/kg]'}de{'Max. Dosierzeit für Mehl [s/kg]'}", IsPersistable = true, DefaultValue = 2000.0)]
        public double DosTimeFlourMax
        {
            get;
            set;
        }

        [ACPropertyInfo(804, "", "en{'Min dosing time flour [s/kg]'}de{'Min. Dosierzeit Mehl [s/kg]'}", IsPersistable = true, DefaultValue = 0.5)]
        public double DosTimeFlourMin
        {
            get;
            set;
        }

        [ACPropertyInfo(805, "", "en{'Dosing time flour correction factor [0 - 1]'}de{'Dosierzeit Mehl-Korrekturfaktor [0 - 1]'}", IsPersistable = true, DefaultValue = 0.5)]
        public double DosTimeFlourCorrectionFactor
        {
            get;
            set;
        }

        protected ACPropertyConfigValue<bool> _HandleStoppOrAbort;
        [ACPropertyConfig("en{'Execute Stop/Abort-Comand when changing source'}de{'Führe Stop/Abbruch-Kommando aus bei Quellenwechsel'}")]
        public bool HandleStoppOrAbort
        {
            get
            {
                return _HandleStoppOrAbort.ValueT;
            }
        }


        #endregion

        #region Methods

        public override void SMStarting()
        {
            base.SMStarting();
            if(CurrentACMethod.ValueT !=null)
            {
                double? targetQuantity = CurrentACMethod.ValueT["TargetQuantity"] as double?;
                if (targetQuantity.HasValue)
                {
                    using (ACMonitor.Lock(_20015_LockValue))
                    {
                        TargetQuantity = targetQuantity.Value;
                    }
                }
            }
        }

        public override void SMIdle()
        {
            base.SMIdle();
            using (ACMonitor.Lock(_20015_LockValue))
            {
                TargetQuantity = null;
            }
        }

        private void ActualWeight_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                IACContainerTNet<double> senderProp = sender as IACContainerTNet<double>;
                if (senderProp != null)
                {
                    double? targetQuantity = null;
                    using (ACMonitor.Lock(_20015_LockValue))
                    {
                        targetQuantity = TargetQuantity;
                    }

                    if (targetQuantity == null)
                        return;

                    FlourDiffQuantity.ValueT = senderProp.ValueT - targetQuantity.Value;
                }
            }
        }

        private void DosTimeFlour_ValueUpdatedOnReceival(object sender, ACPropertyChangedEventArgs e, ACPropertyChangedPhase phase)
        {
            if (phase == ACPropertyChangedPhase.BeforeBroadcast)
                return;

            if (Math.Abs(LastReadDosTime - DosTimeFlour.ValueT) <= FacilityConst.C_ZeroCompare)
                return;
            if (   DosTimeFlour.ValueT > DosTimeFlourMax 
                || DosTimeFlour.ValueT < DosTimeFlourMin)
                return;

            if (DosTimeFlourCorrectionFactor > 1)
                DosTimeFlourCorrectionFactor = 1;
            else if (DosTimeFlourCorrectionFactor < 0.00001)
                DosTimeFlourCorrectionFactor = 0.00001;

            // If initial setting
            if (DosTimeFlourCorrected <= 0.0001)
                DosTimeFlourCorrected = DosTimeFlour.ValueT;
            else
            {
                double timeNew = (DosTimeFlour.ValueT - DosTimeFlourCorrected) * DosTimeFlourCorrectionFactor;
                DosTimeFlourCorrected += timeNew;
            }
            LastReadDosTime = DosTimeFlour.ValueT;
        }

        public double GetFlourDosingTime()
        {
            if (DosTimeFlourCorrected > DosTimeFlourMax)
                return DosTimeFlourMax;
            else if (DosTimeFlourCorrected < DosTimeFlourMin)
                return DosTimeFlourMin;

            return DosTimeFlourCorrected;
        }

        [ACMethodInfo("", "", 9999)]
        public string GetFlourDosingScale()
        {
            return CurrentScaleForWeighing?.ACUrl;
        }

        protected override void OnSourceChangeStoppOrAbort()
        {
            if (HandleStoppOrAbort)
                base.OnSourceChangeStoppOrAbort();
        }

        public override void OnSiloStateChanged(PAMSilo silo, bool outwardEnabled)
        {
            if (IsDosingActiveFromSilo(silo))
            {
                if (!outwardEnabled)
                {
                    ForceSetLackOfMaterial();
                }
            }
            base.OnSiloStateChanged(silo, outwardEnabled);
        }

        public override void SetAbortReasonEmpty()
        {
            if (!IsEnabledSetAbortReasonEmpty())
                return;
            if (HandleStoppOrAbort)
                base.SetAbortReasonEmpty();
            else
                DosingAbortReason.ValueT = PADosingAbortReason.EmptySourceNextSource;
        }

        private void StateLackOfMaterial_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IACContainerTNet<PANotifyState> senderProp = sender as IACContainerTNet<PANotifyState>;
            if (senderProp != null && senderProp.ValueT == PANotifyState.Off)
            {
                DosingAbortReason.ValueT = PADosingAbortReason.NotSet;
            }
        }

        protected override void OnHandleAcknowlegdeDosingAlarms()
        {
            PWBakeryDosingPreProd invokingDosNode = null;
            if (this.CurrentTask != null && CurrentACMethod.ValueT != null)
                invokingDosNode = this.CurrentTask.ValueT as PWBakeryDosingPreProd;
            if (invokingDosNode == null || !invokingDosNode.WillSiloChangedWithoutAbort)
            {
                base.OnHandleAcknowlegdeDosingAlarms();
                return;
            }

            if (StateTolerance.ValueT != PANotifyState.Off)
                FaultAckTolerance.ValueT = true;
            if (StateLackOfMaterial.ValueT != PANotifyState.Off)
            {
                if (_LackOfMaterialForced == true)
                {
                    // Only reset if function is aborted or stopped, because silo change will happen with new start
                    // Otherwise StateLackOfMaterial must be hold active for silo change in pwdosing
                    if (CurrentACState >= ACStateEnum.SMCompleted)
                    {
                        StateLackOfMaterial.ValueT = PANotifyState.Off;
                        _LackOfMaterialForced = false;
                    }
                }
                else
                    FaultAckLackOfMaterial.ValueT = true;
            }
            if (StateDosingTime.ValueT != PANotifyState.Off)
                FaultAckDosingTime.ValueT = true;
        }

        #endregion

        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;

            switch (acMethodName)
            {
                case "GetFlourDosingScale":
                    GetFlourDosingScale();
                    return true;

                default:
                    break;
            }

            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }
    }
}
