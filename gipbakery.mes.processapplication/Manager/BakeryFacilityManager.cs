using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.datamodel;
using gip.mes.facility;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioSystem, "en{'BakeryFacilityManager'}de{'BakeryFacilityManager'}", Global.ACKinds.TPARole, Global.ACStorableTypes.NotStorable, false, false)]
    public class BakeryFacilityManager : FacilityManager
    {
        #region c´tors
        public BakeryFacilityManager(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACInit(Global.ACStartTypes startChildMode = Global.ACStartTypes.Automatic)
        {
            bool result = base.ACInit(startChildMode);
            if (!result)
                return result;
            return result;
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            return base.ACDeInit(deleteACClassTask);
        }
        #endregion

        #region Properties

        public class MachineWithLotChange
        {
            public MachineWithLotChange(gip.core.datamodel.ACClass acClass)
            {
                _ACClass = acClass;
                _LotChangeProperty = acClass.GetProperty(nameof(BakeryCartoner.NewOutputLotOnLotChange));
            }

            private gip.core.datamodel.ACClass _ACClass;
            public gip.core.datamodel.ACClass ACClass
            {
                get
                {
                    return _ACClass;
                }
            }

            private gip.core.datamodel.ACClassProperty _LotChangeProperty;
            public gip.core.datamodel.ACClassProperty LotChangeProperty
            {
                get
                {
                    return _LotChangeProperty;
                }
            }

            public bool LotIsChangeable
            {
                get
                {
                    if (_LotChangeProperty == null)
                        return false;
                    bool changeable = false;
                    if (Boolean.TryParse(_LotChangeProperty.XMLValue, out changeable))
                        return changeable;
                    return false;
                }
            }

        }

        private Dictionary<string, MachineWithLotChange> _MachinesWithLotChange = null;
        public Dictionary<string, MachineWithLotChange> MachinesWithLotChange
        {
            get
            {
                using (ACMonitor.Lock(this._20015_LockValue))
                {
                    if (_MachinesWithLotChange != null)
                        return _MachinesWithLotChange;
                }
                gip.core.datamodel.ACClass[] derivedClasses = null;
                gip.core.datamodel.ACClass baseClassOfCartooner = gip.core.datamodel.Database.GlobalDatabase.GetACType(typeof(BakeryCartoner));
                if (baseClassOfCartooner != null)
                {
                    derivedClasses = baseClassOfCartooner.DerivedClassesInProjects;
                    using (ACMonitor.Lock(this._20015_LockValue))
                    {
                        _MachinesWithLotChange = derivedClasses.ToDictionary(key => key.ACURLComponentCached, val => new MachineWithLotChange(val));
                        return _MachinesWithLotChange;
                    }
                }
                return null;
            }
        }

        public MachineWithLotChange IsMachineForLotChange(string propertyACUrl)
        {
            if (String.IsNullOrEmpty(propertyACUrl))
                return null;
            var dict = MachinesWithLotChange;
            if (dict == null)
                return null;
            MachineWithLotChange machine = null;
            if (dict.TryGetValue(propertyACUrl, out machine))
                return machine;
            return null;
        }

        #endregion

        #region Methods
        protected override bool PreFacilityBooking(ACMethodBooking acMethodBooking)
        {
            bool canBook = base.PreFacilityBooking(acMethodBooking);
            if (canBook)
            {
                if (   !String.IsNullOrEmpty(acMethodBooking.PropertyACUrl)
                    && acMethodBooking.PartslistPosRelation != null
                    && acMethodBooking.OutwardFacilityCharge != null
                    && acMethodBooking.OutwardFacilityCharge.FacilityLotID.HasValue)
                {
                    MachineWithLotChange machine = IsMachineForLotChange(acMethodBooking.PropertyACUrl);
                    if (   machine != null
                        && machine.LotIsChangeable
                        && acMethodBooking.PartslistPosRelation.Foreflushing)
                    {
                        Guid? lastLot = 
                            acMethodBooking.DatabaseApp.FacilityBookingCharge.Where(c => c.ProdOrderPartslistPosRelationID == acMethodBooking.PartslistPosRelation.ProdOrderPartslistPosRelationID)
                            .OrderByDescending(c => c.FacilityBookingChargeNo)
                            .Select(c => c.OutwardFacilityLotID)
                            .FirstOrDefault();
                        if (lastLot.HasValue 
                            && lastLot != acMethodBooking.OutwardFacilityCharge.FacilityLotID
                            && acMethodBooking.PartslistPosRelation.ProdOrderBatchID.HasValue)
                        {
                            ProdOrderPartslistPos posForInwardPosting;
                            if (!acMethodBooking.PartslistPosRelation.TargetProdOrderPartslistPos.Foreflushing)
                                posForInwardPosting = acMethodBooking.PartslistPosRelation.TargetProdOrderPartslistPos;
                            else
                            {
                                posForInwardPosting = acMethodBooking.DatabaseApp.ProdOrderPartslistPosRelation
                                    .Include(c => c.TargetProdOrderPartslistPos)
                                    .Include(c => c.TargetProdOrderPartslistPos.Material)
                                    .Include(c => c.TargetProdOrderPartslistPos.BasedOnPartslistPos)
                                    .Include(c => c.TargetProdOrderPartslistPos.BasedOnPartslistPos.Material)
                                    .Where(c => c.ProdOrderBatchID == acMethodBooking.PartslistPosRelation.ProdOrderBatchID.Value
                                            && c.TargetProdOrderPartslistPos.MaterialPosTypeIndex == (short)GlobalApp.MaterialPosTypes.InwardPartIntern
                                            && c.TargetProdOrderPartslistPos.ParentProdOrderPartslistPosID != null)
                                    .AsEnumerable()
                                    .Where(c => c.TargetProdOrderPartslistPos.IsFinalMixureBatch)
                                    .Select(c => c.TargetProdOrderPartslistPos)
                                    .FirstOrDefault();
                            }
                            if (posForInwardPosting != null)
                            {
                                string secondaryKey = Root.NoManager.GetNewNo(acMethodBooking.DatabaseApp, typeof(FacilityLot), FacilityLot.NoColumnName, FacilityLot.FormatNewNo, this);
                                FacilityLot facilityLot = FacilityLot.NewACObject(acMethodBooking.DatabaseApp, null, secondaryKey);
                                posForInwardPosting.FacilityLot = facilityLot;
                                ProdOrderPartslistPosFacilityLot subLot = ProdOrderPartslistPosFacilityLot.NewACObject(acMethodBooking.DatabaseApp, posForInwardPosting);
                                subLot.FacilityLot = facilityLot;
                                posForInwardPosting.ProdOrderPartslistPosFacilityLot_ProdOrderPartslistPos.Add(subLot);
                            }
                        }
                    }
                }
            }
            return canBook;
        }
        #endregion

    }
}