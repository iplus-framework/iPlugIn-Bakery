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
    public class PWBakeryFermentationStarter : PWBaseNodeProcess
    {
        #region c'tors

        static PWBakeryFermentationStarter()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("AutoDetectTolerance", typeof(short?), null, Global.ParamOption.Optional));
            paramTranslation.Add("AutoDetectTolerance", "en{'Auto detect tolerance [%]'}de{'Toleranz automatisch erkennen [%]'}");

            method.ParameterValueList.Add(new ACValue("SkipToleranceCheck", typeof(bool), false, Global.ParamOption.Optional));
            paramTranslation.Add("SkipToleranceCheck", "en{'Skip tolerance check'}de{'Toleranzprüfung überspringen'}");

            method.ParameterValueList.Add(new ACValue("CheckIsPumpingActive", typeof(bool), true, Global.ParamOption.Optional));
            paramTranslation.Add("CheckIsPumpingActive", "en{'Check is pumpover active'}de{'Prüfen, ob das Umpumpen aktiv ist'}");

            var wrapper = new ACMethodWrapper(method, "en{'Fermentation starter'}de{'Anstellgut'}", typeof(PWBakeryFermentationStarter), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryFermentationStarter), ACStateConst.SMStarting, wrapper);
            RegisterExecuteHandler(typeof(PWBakeryFermentationStarter), HandleExecuteACMethod_PWBakeryFermentationStarter);
        }

        public PWBakeryFermentationStarter(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            if (deleteACClassTask)
            {
                ResetLocalProperties();
            }
            return base.ACDeInit(deleteACClassTask);
        }

        public override void Recycle(IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
        {
            ResetLocalProperties();
            base.Recycle(content, parentACObject, parameter, acIdentifier);
        }

        private void ResetLocalProperties()
        {
            using (ACMonitor.Lock(_20015_LockValue))
            {
                _UserInteractionMode = UserInteractionEnum.None;
                _ScaleDetectMode = ScaleDetectModeEnum.Gross;
            }
            _BookParamNotAvailableClone = null;
            StoredScaleValue = 0;
            FSTargetQuantity.ValueT = null;
            CurrentProdOrderPartslistRel = null;
            _IsCheckedIsPumpOverActive = false;
        }

        public const string PWClassName = nameof(PWBakeryFermentationStarter);

        #endregion

        #region Properties

        public short? AutoDetectTolerance
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
                            return acValue.ParamAsInt16;
                    }
                }
                return null;
            }
        }

        public bool SkipToleranceCheck
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("SkipToleranceCheck");
                    if (acValue != null)
                    {
                        if (acValue.Value != null)
                            return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }

        public bool CheckIsPumpingActive
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("CheckIsPumpingActive");
                    if (acValue != null)
                    {
                        if (acValue.Value != null)
                            return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }

        [ACPropertyInfo(true, 500, "", "en{'Stored gross weight of conatiner'}de{'Gespeicheter Bruttowert des Behälters'}")]
        public double StoredScaleValue
        {
            get;
            set;
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

        public ProdOrderPartslistPosRelation CurrentProdOrderPartslistRel
        {
            get;
            private set;
        }

        private UserInteractionEnum _UserInteractionMode = UserInteractionEnum.None;
        private ScaleDetectModeEnum _ScaleDetectMode;
        private ACMethodBooking _BookParamNotAvailableClone;

        private bool _IsCheckedIsPumpOverActive = false;

        public override bool MustBeInsidePWGroup
        {
            get
            {
                return true;
            }
        }

        #endregion

        #region Methods

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            if (!Root.Initialized)
                return;

            ACMethod paramMethod = MyConfiguration;
            RecalcTimeInfo();
            CreateNewProgramLog(paramMethod);

            base.SMStarting();
        }

        public override void SMRunning()
        {
            if (!Root.Initialized)
                return;

            bool result = StartFermentationStarter();
            if (!result)
                return;

            string dump = string.Format("UserInteraction = {0}; ScaleDetectionMode = {1}", _UserInteractionMode.ToString(), _ScaleDetectMode.ToString());
            Messages.LogInfo(this.GetACUrl(), nameof(SMRunning), dump);

            base.SMRunning();
        }

        public override void SMIdle()
        {
            ResetLocalProperties();
            base.SMIdle();
        }

        public T ParentPWMethod<T>() where T : PWMethodVBBase
        {
            if (ParentRootWFNode == null)
                return null;
            return ParentRootWFNode as T;
        }

        private bool StartFermentationStarter()
        {
            PWMethodProduction pwMethodProduction = ParentPWMethod<PWMethodProduction>();
            // If dosing is not for production, then do nothing
            if (pwMethodProduction == null)
                return true;

            Msg msg = null;
            PAProcessModule processModule = ParentPWGroup.AccessedProcessModule;
            PAFBakeryYeastProducing preProdFunction = processModule?.FindChildComponents<PAFBakeryYeastProducing>(c => c is PAFBakeryYeastProducing).FirstOrDefault();

            if (preProdFunction == null)
            {
                //Error50442: The process function PAFBakerySourDoughProducing can not be found on the process module {0}. Please check your configuration.";
                msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(10)", 176, "Error50442", processModule.ACCaption);
                OnNewAlarmOccurred(ProcessAlarm, msg);
                return true;
            }

            if (CheckIsPumpingActive)
            {
                bool result = IsPumpingActive(preProdFunction, processModule);
                if (result)
                {
                    SubscribeToProjectWorkCycle();
                    return false;
                }
            }

            PAEScaleBase scale = preProdFunction.GetFermentationStarterScale();
            if (scale == null)
            {
                //Error50443: The scale object for fermentation starter can not be found! Please configure scale object on the function PAFBakerySourDoughProducing for {0}.
                msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(20)", 66, "Error50443", processModule.ACCaption);
                OnNewAlarmOccurred(ProcessAlarm, msg);
                return true;
            }

            UserInteractionEnum userAckMode = UserInteractionEnum.None;
            using (ACMonitor.Lock(_20015_LockValue))
            {
                userAckMode = _UserInteractionMode;
            }

            TryFindOrCreateProdOrderPartslistRel(scale, pwMethodProduction);

            if (userAckMode == UserInteractionEnum.UserAbort || CurrentProdOrderPartslistRel == null)
            {
                ForceCompleteFermentationStarter(CurrentProdOrderPartslistRel, true);
            }

            if (!FSTargetQuantity.ValueT.HasValue)
                return true;

            double targetQuantityRel = FSTargetQuantity.ValueT.Value;

            if (AutoDetectTolerance != null)
            {
                double tolerance = targetQuantityRel * AutoDetectTolerance.Value / 100;
                double targetQuantity = targetQuantityRel - tolerance;

                var completeMsg = TryCompleteFermentationStarter(scale, targetQuantity, CurrentProdOrderPartslistRel);
                if (completeMsg != null && userAckMode == UserInteractionEnum.UserAckWithoutToleranceCheck)
                {
                    ForceCompleteFermentationStarter(CurrentProdOrderPartslistRel);
                }
            }
            else
            {
                if (userAckMode == UserInteractionEnum.UserAck || userAckMode == UserInteractionEnum.UserAckWithoutToleranceCheck)
                {
                    var completeMsg = TryCompleteFermentationStarter(scale, targetQuantityRel, CurrentProdOrderPartslistRel);
                    if (completeMsg != null && userAckMode == UserInteractionEnum.UserAckWithoutToleranceCheck)
                    {
                        ForceCompleteFermentationStarter(CurrentProdOrderPartslistRel);
                    }
                }
            }
            return true;
        }

        private bool IsPumpingActive(PAFBakeryYeastProducing preProdFunction, PAProcessModule processModule)
        {
            if (!_IsCheckedIsPumpOverActive)
            {
                if (preProdFunction == null || processModule == null)
                {
                    Messages.LogInfo(this.GetACUrl(), nameof(IsPumpingActive) + "(10)", string.Format("preProdFunction is null: {0}  processModule is null: {1}",
                                                                                                      preProdFunction == null, processModule == null));
                    _IsCheckedIsPumpOverActive = true;
                    return false;
                }

                string pumpModuleACUrl = preProdFunction.PumpOverProcessModuleACUrl;
                if (string.IsNullOrEmpty(pumpModuleACUrl))
                {
                    Messages.LogInfo(this.GetACUrl(), nameof(IsPumpingActive) + "(20)", "pumpModuleACUrl is null or empty!");
                    _IsCheckedIsPumpOverActive = true;
                    return false;
                }

                PAProcessModule pumpModule = ACUrlCommand(pumpModuleACUrl) as PAProcessModule;
                PAFBakeryPumping pump = pumpModule?.FindChildComponents<PAFBakeryPumping>(c => c is PAFBakeryPumping).FirstOrDefault();
                if (pump != null)
                {
                    if (pump.CurrentACState != ACStateEnum.SMIdle)
                    {
                        ACValue sourceItem = pump.CurrentACMethod?.ValueT?.ParameterValueList.GetACValue("Source");
                        if (sourceItem != null && sourceItem.Value != null)
                        {
                            short sourceID = sourceItem.ParamAsInt16;
                            if (sourceID == processModule.RouteItemIDAsNum)
                            {
                                return true;
                            }
                            else
                            {
                                Messages.LogInfo(this.GetACUrl(), nameof(IsPumpingActive) + "(30)", String.Format("PAModule source item: {0}  PumpFunction source item: {1}",
                                                                                                                  processModule.RouteItemIDAsNum, sourceID));
                                _IsCheckedIsPumpOverActive = true;
                            }
                        }
                        else
                        {
                            Messages.LogInfo(this.GetACUrl(), nameof(IsPumpingActive) + "(40)", string.Format("The source item from a pumping function is null! The pump function's CurrentACMethod is null = {0}",
                                                                                                     pump.CurrentACMethod?.ValueT == null));
                        }
                    }
                    else
                    {
                        //Messages.LogInfo(this.GetACUrl(), nameof(IsPumpingActive) + "(50)", String.Format("PumpFunction ACState: {0}", pump.CurrentACState));
                        _IsCheckedIsPumpOverActive = true;
                    }
                }
                else
                {
                    Messages.LogInfo(this.GetACUrl(), nameof(IsPumpingActive) + "(60)" , String.Format("The pumping module is null = {0} and the pumping function is null = {1}", pumpModule == null, pump == null));
                }
            }
            return false;
        }

        public virtual bool RelocateFromTargetToSourceFacility(DatabaseApp dbApp, Facility source, Facility target, PAEScaleBase scale)
        {
            var quants = target.FacilityCharge_Facility.Where(c => !c.NotAvailable).ToArray();
            if (!quants.Any())
            {
                quants = target.FacilityCharge_Facility.Where(c => c.NotAvailable && c.FillingDate.HasValue).OrderByDescending(c => c.FillingDate).Take(1).ToArray();
            }

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
                double quantsTotalQuantity = quants.Sum(c => c.AvailableQuantity);

                int quantsCount = quants.Count();

                foreach (FacilityCharge quant in quants)
                {
                    double calcFactor = quant.AvailableQuantity / quantsTotalQuantity;
                    double calcQuantity = actualQuantity * calcFactor;

                    //when quant available quantity is 0 and it is not available
                    if ((calcQuantity < 0.00001 || double.IsNaN(calcQuantity)) && quantsCount == 1)
                    {
                        calcQuantity = actualQuantity;
                    }

                    ACMethodBooking bookParamRelocationClone = ACFacilityManager.ACUrlACTypeSignature("!" + GlobalApp.FBT_Relocation_Facility_BulkMaterial, gip.core.datamodel.Database.GlobalDatabase) as ACMethodBooking; // Immer Globalen context um Deadlock zu vermeiden 
                    var bookingParam = bookParamRelocationClone.Clone() as ACMethodBooking;

                    bookingParam.InwardFacility = source;
                    bookingParam.OutwardFacility = target;

                    bookingParam.OutwardFacilityCharge = quant;

                    bookingParam.InwardQuantity = calcQuantity;
                    bookingParam.OutwardQuantity = calcQuantity;

                    bookingParam.OutwardFacilityLot = quant.FacilityLot;

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
                    ActivateProcessAlarmWithLog(new Msg(msg.Message, this, eMsgLevel.Error, PWClassName, "BookFermentationStarter(10)", 620), false);
                    return false;
                }
                else if (facilityPreBooking != null)
                {
                    bookingParam.IgnoreIsEnabled = true;
                    ACMethodEventArgs resultBooking = ACFacilityManager.BookFacilityWithRetry(ref bookingParam, dbApp);
                    if (resultBooking.ResultState == Global.ACMethodResultState.Failed || resultBooking.ResultState == Global.ACMethodResultState.Notpossible)
                    {
                        msg = new Msg(bookingParam.ValidMessage.InnerMessage, this, eMsgLevel.Error, PWClassName, "BookFermentationStarter(20)", 629);
                        ActivateProcessAlarm(msg, false);
                        return false;
                    }
                    else
                    {
                        if (!bookingParam.ValidMessage.IsSucceded() || bookingParam.ValidMessage.HasWarnings())
                        {
                            collectedMessages.AddDetailMessage(resultBooking.ValidMessage);
                            msg = new Msg(bookingParam.ValidMessage.InnerMessage, this, eMsgLevel.Error, PWClassName, "BookFermentationStarter(30)", 638);
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
                                msg = new Msg(msg.Message, this, eMsgLevel.Error, PWClassName, "BookFermentationStarter(40)", 650);
                                ActivateProcessAlarmWithLog(msg, false);
                            }

                            msg = dbApp.ACSaveChangesWithRetry();
                            if (msg != null)
                            {
                                collectedMessages.AddDetailMessage(msg);
                                msg = new Msg(msg.Message, this, eMsgLevel.Error, PWClassName, "BookFermentationStarter(50)", 658);
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
                Msg msg = new Msg(this, eMsgLevel.Exception, PWClassName, "BookFermentationStarter(90)", 675, e.Message);
                OnNewAlarmOccurred(ProcessAlarm, msg);

                Messages.LogException(GetACUrl(), "BookFermentationStarter(100)", e);
                return false;
            }
            return true;
        }

        private Msg TryCompleteFermentationStarter(PAEScaleBase scale, double targetQuantity, ProdOrderPartslistPosRelation prodRelation)
        {
            Msg msg = null;

            double scaleWeight = scale.ActualValue.ValueT;

            bool isStarterQuantityInTank = scaleWeight >= targetQuantity;
            if (_ScaleDetectMode == ScaleDetectModeEnum.Difference)
            {
                double diff = scaleWeight - StoredScaleValue;
                isStarterQuantityInTank = diff >= targetQuantity;
            }
            else if (_ScaleDetectMode == ScaleDetectModeEnum.Skip)
            {
                isStarterQuantityInTank = true;
            }

            if (isStarterQuantityInTank)
            {
                if (prodRelation == null)
                    return null;
                using (DatabaseApp dbApp = new DatabaseApp())
                {
                    ProdOrderPartslistPosRelation rel = prodRelation.FromAppContext<ProdOrderPartslistPosRelation>(dbApp);

                    PWBakeryGroupFermentation pwGroup = ParentPWGroup as PWBakeryGroupFermentation;

                    if (pwGroup == null)
                    {
                        //Error50448: The parent PWGroup is not PWBakeryGroupFermentation. Please switch parent PWGroup to PWBakeryGroupFermentation.
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(80)", 710, "Error50448");
                        return msg;
                    }

                    Facility sourceFacility = pwGroup.SourceFacility;
                    sourceFacility = sourceFacility?.FromAppContext<Facility>(dbApp);
                    if (sourceFacility == null)
                    {
                        //Error50449: The virtual source facility can not be found!
                        msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(90)", 720, "Error50449");
                        return msg;
                    }

                    var availableQuants = sourceFacility.FacilityCharge_Facility
                                                        .Where(c => c.MaterialID == rel.SourceProdOrderPartslistPos.MaterialID
                                                                && !c.NotAvailable
                                                                &&  c.FacilityBookingCharge_InwardFacilityCharge.FirstOrDefault()?.FacilityBookingType != GlobalApp.FacilityBookingType.InventoryNewQuant)
                                                        .ToArray();

                    if (availableQuants.Any())
                    {
                        MDProdOrderPartslistPosState posState = DatabaseApp.s_cQry_GetMDProdOrderPosState(dbApp, MDProdOrderPartslistPosState.ProdOrderPartslistPosStates.Completed).FirstOrDefault();

                        if (posState == null)
                        {
                            SubscribeToProjectWorkCycle();
                            //Error50483: MDProdOrderPartslistPosState for Completed-State doesn't exist
                            msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(100)", 734, "Error50483");
                            ActivateProcessAlarmWithLog(msg, false);
                            return msg;
                        }

                        bool errorInBooking = false;

                        foreach (FacilityCharge quant in availableQuants)
                        {
                            var facilityBookingCharge = quant.FacilityBookingCharge_InwardFacilityCharge.FirstOrDefault();
                            if (facilityBookingCharge != null && facilityBookingCharge.FacilityBookingType == GlobalApp.FacilityBookingType.InventoryNewQuant)
                                continue;

                            bool result = BookFermentationStarter(dbApp, rel, quant, sourceFacility);
                            if (!result && !errorInBooking)
                            {
                                errorInBooking = true;
                            }
                        }

                        if (!errorInBooking)
                        {
                            rel.MDProdOrderPartslistPosState = posState;
                            msg = dbApp.ACSaveChanges();
                            if (msg == null)
                            {
                                CurrentACState = ACStateEnum.SMCompleted;
                                return null;
                            }
                        }
                    }
                    else
                    {
                        //wait for available quant in source facility
                        _ScaleDetectMode = ScaleDetectModeEnum.Difference;
                        SubscribeToProjectWorkCycle();
                        if (msg == null)
                            msg = new Msg();
                        return msg;
                    }
                }
            }
            else
            {
                SubscribeToProjectWorkCycle();
                if (msg == null)
                    msg = new Msg();
                return msg;
            }
            return msg;
        }

        private void ForceCompleteFermentationStarter(ProdOrderPartslistPosRelation prodRelation, bool isAbort = false)
        {
            if (prodRelation != null)
            {
                using (DatabaseApp dbApp = new DatabaseApp())
                {
                    ProdOrderPartslistPosRelation rel = prodRelation.FromAppContext<ProdOrderPartslistPosRelation>(dbApp);
                    if (rel != null)
                    {
                        MDProdOrderPartslistPosState.ProdOrderPartslistPosStates posStateEnum = MDProdOrderPartslistPosState.ProdOrderPartslistPosStates.Completed;
                        if (isAbort)
                            posStateEnum = MDProdOrderPartslistPosState.ProdOrderPartslistPosStates.Cancelled;

                        MDProdOrderPartslistPosState posState = DatabaseApp.s_cQry_GetMDProdOrderPosState(dbApp, posStateEnum).FirstOrDefault();
                        if (posState == null)
                        {
                            SubscribeToProjectWorkCycle();
                            //Error50483: MDProdOrderPartslistPosState for Completed-State doesn't exist
                            Msg msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(100)", 734, "Error50483");
                            ActivateProcessAlarmWithLog(msg, false);
                        }

                        if (posState != null)
                        {
                            rel.MDProdOrderPartslistPosState = posState;
                            Msg msg = dbApp.ACSaveChanges();
                            if (msg != null)
                            {
                                ActivateProcessAlarmWithLog(msg, false);
                            }
                        }
                    }
                }
            }

            Messages.LogInfo(this.GetACUrl(), nameof(ForceCompleteFermentationStarter), "Fermentation starter is forced to complete.");
            CurrentACState = ACStateEnum.SMCompleted;
        }

        private Msg TryFindOrCreateProdOrderPartslistRel(PAEScaleBase scale, PWMethodProduction pwMethodProduction)
        {
            Msg msg;
            if (CurrentProdOrderPartslistRel == null && !FSTargetQuantity.ValueT.HasValue)
            {
                StoredScaleValue = scale.ActualValue.ValueT;

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
                            return msg;
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
                            return msg;
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
                            return msg;
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
                            return msg;
                        }

                        var relations = intermediateChildPos.ProdOrderPartslistPosRelation_TargetProdOrderPartslistPos.ToArray();

                        var prodRelation = relations.FirstOrDefault(c => c.SourceProdOrderPartslistPos.MaterialID == endBatchPos.BookingMaterial.MaterialID);

                        PWBakeryGroupFermentation pwGroup = ParentPWGroup as PWBakeryGroupFermentation;

                        if (pwGroup == null)
                        {
                            //Error50448: The parent PWGroup is not PWBakeryGroupFermentation. Please switch parent PWGroup to PWBakeryGroupFermentation.
                            msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(80)", 294, "Error50448");
                            ActivateProcessAlarmWithLog(msg, false);
                            return msg;
                        }

                        Facility sourceFacility = pwGroup.SourceFacility;
                        sourceFacility = sourceFacility?.FromAppContext<Facility>(dbApp);
                        if (sourceFacility == null)
                        {
                            //Error50449: The virtual source facility can not be found!
                            msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(90)", 304, "Error50449");
                            ActivateProcessAlarmWithLog(msg, false);
                            return msg;
                        }

                        Facility targetFacility = pwGroup.TargetFacility;
                        targetFacility = targetFacility?.FromAppContext<Facility>(dbApp);
                        if (targetFacility == null)
                        {
                            //Error50450: The virtual target facility can not be found!
                            msg = new Msg(this, eMsgLevel.Error, PWClassName, "StartFermentationStarter(100)", 314, "Error50450");
                            ActivateProcessAlarmWithLog(msg, false);
                            return msg;
                        }

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
                                    return msg;
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
                                    return msg;
                                }

                            }

                            prodRelation = AdjustBatchPosInProdOrderPartslist(dbApp, currentProdOrderPartslist, intermediateChildPos.Material, component, batch, 0, 0);

                            msg = dbApp.ACSaveChanges();
                            if (msg != null)
                            {
                                ActivateProcessAlarmWithLog(msg);
                                return msg;
                            }

                            _ScaleDetectMode = ScaleDetectModeEnum.Skip;
                        }

                        //if (prodRelation == null)
                        //{
                        //    CurrentACState = ACStateEnum.SMCompleted;
                        //    return null;
                        //}

                        if (prodRelation != null)
                        {
                            CurrentProdOrderPartslistRel = prodRelation;
                            FSTargetQuantity.ValueT = prodRelation.TargetQuantityUOM;
                        }
                        SubscribeToProjectWorkCycle();
                    }
                }
            }

            return null;
        }

        [ACMethodInfo("", "en{'Acknowledge fermentation starter'}de{'Anstellgut quittieren'}", 700)]
        public void AckFermentationStarter()
        {
            UserInteractionEnum interaction = UserInteractionEnum.UserAck;
            if (SkipToleranceCheck)
                interaction = UserInteractionEnum.UserAckWithoutToleranceCheck;

            using (ACMonitor.Lock(_20015_LockValue))
            {
                _UserInteractionMode = interaction;
            }

            Messages.LogInfo(this.GetACUrl(), nameof(AckFermentationStarter), "UserInteraction = " + interaction.ToString());
        }

        [ACMethodInfo("", "en{'Abort fermentation starter'}de{'Anstellgut abbrechen'}", 701)]
        public void AbortFermentationStarter()
        {
            using (ACMonitor.Lock(_20015_LockValue))
            {
                _UserInteractionMode = UserInteractionEnum.UserAbort;
            }
            SubscribeToProjectWorkCycle();

            Messages.LogInfo(this.GetACUrl(), nameof(AbortFermentationStarter), "UserInteraction = " + UserInteractionEnum.UserAbort.ToString());
        }

        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            switch (acMethodName)
            {
                case nameof(AckFermentationStarter):
                    AckFermentationStarter();
                    return true;
                case nameof(AbortFermentationStarter):
                    AbortFermentationStarter();
                    return true;
            }

            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        private static bool HandleExecuteACMethod_PWBakeryFermentationStarter(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, object[] acParameter)
        {
            return HandleExecuteACMethod_PWBaseNodeProcess(out result, acComponent, acMethodName, acClassMethod, acParameter);
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

            xmlChild = xmlACPropertyList["UserInteractionMode"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("UserInteractionMode");
                if (xmlChild != null)
                    xmlChild.InnerText = _UserInteractionMode.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["ScaleDetectMode"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("ScaleDetectMode");
                if (xmlChild != null)
                    xmlChild.InnerText = _ScaleDetectMode.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }

            xmlChild = xmlACPropertyList["_IsCheckedIsPumpOverActive"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("_IsCheckedIsPumpOverActive");
                if (xmlChild != null)
                    xmlChild.InnerText = _IsCheckedIsPumpOverActive.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }
        }

        #endregion

        #region Enums

        private enum ScaleDetectModeEnum : short
        {
            Gross = 0, //Gross weight must be greather than autodetect quantity
            Difference = 10, // Difference weight must be greather than autodetect quantity (WeightOnEnd - WeightOnStart) > Autodetect
            Skip = 20 // Skip scale weight check, in case if is a fermentation starter combined with manual weighing
        }

        private enum UserInteractionEnum
        {
            None = 0,
            UserAck = 10,
            UserAckWithoutToleranceCheck = 20,
            UserAbort = 30
        }

        #endregion
    }
}
