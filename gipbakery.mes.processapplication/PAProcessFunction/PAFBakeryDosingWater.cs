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
    /// Represents the dosing function for water in a sour dough production
    /// </summary>
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Dosing water in sour dough'}de{'Dosierung von Wasser in Sauerteig'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, PWBakeryDosing.PWClassName, true)]
    public class PAFBakeryDosingWater : PAFDosing
    {
        #region c'tors

        public PAFBakeryDosingWater(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACPostInit()
        {
            bool result = base.ACPostInit();

            (DosTimeWater as IACPropertyNetServer).ValueUpdatedOnReceival += DosTimeWater_ValueUpdatedOnReceival;

            if (CurrentScaleForWeighing != null)
            {
                ActualWeightProp = CurrentScaleForWeighing.ActualWeight;
                ActualWeightProp.PropertyChanged += ActualWeight_PropertyChanged;
            }

            return result;
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            (DosTimeWater as IACPropertyNetServer).ValueUpdatedOnReceival -= DosTimeWater_ValueUpdatedOnReceival;

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

        [ACPropertyBindingTarget(800, "", "en{'Dosing time water [s/kg]'}de{'Dosierzeit Wasser [s/kg]'}", "", true, true)]
        public IACContainerTNet<double> DosTimeWater
        {
            get;
            set;
        }

        [ACPropertyBindingTarget(801, "", "en{'Dosing time water control [sec]'}de{'Dosierzeit Wasser Regelung [sec]'}", "", true, true)]
        public IACContainerTNet<double> DosTimeWaterControl
        {
            get;
            set;
        }

        [ACPropertyBindingTarget]
        public IACContainerTNet<double> WaterDiffQuantity
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

        [ACPropertyInfo(802, "", "en{'Dosing time water [s/kg]'}de{'Dosierzeit Wasser [s/kg]'}", IsPersistable = true)]
        public double DosTimeWaterCorrected
        {
            get;
            set;
        }

        [ACPropertyInfo(803, "", "en{'Max dosing time water [s/kg]'}de{'Max. Dosierzeit Wasser [s/kg]'}", IsPersistable = true, DefaultValue = 2000.0)]
        public double DosTimeWaterMax
        {
            get;
            set;
        }

        [ACPropertyInfo(804, "", "en{'Min dosing time water [s/kg]'}de{'Min. Dosierzeit Wasser [s/kg]'}", IsPersistable = true, DefaultValue = 0.5)]
        public double DosTimeWaterMin
        {
            get;
            set;
        }

        [ACPropertyInfo(805, "", "en{'Dosing time water correction factor [0 - 1]'}de{'Dosierzeit Wasser Korrekturfaktor [0 - 1]'}", IsPersistable = true, DefaultValue = 0.5)]
        public double DosTimeWaterCorrectionFactor
        {
            get;
            set;
        }

        #endregion

        #region Methods

        public override void SMStarting()
        {
            base.SMStarting();
            if(CurrentACMethod.ValueT != null)
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

                    WaterDiffQuantity.ValueT = senderProp.ValueT - targetQuantity.Value;
                }
            }
        }

        private void DosTimeWater_ValueUpdatedOnReceival(object sender, ACPropertyChangedEventArgs e, ACPropertyChangedPhase phase)
        {
            if (phase == ACPropertyChangedPhase.BeforeBroadcast)
                return;

            if (Math.Abs(LastReadDosTime - DosTimeWater.ValueT) <= FacilityConst.C_ZeroCompare)
                return;
            if (   DosTimeWater.ValueT > DosTimeWaterMax 
                || DosTimeWater.ValueT < DosTimeWaterMin)
                return;

            if (DosTimeWaterCorrectionFactor > 1)
                DosTimeWaterCorrectionFactor = 1;
            else if (DosTimeWaterCorrectionFactor < 0.00001)
                DosTimeWaterCorrectionFactor = 0.00001;

            // If initial setting
            if (DosTimeWaterCorrected <= 0.0001)
                DosTimeWaterCorrected = DosTimeWater.ValueT;
            else
            {
                double timeNew = (DosTimeWater.ValueT - DosTimeWaterCorrected) * DosTimeWaterCorrectionFactor;
                DosTimeWaterCorrected += timeNew;
            }
            LastReadDosTime = DosTimeWater.ValueT;
        }

        public double GetWaterDosingTime()
        {
            if (DosTimeWaterCorrected > DosTimeWaterMax)
                return DosTimeWaterMax;
            else if (DosTimeWaterCorrected < DosTimeWaterMin)
                return DosTimeWaterMin;

            return DosTimeWaterCorrected;
        }

        [ACMethodInfo("", "", 9999)]
        public string GetWaterDosingScale()
        {
            return CurrentScaleForWeighing?.ACUrl;
        }

        #endregion

        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;

            switch (acMethodName)
            {
                case "GetWaterDosingScale":
                    GetWaterDosingScale();
                    return true;
            }

            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }
    }
}
