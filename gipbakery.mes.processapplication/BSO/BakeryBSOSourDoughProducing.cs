using gip.bso.manufacturing;
using gip.core.autocomponent;
using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioManufacturing, "en{'Sour dough'}de{'Sauerteig'}", Global.ACKinds.TACBSO, Global.ACStorableTypes.NotStorable, true, true, SortIndex = 50)]
    public class BakeryBSOSourDoughProducing : BSOWorkCenterChild
    {
        #region c'tors

        public BakeryBSOSourDoughProducing(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public const string ClassName = "BakeryBSOSourDoughProducing";

        #endregion

        #region Properties

        private double _FlourActualQuantity;
        [ACPropertyInfo(800, "", "en{'Flour actual q.'}de{'Mehl Ist'}")]
        public double FlourActualQuantity
        {
            get => _FlourActualQuantity;
            set
            {
                _FlourActualQuantity = value;
                OnPropertyChanged("FlourActualQuantity");
            }
        }

        private double _FlourDiffQuantity;
        [ACPropertyInfo(801, "", "en{'Flour difference q.'}de{'Mehl Rest'}")]
        public double FlourDiffQuantity
        {
            get => _FlourDiffQuantity;
            set
            {
                _FlourDiffQuantity = value;
                OnPropertyChanged("FlourDiffQuantity");
            }
        }

        private double _WaterActualQuantity;
        [ACPropertyInfo(802, "", "en{'Water actual q.'}de{'Wasser Ist'}")]
        public double WaterActualQuantity
        {
            get => _WaterActualQuantity;
            set
            {
                _WaterActualQuantity = value;
                OnPropertyChanged("WaterActualQuantity");
            }
        }

        private double _WaterDiffQuantity;
        [ACPropertyInfo(803, "", "en{'Water difference q.'}de{'Wasser Rest'}")]
        public double WaterDiffQuantity
        {
            get => _WaterDiffQuantity;
            set
            {
                _WaterDiffQuantity = value;
                OnPropertyChanged("WaterDiffQuantity");
            }
        }

        private short _NextStage;
        [ACPropertyInfo(804, "", "en{'Next stage'}de{'Nach. Stufe'}")]
        public short NextStage
        {
            get => _NextStage;
            set
            {
                _NextStage = value;
                OnPropertyChanged("NextStage");
            }
        }

        private DateTime _StartDateTime;
        [ACPropertyInfo(805, "", "en{'Start time'}de{'Start zeit'}")]
        public DateTime StartDateTime
        {
            get => _StartDateTime;
            set
            {
                _StartDateTime = value;
                OnPropertyChanged("StartDateTime");
            }
        }

        private DateTime _ReadyForDosing;
        [ACPropertyInfo(805, "", "en{'Ready for dosing'}de{'Dosierbereitschaft'}")]
        public DateTime ReadyForDosing
        {
            get => _ReadyForDosing;
            set
            {
                _ReadyForDosing = value;
                OnPropertyChanged("ReadyForDosing");
            }
        }

        #endregion

        #region Methods

        public override void Activate(ACComponent selectedProcessModule)
        {
            
        }

        public override void DeActivate()
        {
            
        }

        [ACMethodInfo("","en{'Acknowledge - Start'}de{'Quittieren - Start'}",800, true)]
        public void Acknowledge()
        {

        }

        public bool IsEnabledAcknowledge()
        {
            return true;
        }

        [ACMethodInfo("", "en{'Clean'}de{'Reinigen'}", 801, true)]
        public void Clean()
        {

        }

        public bool IsEnabledClean()
        {
            return true;
        }

        [ACMethodInfo("", "en{'Switch availability'}de{'Schalterverfügbarkeit'}", 802, true)]
        public void SwitchAvailability()
        {

        }

        public bool IsEnabledSwitchAvailability()
        {
            return true;
        }

        #endregion
    }
}
