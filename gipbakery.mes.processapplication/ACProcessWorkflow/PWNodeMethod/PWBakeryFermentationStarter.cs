using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
using gip.mes.datamodel;
using gip.mes.facility;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Fermentation starter'}de{'Anstellgut'}", Global.ACKinds.TPWNodeStatic, Global.ACStorableTypes.Optional, false, "PWProcessFunction", true, "", "", 9999)]
    public class PWBakeryFermentationStarter : PWNodeProcessMethod
    {
        #region c'tors

        static PWBakeryFermentationStarter()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("AutoDetectTolerance", typeof(short?), null, Global.ParamOption.Optional));
            paramTranslation.Add("AutoDetectTolerance", "en{'Auto detect tolerance [%]'}de{'Toleranz automatisch erkennen [%]'}");

            var wrapper = new ACMethodWrapper(method, "en{'Fermentation starter'}de{'Anstellgut'}", typeof(PWBakeryFermentationStarter), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryFermentationStarter), ACStateConst.SMStarting, wrapper);
            RegisterExecuteHandler(typeof(PWBakeryFermentationStarter), HandleExecuteACMethod_PWBakeryFermentationStarter);
        }

        public PWBakeryFermentationStarter(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public const string PWClassName = "PWBakeryFermentationStarter";

        public const string PN_FSTargetQuantity = "FSTargetQuantity";
        public const string MN_AckFermentationStarter = "AckFermentationStarter";

        #endregion

        #region Properties

        private ACMethod _MyConfiguration;
        [ACPropertyInfo(9999)]
        public ACMethod MyConfiguration
        {
            get
            {
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    if (_MyConfiguration != null)
                        return _MyConfiguration;
                }

                var myNewConfig = NewACMethodWithConfiguration();
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    _MyConfiguration = myNewConfig;
                }
                return myNewConfig;
            }
        }

        public int? AutoDetectTolerance
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("AutoDetectTolerance");
                    if (acValue != null)
                    {
                        if (acValue.Value != null)
                            return acValue.ParamAsInt32;
                    }
                }
                return null;
            }
        }

        public PWMethodVBBase ParentPWMethodVBBase
        {
            get
            {
                return ParentRootWFNode as PWMethodVBBase;
            }
        }

        protected FacilityManager ACFacilityManager
        {
            get
            {
                if (ParentPWMethodVBBase == null)
                    return null;
                return ParentPWMethodVBBase.ACFacilityManager as FacilityManager;
            }
        }

        public ACProdOrderManager ProdOrderManager
        {
            get
            {
                PWMethodProduction pwMethodProduction = ParentPWMethod<PWMethodProduction>();
                return pwMethodProduction != null ? pwMethodProduction.ProdOrderManager : null;
            }
        }

        [ACPropertyBindingSource]
        public IACContainerTNet<double?> FSTargetQuantity
        {
            get;
            set;
        }

        private bool _IsUserAck = false;
        private ACMethodBooking _BookParamNotAvailableClone;
        private double _ScaleActualValue;

        #endregion

        #region Methods

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        public override void SMRunning()
        {
            StartFermentationStarter();

            base.SMRunning();
        }

        public override void SMIdle()
        {
            FSTargetQuantity.ValueT = null;
            _ScaleActualValue = 0;

            using (ACMonitor.Lock(_20015_LockValue))
            {
                _IsUserAck = false;
            }
            base.SMIdle();
        }

        public T ParentPWMethod<T>() where T : PWMethodVBBase
        {
            if (ParentRootWFNode == null)
                return null;
            return ParentRootWFNode as T;
        }

        private void StartFermentationStarter()
        {
            PWMethodProduction pwMethodProduction = ParentPWMethod<PWMethodProduction>();
            // If dosing is not for production, then do nothing
            if (pwMethodProduction == null)
                return;

            Msg msg = null;
            PAProcessModule processModule = ParentPWGroup.AccessedProcessModule;
            PAFBakeryYeastProducing preProdFunction = processModule.FindChildComponents<PAFBakeryYeastProducing>().FirstOrDefault();

            if (preProdFunction == null)
            {
                //Error50442: The process function PAFBakerySourDoughProducing can not be found on the process module {0}. Please check your configuration.";
                msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(10)", 176, "Error50442", processModule.ACCaption);
                OnNewAlarmOccurred(ProcessAlarm, msg);
                return;
            }

            PAEScaleBase scale = preProdFunction.GetFermentationStarterScale();
            if (scale == null)
            {
                //Error50443: The scale object for fermentation starter can not be found! Please configure scale object on the function PAFBakerySourDoughProducing for {0}.
                msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(20)", 66, "Error50443", processModule.ACCaption);
                OnNewAlarmOccurred(ProcessAlarm, msg);
                return;
            }

            _ScaleActualValue = scale.ActualValue.ValueT;

            using (var dbIPlus = new Database())
            {
                using (var dbApp = new DatabaseApp(dbIPlus))
                {
                    ProdOrderPartslistPos endBatchPos = pwMethodProduction.CurrentProdOrderPartslistPos.FromAppContext<ProdOrderPartslistPos>(dbApp);
                    if (pwMethodProduction.CurrentProdOrderBatch == null)
                    {
                        // Error50444: No batch assigned to last intermediate material of this workflow
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(30)", 200, "Error50444");

                        if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                            Messages.LogError(this.GetACUrl(), msg.ACIdentifier, msg.InnerMessage);
                        OnNewAlarmOccurred(ProcessAlarm, msg, false);
                        return;
                    }

                    var contentACClassWFVB = ContentACClassWF.FromAppContext<gip.mes.datamodel.ACClassWF>(dbApp);
                    ProdOrderBatch batch = pwMethodProduction.CurrentProdOrderBatch.FromAppContext<ProdOrderBatch>(dbApp);
                    ProdOrderBatchPlan batchPlan = batch.ProdOrderBatchPlan;
                    MaterialWFConnection matWFConnection = null;
                    if (batchPlan != null && batchPlan.MaterialWFACClassMethodID.HasValue)
                    {
                        matWFConnection = dbApp.MaterialWFConnection
                                                .Where(c => c.MaterialWFACClassMethod.MaterialWFACClassMethodID == batchPlan.MaterialWFACClassMethodID.Value
                                                        && c.ACClassWFID == contentACClassWFVB.ACClassWFID)
                                                .FirstOrDefault();
                    }
                    else
                    {
                        PartslistACClassMethod plMethod = endBatchPos.ProdOrderPartslist.Partslist.PartslistACClassMethod_Partslist.FirstOrDefault();
                        if (plMethod != null)
                        {
                            matWFConnection = dbApp.MaterialWFConnection
                                                    .Where(c => c.MaterialWFACClassMethod.MaterialWFACClassMethodID == plMethod.MaterialWFACClassMethodID
                                                            && c.ACClassWFID == contentACClassWFVB.ACClassWFID)
                                                    .FirstOrDefault();
                        }
                        else
                        {
                            matWFConnection = contentACClassWFVB.MaterialWFConnection_ACClassWF
                                .Where(c => c.MaterialWFACClassMethod.MaterialWFID == endBatchPos.ProdOrderPartslist.Partslist.MaterialWFID
                                            && c.MaterialWFACClassMethod.PartslistACClassMethod_MaterialWFACClassMethod.Where(d => d.PartslistID == endBatchPos.ProdOrderPartslist.PartslistID).Any())
                                .FirstOrDefault();
                        }
                    }

                    if (matWFConnection == null)
                    {
                        // Error50445: No relation defined between Workflownode and intermediate material in Materialworkflow
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(40)", 241, "Error50445");

                        if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                            Messages.LogError(this.GetACUrl(), msg.ACIdentifier, msg.InnerMessage);
                        OnNewAlarmOccurred(ProcessAlarm, msg, false);
                        return;
                    }

                    // Find intermediate position which is assigned to this Dosing-Workflownode
                    var currentProdOrderPartslist = endBatchPos.ProdOrderPartslist.FromAppContext<ProdOrderPartslist>(dbApp);
                    ProdOrderPartslistPos intermediatePosition = currentProdOrderPartslist.ProdOrderPartslistPos_ProdOrderPartslist
                        .Where(c => c.MaterialID.HasValue && c.MaterialID == matWFConnection.MaterialID
                            && c.MaterialPosType == GlobalApp.MaterialPosTypes.InwardIntern
                            && !c.ParentProdOrderPartslistPosID.HasValue).FirstOrDefault();
                    if (intermediatePosition == null)
                    {
                        // Error50446: Intermediate product line not found which is assigned to this workflownode.
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(50)", 258, "Error50446");

                        if (IsAlarmActive(ProcessAlarm, msg.Message) == null)
                            Messages.LogError(this.GetACUrl(), msg.ACIdentifier, msg.InnerMessage);
                        OnNewAlarmOccurred(ProcessAlarm, msg, false);
                        return;
                    }

                    ProdOrderPartslistPos intermediateChildPos = null;
                    // Lock, if a parallel Dosing also creates a child Position for this intermediate Position

                    using (ACMonitor.Lock(pwMethodProduction._62000_PWGroupLockObj))
                    {
                        // Find intermediate child position, which is assigned to this Batch
                        intermediateChildPos = intermediatePosition.ProdOrderPartslistPos_ParentProdOrderPartslistPos
                            .Where(c => c.ProdOrderBatchID.HasValue
                                        && c.ProdOrderBatchID.Value == pwMethodProduction.CurrentProdOrderBatch.ProdOrderBatchID)
                            .FirstOrDefault();
                    }
                    if (intermediateChildPos == null)
                    {
                        //Error50447:intermediateChildPos is null.
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(70)", 280, "Error50447");
                        ActivateProcessAlarmWithLog(msg, false);
                        return;
                    }

                    var relations = intermediateChildPos.ProdOrderPartslistPosRelation_TargetProdOrderPartslistPos.ToArray();

                    var prodRelation = relations.FirstOrDefault(c => c.SourceProdOrderPartslistPos.MaterialID == endBatchPos.BookingMaterial.MaterialID);

                    PWBakeryGroupFermentation pwGroup = ParentPWGroup as PWBakeryGroupFermentation;

                    if (pwGroup == null)
                    {
                        //Error50448: The parent PWGroup is not PWBakeryGroupFermentation. Please switch parent PWGroup to PWBakeryGroupFermentation.
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(80)", 294, "Error50448");
                        ActivateProcessAlarmWithLog(msg, false);
                        return;
                    }

                    Facility sourceFacility = pwGroup.GetSourceFacility();

                    if (sourceFacility == null)
                    {
                        //Error50449: The virtual source facility can not be found!
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(90)", 304, "Error50449");
                        ActivateProcessAlarmWithLog(msg, false);
                        return;
                    }

                    Facility targetFacility = pwGroup.GetTargetFacility();

                    if (targetFacility == null)
                    {
                        //Error50450: The virtual target facility can not be found!
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(100)", 314, "Error50450");
                        ActivateProcessAlarmWithLog(msg, false);
                        return;
                    }

                    sourceFacility = sourceFacility.FromAppContext<Facility>(dbApp);
                    targetFacility = targetFacility.FromAppContext<Facility>(dbApp);

                    RelocateFromTargetToSourceFacility(dbApp, sourceFacility, targetFacility, scale);

                    Guid prodMaterialID = endBatchPos.BookingMaterial.MaterialID;

                    bool anyQuantWithProdMaterial = sourceFacility.FacilityCharge_Facility.Any(c => !c.NotAvailable && c.MaterialID == prodMaterialID);

                    if (prodRelation == null && anyQuantWithProdMaterial)
                    {
                        var components = currentProdOrderPartslist.ProdOrderPartslistPos_ProdOrderPartslist.Where(c => c.MaterialID.HasValue
                                                                                                                    && c.MaterialPosType == GlobalApp.MaterialPosTypes.OutwardRoot);

                        ProdOrderPartslistPos component = components.FirstOrDefault(c => c.MaterialID == endBatchPos.BookingMaterial.MaterialID);
                        if (component == null)
                        {
                            Material material = dbApp.Material.FirstOrDefault(c => c.MaterialID == prodMaterialID);
                            if (material == null)
                            {
                                // Error50451: The material with material ID {0} can not be found in the database.
                                msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(110)", 1397, "Error50416", prodMaterialID);
                                ActivateProcessAlarmWithLog(msg);
                                return;
                            }

                            component = ProdOrderPartslistPos.NewACObject(dbApp, currentProdOrderPartslist);
                            component.Material = material;
                            component.MaterialPosTypeIndex = (short)GlobalApp.MaterialPosTypes.OutwardRoot;
                            component.Sequence = 1;
                            if (components.Any())
                            {
                                component.Sequence = components.Max(x => x.Sequence) + 1;
                            }
                            component.MDUnit = material.BaseMDUnit;

                            dbApp.ProdOrderPartslistPos.AddObject(component);

                            msg = dbApp.ACSaveChanges();
                            if (msg != null)
                            {
                                ActivateProcessAlarmWithLog(msg);
                            }
                        }

                        prodRelation = AdjustBatchPosInProdOrderPartslist(dbApp, currentProdOrderPartslist, intermediateChildPos.Material, component, batch, 0, 0);

                        msg = dbApp.ACSaveChanges();
                        if (msg != null)
                        {
                            ActivateProcessAlarmWithLog(msg);
                        }
                    }

                    if (prodRelation == null)
                    {
                        CurrentACState = ACStateEnum.SMCompleted;
                        return;
                    }

                    FSTargetQuantity.ValueT = prodRelation.TargetQuantityUOM;

                    if (AutoDetectTolerance != null)
                    {
                        double tolerance = prodRelation.TargetQuantityUOM * AutoDetectTolerance.Value / 100;
                        double targetQuantity = prodRelation.TargetQuantityUOM - tolerance;

                        TryCompleteFermentationStarter(dbApp, scale, targetQuantity, sourceFacility, prodRelation);
                    }
                    else
                    {
                        bool isUserAck = false;
                        using (ACMonitor.Lock(_20015_LockValue))
                        {
                            isUserAck = _IsUserAck;
                        }

                        if (isUserAck)
                        {
                            TryCompleteFermentationStarter(dbApp, scale, prodRelation.TargetQuantityUOM, sourceFacility, prodRelation);
                        }
                    }
                }
            }
        }

        public virtual bool RelocateFromTargetToSourceFacility(DatabaseApp dbApp, Facility source, Facility target, PAEScaleBase scale)
        {
            var quants = target.FacilityCharge_Facility.Where(c => !c.NotAvailable).ToArray();
            if (quants.Any())
            {
                if (ACFacilityManager == null)
                {
                    
                    return false;
                }

                bool outwardEnabled = target.OutwardEnabled;

                if (!target.OutwardEnabled)
                    target.OutwardEnabled = true;

                double actualQuantity = scale.ActualValue.ValueT;

                PAEScaleTotalizing scaleTotal = scale as PAEScaleTotalizing;
                if (scaleTotal != null)
                {
                    actualQuantity = scaleTotal.TotalActualWeight.ValueT;
                }

                double quantsTotalQuantity = quants.Sum(c => c.AvailableQuantity);

                foreach (FacilityCharge quant in quants)
                {
                    double calcFactor = quant.AvailableQuantity / quantsTotalQuantity;
                    double calcQuantity = actualQuantity * calcFactor;

                    ACMethodBooking bookParamRelocationClone = ACFacilityManager.ACUrlACTypeSignature("!" + GlobalApp.FBT_Relocation_Facility_BulkMaterial, gip.core.datamodel.Database.GlobalDatabase) as ACMethodBooking; // Immer Globalen context um Deadlock zu vermeiden 
                    var bookingParam = bookParamRelocationClone.Clone() as ACMethodBooking;

                    bookingParam.InwardFacility = source;
                    bookingParam.OutwardFacility = target;

                    //bookingParam.InwardFacilityCharge = quant;
                    bookingParam.OutwardFacilityCharge = quant;

                    bookingParam.InwardQuantity = calcQuantity;
                    bookingParam.OutwardQuantity = calcQuantity;

                    //bookingParam.InwardMaterial = quant.Material;
                    //bookingParam.OutwardMaterial = quant.Material;

                    //bookingParam.InwardFacilityLot = quant.FacilityLot;
                    bookingParam.OutwardFacilityLot = quant.FacilityLot;

                    //bookingParam.InwardFacilityLocation = source;

                    ACMethodEventArgs resultBooking = ACFacilityManager.BookFacility(bookingParam, dbApp);
                    Msg msg;

                    if (resultBooking.ResultState == Global.ACMethodResultState.Failed || resultBooking.ResultState == Global.ACMethodResultState.Notpossible)
                    {
                        msg = new Msg(bookingParam.ValidMessage.InnerMessage, this, eMsgLevel.Error, PWClassName, "DoManualWeighingBooking(60)", 2045);
                        ActivateProcessAlarm(msg, false);
                        return false;
                    }
                    else
                    {
                        if (!bookingParam.ValidMessage.IsSucceded() || bookingParam.ValidMessage.HasWarnings())
                        {
                            //collectedMessages.AddDetailMessage(resultBooking.ValidMessage);
                            msg = new Msg(bookingParam.ValidMessage.InnerMessage, this, eMsgLevel.Error, PWClassName, "DoManualWeighingBooking(70)", 2053);
                            ActivateProcessAlarmWithLog(msg, false);
                        }
                    }

                    msg = dbApp.ACSaveChanges();
                    if (msg != null)
                    {
                        ActivateProcessAlarmWithLog(msg);
                    }
                }

                target.OutwardEnabled = outwardEnabled;
                Msg msg1 = dbApp.ACSaveChanges();
                if (msg1 != null)
                {
                    ActivateProcessAlarmWithLog(msg1);
                }

                quants = target.FacilityCharge_Facility.Where(c => !c.NotAvailable).ToArray();
                if (quants.Any())
                {
                    if (_BookParamNotAvailableClone == null)
                        _BookParamNotAvailableClone = ACFacilityManager.ACUrlACTypeSignature("!" + GlobalApp.FBT_ZeroStock_Facility_BulkMaterial, gip.core.datamodel.Database.GlobalDatabase) as ACMethodBooking; // Immer Globalen context um Deadlock zu vermeiden 
                    ACMethodBooking clone = _BookParamNotAvailableClone.Clone() as ACMethodBooking;
                    if (target != null)
                    {
                        clone.InwardFacility = target;
                    }

                    clone.MDZeroStockState = MDZeroStockState.DefaultMDZeroStockState(dbApp, MDZeroStockState.ZeroStockStates.SetNotAvailable);

                    ACMethodEventArgs result = ACFacilityManager.BookFacility(clone, dbApp) as ACMethodEventArgs;
                    if (!clone.ValidMessage.IsSucceded() || clone.ValidMessage.HasWarnings())
                        Messages.Msg(clone.ValidMessage);
                    else if (result.ResultState == Global.ACMethodResultState.Failed || result.ResultState == Global.ACMethodResultState.Notpossible)
                    {
                        if (String.IsNullOrEmpty(result.ValidMessage.Message))
                            result.ValidMessage.Message = result.ResultState.ToString();
                        Messages.Msg(result.ValidMessage);
                    }
                }
            }
            return true;
        }

        private ProdOrderPartslistPosRelation AdjustBatchPosInProdOrderPartslist(DatabaseApp dbApp, ProdOrderPartslist poPartslist, Material intermediateMaterial, ProdOrderPartslistPos sourcePos, ProdOrderBatch batch,
                                                 double quantity, double totalWatersQuantity)
        {
            ProdOrderPartslistPos targetPos = poPartslist.ProdOrderPartslistPos_ProdOrderPartslist
                                                             .FirstOrDefault(c => c.MaterialID == intermediateMaterial.MaterialID
                                                                               && c.MaterialPosType == GlobalApp.MaterialPosTypes.InwardIntern);

            if (targetPos == null)
            {
                targetPos = ProdOrderPartslistPos.NewACObject(dbApp, null);
                targetPos.Sequence = 1;
                targetPos.MaterialPosTypeIndex = (short)GlobalApp.MaterialPosTypes.InwardIntern;
                dbApp.ProdOrderPartslistPos.AddObject(targetPos);
            }

            ProdOrderPartslistPosRelation topRelation = sourcePos.ProdOrderPartslistPosRelation_SourceProdOrderPartslistPos
                                                           .FirstOrDefault(c => c.TargetProdOrderPartslistPos.MaterialID == targetPos.MaterialID
                                                                             && c.TargetProdOrderPartslistPos.MaterialPosType == GlobalApp.MaterialPosTypes.InwardIntern);

            if (topRelation == null)
            {
                topRelation = ProdOrderPartslistPosRelation.NewACObject(dbApp, null);
                topRelation.SourceProdOrderPartslistPos = sourcePos;
                topRelation.TargetProdOrderPartslistPos = targetPos;
                topRelation.Sequence = targetPos.ProdOrderPartslistPosRelation_TargetProdOrderPartslistPos
                                                .Where(c => c.TargetProdOrderPartslistPos.MaterialID == targetPos.MaterialID
                                                         && c.TargetProdOrderPartslistPos.MaterialPosType == GlobalApp.MaterialPosTypes.InwardIntern)
                                                .Max(x => x.Sequence) + 1;

                dbApp.ProdOrderPartslistPosRelation.AddObject(topRelation);
            }

            ProdOrderPartslistPosRelation batchRelation = batch.ProdOrderPartslistPosRelation_ProdOrderBatch
                                                               .FirstOrDefault(c => c.ParentProdOrderPartslistPosRelationID == topRelation.ProdOrderPartslistPosRelationID);

            ProdOrderPartslistPos batchPos = batch.ProdOrderPartslistPos_ProdOrderBatch.FirstOrDefault(c => c.MaterialID == intermediateMaterial.MaterialID
                                                                                     && c.MaterialPosType == GlobalApp.MaterialPosTypes.InwardPartIntern);

            if (batchPos == null)
            {
                batchPos = ProdOrderPartslistPos.NewACObject(dbApp, targetPos);
                batchPos.Sequence = 1;
                var existingBatchPos = batch.ProdOrderPartslistPos_ProdOrderBatch.Where(c => c.MaterialID == intermediateMaterial.MaterialID
                                                                                     && c.MaterialPosType == GlobalApp.MaterialPosTypes.InwardPartIntern);

                if (existingBatchPos != null && existingBatchPos.Any())
                    batchPos.Sequence = existingBatchPos.Max(c => c.Sequence) + 1;

                batchPos.TargetQuantityUOM = totalWatersQuantity;
                batchPos.ProdOrderBatch = batch;
                batchPos.MDUnit = targetPos.MDUnit;
                targetPos.ProdOrderPartslistPos_ParentProdOrderPartslistPos.Add(batchPos);
            }

            targetPos.CalledUpQuantityUOM += quantity;

            if (batchRelation == null)
            {
                batchRelation = ProdOrderPartslistPosRelation.NewACObject(dbApp, topRelation);
                batchRelation.Sequence = topRelation.Sequence;
                batchRelation.TargetProdOrderPartslistPos = batchPos;
                batchRelation.SourceProdOrderPartslistPos = topRelation.SourceProdOrderPartslistPos;
                batchRelation.ProdOrderBatch = batch;
            }

            batchRelation.TargetQuantityUOM = quantity;

            return batchRelation;
        }

        public virtual bool BookFermentationStarter(DatabaseApp dbApp, ProdOrderPartslistPosRelation prodRelation, FacilityCharge facilityCharge, Facility facility)
        {
            try
            {
                MsgWithDetails collectedMessages = new MsgWithDetails();
                Msg msg = null;

                FacilityPreBooking facilityPreBooking = ProdOrderManager.NewOutwardFacilityPreBooking(this.ACFacilityManager, dbApp, prodRelation);
                ACMethodBooking bookingParam = facilityPreBooking.ACMethodBooking as ACMethodBooking;
                bookingParam.OutwardQuantity = (double)facilityCharge.AvailableQuantity;
                bookingParam.OutwardFacility = facility;
                bookingParam.OutwardFacilityCharge = facilityCharge;
                //bookingParam.SetCompleted = true;
                if (ParentPWGroup != null && ParentPWGroup.AccessedProcessModule != null)
                    bookingParam.PropertyACUrl = ParentPWGroup.AccessedProcessModule.GetACUrl();
                msg = dbApp.ACSaveChangesWithRetry();

                if (msg != null)
                {
                    collectedMessages.AddDetailMessage(msg);
                    ActivateProcessAlarmWithLog(new Msg(msg.Message, this, eMsgLevel.Error, PWClassName, "DoManualWeighingBooking(40)", 2020), false);
                    return false;
                }
                else if (facilityPreBooking != null)
                {
                    bookingParam.IgnoreIsEnabled = true;
                    ACMethodEventArgs resultBooking = ACFacilityManager.BookFacilityWithRetry(ref bookingParam, dbApp);
                    if (resultBooking.ResultState == Global.ACMethodResultState.Failed || resultBooking.ResultState == Global.ACMethodResultState.Notpossible)
                    {
                        msg = new Msg(bookingParam.ValidMessage.InnerMessage, this, eMsgLevel.Error, PWClassName, "DoManualWeighingBooking(60)", 2045);
                        ActivateProcessAlarm(msg, false);
                        return false;
                    }
                    else
                    {
                        if (!bookingParam.ValidMessage.IsSucceded() || bookingParam.ValidMessage.HasWarnings())
                        {
                            collectedMessages.AddDetailMessage(resultBooking.ValidMessage);
                            msg = new Msg(bookingParam.ValidMessage.InnerMessage, this, eMsgLevel.Error, PWClassName, "DoManualWeighingBooking(70)", 2053);
                            ActivateProcessAlarmWithLog(msg, false);
                            return false;
                        }
                        if (bookingParam.ValidMessage.IsSucceded())
                        {
                            facilityPreBooking.DeleteACObject(dbApp, true);
                            prodRelation.IncreaseActualQuantityUOM(bookingParam.OutwardQuantity.Value);
                            msg = dbApp.ACSaveChangesWithRetry();
                            if (msg != null)
                            {
                                collectedMessages.AddDetailMessage(msg);
                                msg = new Msg(msg.Message, this, eMsgLevel.Error, PWClassName, "DoManualWeighingBooking(80)", 2065);
                                ActivateProcessAlarmWithLog(msg, false);
                            }

                            msg = dbApp.ACSaveChangesWithRetry();
                            if (msg != null)
                            {
                                collectedMessages.AddDetailMessage(msg);
                                msg = new Msg(msg.Message, this, eMsgLevel.Error, PWClassName, "DoManualWeighingBooking(90)", 2094);
                                ActivateProcessAlarmWithLog(msg, false);
                            }
                            else
                            {
                                prodRelation.RecalcActualQuantityFast();
                                if (dbApp.IsChanged)
                                    dbApp.ACSaveChanges();
                            }
                        }
                        else
                            collectedMessages.AddDetailMessage(resultBooking.ValidMessage);
                    }
                }
            }
            catch (Exception e)
            {
                //TODO:error
                Messages.LogException(GetACUrl(), "BookFermentationStarter()", e);
                return false;
            }
            return true;
        }

        public Msg TryCompleteFermentationStarter(DatabaseApp dbApp, PAEScaleBase scale, double targetQuantity, Facility sourceFacility,
                                                   ProdOrderPartslistPosRelation prodRelation)
        {
            Msg msg = null;

            double scaleWeight = scale.ActualValue.ValueT;
            PAEScaleTotalizing totalScale = scale as PAEScaleTotalizing;
            if (totalScale != null)
            {
                scaleWeight = totalScale.TotalActualWeight.ValueT;
            }

            if (scaleWeight >= targetQuantity) //TODO: scale weight on start depend on scenario
            {

                var availableQuants = sourceFacility.FacilityCharge_Facility.Where(c => c.MaterialID == prodRelation.SourceProdOrderPartslistPos.MaterialID && !c.NotAvailable);
                if (availableQuants.Any())
                {
                    MDProdOrderPartslistPosState posState = DatabaseApp.s_cQry_GetMDProdOrderPosState(dbApp, MDProdOrderPartslistPosState.ProdOrderPartslistPosStates.Completed).FirstOrDefault();

                    if (posState == null)
                    {
                        SubscribeToProjectWorkCycle();
                        //TODO
                        // Error50265: MDProdOrderPartslistPosState for Completed-State doesn't exist
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "DoManualWeighingBooking(1)", 1702, "Error50265");
                        ActivateProcessAlarmWithLog(msg, false);
                        return msg;
                    }

                    bool errorInBooking = false;

                    foreach (FacilityCharge quant in availableQuants)
                    {
                        bool result = BookFermentationStarter(dbApp, prodRelation, quant, sourceFacility);
                        if (!result && !errorInBooking)
                        {
                            errorInBooking = true;
                        }
                    }

                    if (!errorInBooking)
                    {
                        prodRelation.MDProdOrderPartslistPosState = posState;
                        msg = dbApp.ACSaveChanges();
                        if (msg == null)
                        {
                            CurrentACState = ACStateEnum.SMCompleted;
                        }
                    }
                }
                else
                {
                    //wait for available quant in source facility
                    SubscribeToProjectWorkCycle();
                    return msg;
                }
            }
            else
            {
                SubscribeToProjectWorkCycle();
                return msg;
            }

            return msg;
        }

        [ACMethodInfo("", "", 700)]
        public void AckFermentationStarter(bool force)
        {
            using (ACMonitor.Lock(_20015_LockValue))
            {
                _IsUserAck = true;
            }
        }

        private static bool HandleExecuteACMethod_PWBakeryFermentationStarter(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, object[] acParameter)
        {
            return HandleExecuteACMethod_PWNodeProcessMethod(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

        protected override void DumpPropertyList(XmlDocument doc, XmlElement xmlACPropertyList)
        {
            base.DumpPropertyList(doc, xmlACPropertyList);

            XmlElement xmlChild = xmlACPropertyList["AutoDetectTolerance"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("AutoDetectTolerance");
                if (xmlChild != null)
                    xmlChild.InnerText = AutoDetectTolerance?.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["IsUserAck"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("IsUserAck");
                if (xmlChild != null)
                    xmlChild.InnerText = _IsUserAck.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }
        }

        #endregion
    }
}
