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

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Fermentation starter'}de{'Anstellgut'}", Global.ACKinds.TPWNodeStatic, Global.ACStorableTypes.Optional, false, "PWProcessFunction", true, "", "", 9999)]
    public class PWBakeryFermentationStarter : PWNodeProcessMethod
    {

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

        private double _ScaleActualValue;

        PAEScaleBase _FermentationStarterScale = null;

        public T ParentPWMethod<T>() where T : PWMethodVBBase
        {
            if (ParentRootWFNode == null)
                return null;
            return ParentRootWFNode as T;
        }

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
            using (ACMonitor.Lock(_20015_LockValue))
            {
                FSTargetQuantity.ValueT = null;
                _ScaleActualValue = 0;
                _IsUserAck = false;
            }
            base.SMIdle();
        }

        public void StartFermentationStarter()
        {
            PWMethodProduction pwMethodProduction = ParentPWMethod<PWMethodProduction>();
            // If dosing is not for production, then do nothing
            if (pwMethodProduction == null)
                return;

            Msg msg = null;

            PAFBakerySourDoughProducing sourDoughProducing = ParentPWGroup.AccessedProcessModule.FindChildComponents<PAFBakerySourDoughProducing>().FirstOrDefault();
            if (sourDoughProducing == null)
            {
                //TODO: add message
                string error = "The process function PAFBakerySourDoughProducing is not installed on AcessedProcessModule";
                msg = new Msg(error, this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(10)", 66);
                OnNewAlarmOccurred(ProcessAlarm, msg);
                return;
            }

            PAEScaleBase scale = sourDoughProducing.GetFermentationStarterScale();
            if (scale == null)
            {
                //TODO: add message
                string error = "The scale object for fermentation starter can not be found! Please configure scale object on the function PAFBakerySourDoughProducing.";
                msg = new Msg(error, this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(20)", 66);
                OnNewAlarmOccurred(ProcessAlarm, msg);
                return;
            }

            using (ACMonitor.Lock(_20015_LockValue))
            {
                _FermentationStarterScale = scale;
                _ScaleActualValue = scale.ActualValue.ValueT;
            }


            using (var dbIPlus = new Database())
            {
                using (var dbApp = new DatabaseApp(dbIPlus))
                {
                    ProdOrderPartslistPos endBatchPos = pwMethodProduction.CurrentProdOrderPartslistPos.FromAppContext<ProdOrderPartslistPos>(dbApp);
                    if (pwMethodProduction.CurrentProdOrderBatch == null)
                    {
                        // Error50276: No batch assigned to last intermediate material of this workflow
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartManualWeighingProd(30)", 1010, "Error50276");

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
                        // Error50277: No relation defined between Workflownode and intermediate material in Materialworkflow
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartManualWeighingProd(40)", 761, "Error50277");

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
                        // Error50278: Intermediate product line not found which is assigned to this workflownode.
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartManualWeighingProd(50)", 778, "Error50278");

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
                        //Error50279:intermediateChildPos is null.
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartManualWeighingProd(70)", 1238, "Error50279");
                        ActivateProcessAlarmWithLog(msg, false);
                        return;
                    }


                    var relations = intermediateChildPos.ProdOrderPartslistPosRelation_TargetProdOrderPartslistPos.ToArray();

                    if (relations.Count() > 2)
                    {
                        //TODO: error
                        return;
                    }

                    var prodRelation = relations.FirstOrDefault(c => c.SourceProdOrderPartslistPos.MaterialID == endBatchPos.BookingMaterial.MaterialID);

                    PWBakeryGroupFermentation pwGroup = ParentPWGroup as PWBakeryGroupFermentation;

                    if (pwGroup == null)
                    {
                        //todo error
                        return;
                    }

                    Facility sourceFacility = pwGroup.GetSourceFacility();

                    if (sourceFacility == null)
                    {
                        //todo error
                        return;
                    }

                    Facility targetFacility = pwGroup.GetTargetFacility();

                    if (targetFacility == null)
                    {
                        //todo error
                        return;
                    }

                    sourceFacility = sourceFacility.FromAppContext<Facility>(dbApp);
                    targetFacility = targetFacility.FromAppContext<Facility>(dbApp);

                    RelocateFromTargetToSourceFacility(dbApp, sourceFacility, targetFacility);

                    if (prodRelation == null)
                    {
                        //todo
                        // if on source facility exists quant with material which is same like production material, error because prodRelation is null

                        CurrentACState = ACStateEnum.SMCompleted;
                        return;
                    }


                    if (AutoDetectTolerance != null)
                    {
                        double tolerance = prodRelation.TargetQuantityUOM * AutoDetectTolerance.Value / 100;
                        double targetQuantity = prodRelation.TargetQuantityUOM - tolerance;

                        TryCompleteFermentationStarter(dbApp, scale, targetQuantity, sourceFacility, prodRelation);
                    }
                    else
                    {
                        using (ACMonitor.Lock(_20015_LockValue))
                        {
                            FSTargetQuantity.ValueT = prodRelation.TargetQuantityUOM;
                        }

                        bool isUserAck = false;
                        using(ACMonitor.Lock(_20015_LockValue))
                        {
                            isUserAck = _IsUserAck;
                        }

                        if (_IsUserAck)
                        {
                            TryCompleteFermentationStarter(dbApp, scale, prodRelation.TargetQuantityUOM, sourceFacility, prodRelation);
                        }
                    }
                }
            }
        }

        public virtual bool RelocateFromTargetToSourceFacility(DatabaseApp dbApp, Facility source, Facility target)
        {
            var quants = target.FacilityCharge_Facility.Where(c => !c.NotAvailable);
            if (quants.Any())
            {
                if (quants.Count() > 1)
                {
                    //todo error
                    return false;
                }
                else
                {
                    FacilityCharge quant = quants.FirstOrDefault();

                    if (ACFacilityManager == null)
                    {
                        //TODO:Error;
                        return false;
                    }

                    bool outwardEnabled = target.OutwardEnabled;

                    if (!target.OutwardEnabled)
                        target.OutwardEnabled = true;

                    ACMethodBooking bookParamRelocationClone = ACFacilityManager.ACUrlACTypeSignature("!" + GlobalApp.FBT_Relocation_Facility_BulkMaterial, gip.core.datamodel.Database.GlobalDatabase) as ACMethodBooking; // Immer Globalen context um Deadlock zu vermeiden 
                    var bookingParam = bookParamRelocationClone.Clone() as ACMethodBooking;

                    bookingParam.InwardFacility = source;
                    bookingParam.OutwardFacility = target;

                    

                    //bookingParam.InwardFacilityCharge = quant;
                    bookingParam.OutwardFacilityCharge = quant;

                    bookingParam.InwardQuantity = quant.AvailableQuantity;
                    bookingParam.OutwardQuantity = quant.AvailableQuantity;

                    //bookingParam.InwardMaterial = quant.Material;
                    //bookingParam.OutwardMaterial = quant.Material;

                    //bookingParam.InwardFacilityLot = quant.FacilityLot;
                    bookingParam.OutwardFacilityLot = quant.FacilityLot;

                    //bookingParam.InwardFacilityLocation = source;

                    ACMethodEventArgs resultBooking = ACFacilityManager.BookFacility(bookingParam, dbApp);
                    Msg msg;

                    target.OutwardEnabled = outwardEnabled;

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
                }
            }
            return true;
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
                return false;
            }


            return true;
        }

        public Msg TryCompleteFermentationStarter(DatabaseApp dbApp, PAEScaleBase scale, double targetQuantity, Facility sourceFacility, 
                                                   ProdOrderPartslistPosRelation prodRelation)
        {
            Msg msg = null;

            if (scale.ActualValue.ValueT >= targetQuantity)
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


        [ACMethodInfo("","",700)]
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

    }
}
