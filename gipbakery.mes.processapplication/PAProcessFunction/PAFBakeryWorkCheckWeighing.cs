using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Check weighing'}de{'Wiegen prüfen'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, PWBakeryWorkProofing.PWClassName, true)]
    public class PAFBakeryWorkCheckWeighing : PAFWorkTaskGeneric
    {
        #region Constructors

        static PAFBakeryWorkCheckWeighing()
        {
            ACMethod.RegisterVirtualMethod(typeof(PAFBakeryWorkCheckWeighing), ACStateConst.TMStart, CreateVirtualMethod("CheckWeighing", "en{'Check Weighing'}de{'Wiegen prüfen'}", typeof(PWBakeryWorkCheckWeighing)));
            RegisterExecuteHandler(typeof(PAFBakeryWorkCheckWeighing), HandleExecuteACMethod_PAFBakeryWorkCheckWeighing);
        }

        public PAFBakeryWorkCheckWeighing(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        #endregion

        #region Properties
        #endregion

        #region override methods

        protected override bool PWWorkTaskScanSelector(IACComponent c)
        {
            return c is PWBakeryWorkCheckWeighing;
        }

        protected override bool PWWorkTaskScanDeSelector(IACComponent c)
        {
            return c is PWBakeryWorkCheckWeighing;
        }


        #region Execute-Helper-Handlers
        public static bool HandleExecuteACMethod_PAFBakeryWorkCheckWeighing(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PAFWorkTaskScanBase(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

        public override void InitializeRouteAndConfig(Database dbIPlus)
        {
        }

        protected override CompleteResult AnalyzeACMethodResult(ACMethod acMethod, out MsgWithDetails msg, CompleteResult completeResult)
        {
            msg = null;
            return CompleteResult.Succeeded;
        }

        protected override MsgWithDetails CompleteACMethodOnSMStarting(ACMethod acMethod, ACMethod previousParams)
        {
            return null;
        }

        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        #endregion

        [ACMethodAsync("Process", "en{'Start'}de{'Start'}", (short)MISort.Start, false)]
        public override ACMethodEventArgs Start(ACMethod acMethod)
        {
            return base.Start(acMethod);
        }

        protected static new ACMethodWrapper CreateVirtualMethod(string acIdentifier, string captionTranslation, Type pwClass)
        {
            ACMethod method = new ACMethod(acIdentifier);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();
            Dictionary<string, string> resultTranslation = new Dictionary<string, string>();

            return new ACMethodWrapper(method, captionTranslation, pwClass, paramTranslation, resultTranslation);
        }

        protected override WorkTaskScanResult OnOccupyingProcessModuleOnScan(PAProcessModule parentPM, PWWorkTaskScanBase pwNode, PAProdOrderPartslistWFInfo releaseOrderInfo, BarcodeSequenceBase sequence, PAProdOrderPartslistWFInfo selectedPOLWf, Guid facilityChargeID, int scanSequence, short? sQuestionResult)
        {
            PWGroup pwGroup = parentPM.Semaphore.ConnectionList.FirstOrDefault()?.ValueT as PWGroup;
            if (pwGroup != null)
            {
                PWBakeryWorkCheckWeighing checkWeighing = pwGroup.FindChildComponents<PWBakeryWorkCheckWeighing>(c => c is PWBakeryWorkCheckWeighing).FirstOrDefault();
                if (checkWeighing != null)
                {
                    checkWeighing.ReleaseProcessModuleOnScan(this, true);
                }
            }

            return base.OnOccupyingProcessModuleOnScan(parentPM, pwNode, releaseOrderInfo, sequence, selectedPOLWf, facilityChargeID, scanSequence, sQuestionResult);
        }

        protected override WorkTaskScanResult OnReleasingProcessModuleOnScan(PWWorkTaskScanBase pwNode, PAProdOrderPartslistWFInfo releaseOrderInfo, BarcodeSequenceBase sequence, PAProdOrderPartslistWFInfo selectedPOLWf, Guid facilityChargeID, int scanSequence, short? sQuestionResult)
        {
            return base.OnReleasingProcessModuleOnScan(pwNode, releaseOrderInfo, sequence, selectedPOLWf, facilityChargeID, scanSequence, sQuestionResult);
        }

        public override WorkTaskScanResult OnScanEvent(BarcodeSequenceBase sequence, PAProdOrderPartslistWFInfo selectedPOLWf, Guid facilityChargeID, int scanSequence, short? sQuestionResult, ACMethod acMethod, bool? machineMalfuntion)
        {
            WorkTaskScanResult tempResult = base.OnScanEvent(sequence, selectedPOLWf, facilityChargeID, scanSequence, sQuestionResult, acMethod, machineMalfuntion);

            if (tempResult != null && tempResult.Result.State == BarcodeSequenceBase.ActionState.Selection)
            {
                tempResult.Result.State = BarcodeSequenceBase.ActionState.FastSelection;
            }

            return tempResult;
        }

        #endregion
    }
}
