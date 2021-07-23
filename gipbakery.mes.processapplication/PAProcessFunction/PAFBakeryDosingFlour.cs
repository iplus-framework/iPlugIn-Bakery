using gip.core.datamodel;
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
        public PAFBakeryDosingFlour(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACPostInit()
        {
            bool result = base.ACPostInit();

            DosTimeFlour.PropertyChanged += DosTimeFlour_PropertyChanged;

            if (CurrentScaleForWeighing != null)
            {
                ActualWeightProp = CurrentScaleForWeighing.ActualWeight;
                ActualWeightProp.PropertyChanged += ActualWeight_PropertyChanged;
            }

            return result;
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            DosTimeFlour.PropertyChanged -= DosTimeFlour_PropertyChanged;
            if(ActualWeightProp != null)
            {
                ActualWeightProp.PropertyChanged -= ActualWeight_PropertyChanged;
                ActualWeightProp = null;
            }

            return base.ACDeInit(deleteACClassTask);
        }

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

        public override void SMStarting()
        {
            base.SMStarting();
            double? targetQuantity = CurrentACMethod.ValueT["TargetQuantity"] as double?;
            if (targetQuantity.HasValue)
            {
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    TargetQuantity = targetQuantity.Value;
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
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    if (TargetQuantity == null || ActualWeightProp == null)
                        return;

                    FlourDiffQuantity.ValueT = ActualWeightProp.ValueT - TargetQuantity.Value;
                }
            }
        }

        private void DosTimeFlour_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Const.ValueT)
            {
                if (DosTimeFlourCorrectionFactor > 1)
                    DosTimeFlourCorrectionFactor = 1;
                else if (DosTimeFlourCorrectionFactor < 0.00001)
                    DosTimeFlourCorrectionFactor = 0.00001;

                double timeNew = (DosTimeFlour.ValueT - DosTimeFlourCorrected) * DosTimeFlourCorrectionFactor;
                DosTimeFlourCorrected += timeNew;
            }
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

        [ACPropertyInfo(801, "", "en{'Dosing time flour [s/kg]'}de{'Dosierzeit Mehl [s/kg]'}", IsPersistable = true)]
        public double DosTimeFlourCorrected
        {
            get;
            set;
        }

        [ACPropertyInfo(801, "", "en{'Max dosing time flour [s/kg]'}de{'Max. Dosierzeit für Mehl [s/kg]'}", IsPersistable = true, DefaultValue = 2000.0)]
        public double DosTimeFlourMax
        {
            get;
            set;
        }

        [ACPropertyInfo(802, "", "en{'Min dosing time flour [s/kg]'}de{'Min. Dosierzeit Mehl [s/kg]'}", IsPersistable = true, DefaultValue = 0.5)]
        public double DosTimeFlourMin
        {
            get;
            set;
        }

        [ACPropertyInfo(803, "", "en{'Dosing time flour correction factor [0 - 1]'}de{'Dosierzeit Mehl-Korrekturfaktor [0 - 1]'}", IsPersistable = true, DefaultValue = 0.5)]
        public double DosTimeFlourCorrectionFactor
        {
            get;
            set;
        }

        public double GetFlourDosingTime()
        {
            if (DosTimeFlourCorrected > DosTimeFlourMax)
                return DosTimeFlourMax;
            else if (DosTimeFlourCorrected < DosTimeFlourMin)
                return DosTimeFlourMin;

            return DosTimeFlourCorrected;
        }

        [ACMethodInfo("","",9999)]
        public string  GetFlourDosingScale()
        {
            return CurrentScaleForWeighing?.ACUrl;
        }

    }
}
