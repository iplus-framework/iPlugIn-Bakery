using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Packaging machine'}de{'Packmaschine'}", Global.ACKinds.TPAProcessModule, Global.ACStorableTypes.Required, false, PWGroupVB.PWClassName, true)]
    public class BakeryPackMachine : PAProcessModuleVB
    {
        #region c'tors
        static BakeryPackMachine()
        {
            RegisterExecuteHandler(typeof(BakeryPackMachine), HandleExecuteACMethod_BakeryPackMachine);
        }

        public BakeryPackMachine(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
            _PAPointMatIn1 = new PAPoint(this, nameof(PAPointMatIn1));
            _PAPointMatOut1 = new PAPoint(this, nameof(PAPointMatOut1));
        }

        public override bool ACInit(Global.ACStartTypes startChildMode = Global.ACStartTypes.Automatic)
        {
            _SemaphoreCapacity = new ACPropertyConfigValue<uint>(this, nameof(SemaphoreCapacity), 0);
            if (!base.ACInit(startChildMode))
                return false;
            return true;
        }

        public override bool ACPostInit()
        {
            _ = SemaphoreCapacity;
            return base.ACPostInit();
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            return base.ACDeInit(deleteACClassTask);
        }

        public override uint OnGetSemaphoreCapacity()
        {
            return SemaphoreCapacity; // Infinite
        }
        #endregion

        #region Points
        PAPoint _PAPointMatIn1;
        [ACPropertyConnectionPoint(9999, "PointMaterial")]
        public PAPoint PAPointMatIn1
        {
            get
            {
                return _PAPointMatIn1;
            }
        }

        PAPoint _PAPointMatOut1;
        [ACPropertyConnectionPoint(9999, "PointMaterial")]
        [ACPointStateInfo(GlobalProcApp.AvailabilityStatePropName, gip.core.processapplication.AvailabilityState.Idle, GlobalProcApp.AvailabilityStateGroupName, "", Global.Operators.none)]
        public PAPoint PAPointMatOut1
        {
            get
            {
                return _PAPointMatOut1;
            }
        }
        #endregion

        #region Properties
        private bool _CanCountPieces;
        [ACPropertyInfo(true, 202, "", "en{'Can count pieces'}de{'Stückzählung möglich'}", "", true)]
        public bool CanCountPieces
        {
            get
            {
                return _CanCountPieces;
            }
            set
            {
                _CanCountPieces = value;
                OnPropertyChanged("CanCountPieces");
            }
        }

        [ACPropertyBindingTarget(203, "Read from PLC", "en{'Piece counter'}de{'Stückzähler'}", "", false, false)]
        public IACContainerTNet<int> PieceCounter { get; set; }

        [ACPropertyBindingTarget(204, "Read from PLC", "en{'Reset Counter'}de{'Zähler Rücksetzen'}", "", false, false)]
        public IACContainerTNet<bool> ResetCounter { get; set; }

        [ACPropertyBindingTarget(205, "Read from PLC", "en{'Activate Counter'}de{'Zähler aktivieren'}", "", false, false)]
        public IACContainerTNet<bool> ActivateCounter { get; set; }


        private ACPropertyConfigValue<uint> _SemaphoreCapacity;
        [ACPropertyConfig("en{'Simultenous orders'}de{'Anzahl gleichzeitiger Aufträge'}")]
        public uint SemaphoreCapacity
        {
            get { return _SemaphoreCapacity.ValueT; }
            set { _SemaphoreCapacity.ValueT = value; }
        }

        #endregion


        #region Execute-Helper-Handlers
        public static bool HandleExecuteACMethod_BakeryPackMachine(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PAProcessModuleVB(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
        #endregion
    }
}
