using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
using gip.mes.datamodel;
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
        public PWBakeryFermentationStarter(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public const string PWClassName = "PWBakeryFermentationStarter";

        [ACPropertyBindingSource]
        public IACContainerTNet<double?> FSTargetQuantity
        {
            get;
            set;
        }

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

                    var targetRelation = intermediateChildPos.ProdOrderPartslistPosRelation_TargetProdOrderPartslistPos.FirstOrDefault();
                    using (ACMonitor.Lock(_20015_LockValue))
                    {
                        FSTargetQuantity.ValueT = targetRelation.TargetQuantityUOM;
                    }
                    UnSubscribeToProjectWorkCycle();
                }
            }
        }

        [ACMethodInfo("","",700)]
        public bool AckFermentationStarter(bool force)
        {
            PAEScaleBase scale = null;
            using (ACMonitor.Lock(_20015_LockValue))
            {
                scale = _FermentationStarterScale;
            }

            if ((scale != null && scale.ActualValue.ValueT >= FSTargetQuantity.ValueT) || force)
            {
                CurrentACState = ACStateEnum.SMCompleted;
                using (ACMonitor.Lock(_20015_LockValue))
                {
                    FSTargetQuantity.ValueT = null;
                }
                return true;
            }
           return false;
        }
    }
}
