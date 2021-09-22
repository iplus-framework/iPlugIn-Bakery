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
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'PWBakeryPumping'}de{'PWBakeryPumping'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryPumping : PWDischarging
    {
        #region c'tors

        public PWBakeryPumping(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public new const string PWClassName = "PWBakeryPumping";

        #endregion

        #region Properties

        private double _ScaleWeightOnStart;
        [ACPropertyInfo(true, 9999)]
        public double ScaleWeightOnStart
        {
            get => _ScaleWeightOnStart;
            set
            {
                _ScaleWeightOnStart = value;
                OnPropertyChanged("ScaleWeightOnStart");
            }
        }

        private PAEScaleBase _TargetScale;

        #endregion

        #region Methods

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        public override void SMIdle()
        {
            ScaleWeightOnStart = 0;
            base.SMIdle();
        }

        public override bool AfterConfigForACMethodIsSet(ACMethod paramMethod, bool isForPAF, params object[] acParameter)
        {
            DatabaseApp dbApp = acParameter[0] as DatabaseApp;
            PickingPos pickingPos = acParameter[1] as PickingPos;
            PAProcessModule targetModule = acParameter[2] as PAProcessModule;

            if (dbApp == null || pickingPos == null || pickingPos.FromFacility == null)
                return false;

            PAProcessModule sModule, tModule = null;

            using (Database db = new Database())
            {
                gip.core.datamodel.ACClass sourceFacilityClass = pickingPos.FromFacility.GetFacilityACClass(db);
                gip.core.datamodel.ACClass targetModuleClass = targetModule.ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(db);

                RoutingResult rResult = ACRoutingService.FindSuccessors(RoutingService, db, false, sourceFacilityClass,
                                                                        PAProcessModule.SelRuleID_ProcessModule, RouteDirections.Backwards, null, null, null, 0,
                                                                        true, true);

                sModule = rResult?.Routes.FirstOrDefault().GetRouteSource()?.SourceACComponent as PAProcessModule;

                rResult = ACRoutingService.FindSuccessors(RoutingService, db, false, targetModuleClass,
                                                          PAProcessModule.SelRuleID_ProcessModule, RouteDirections.Forwards, null, null, null, 0,
                                                          true, true);

                tModule = rResult?.Routes?.FirstOrDefault()?.GetRouteTarget()?.TargetACComponent as PAProcessModule;
            }

            if (sModule == null || tModule == null)
                return false;

            var pafPreProd = sModule.FindChildComponents<PAFBakeryYeastProducing>().FirstOrDefault();
            PAEScaleBase scaleBase = pafPreProd?.GetFermentationStarterScale();
            _TargetScale = scaleBase;

            SaveScaleWeightOnStart(scaleBase);

            paramMethod["Source"] = sModule.RouteItemIDAsNum;
            paramMethod["Destination"] = tModule.RouteItemIDAsNum;
            paramMethod["TargetQuantity"] = pickingPos.TargetQuantityUOM;
            paramMethod["ScaleACUrl"] = scaleBase?.ACUrl;

            return true;
        }

        private bool SaveScaleWeightOnStart(PAEScaleBase scaleBase)
        {
            PAEScaleTotalizing totalScale = scaleBase as PAEScaleTotalizing;
            if (totalScale != null)
            {
                ScaleWeightOnStart = totalScale.TotalActualWeight.ValueT;
            }
            else if (scaleBase != null)
            {
                ScaleWeightOnStart = scaleBase.ActualValue.ValueT;
            }

            return true;
        }

        public override void TaskCallback(IACPointNetBase sender, ACEventArgs e, IACObject wrapObject)
        {
            _InCallback = true;
            try
            {
                if (e != null)
                {
                    IACTask taskEntry = wrapObject as IACTask;
                    ACMethodEventArgs eM = e as ACMethodEventArgs;
                    _CurrentMethodEventArgs = eM;

                    if (taskEntry.State == PointProcessingState.Deleted && CurrentACState != ACStateEnum.SMIdle)
                    {
                        ACMethod acMethod = e.ParentACMethod;
                        if (acMethod == null)
                            acMethod = taskEntry.ACMethod;

                        PAProcessModule module = sender.ParentACComponent as PAProcessModule;
                        if (module != null)
                        {
                            PAFBakeryPumping function = module.GetExecutingFunction<PAFBakeryPumping>(eM.ACRequestID);
                            if (function != null)
                            {
                                PAEScaleBase scaleBase = _TargetScale;

                                if (scaleBase == null)
                                {
                                    ACValue scaleACUrl = acMethod.ParameterValueList.GetACValue("ScaleACUrl");
                                    if (scaleACUrl != null)
                                    {
                                        string acUrl = scaleACUrl.ParamAsString;
                                        scaleBase = Root.ACUrlCommand(acUrl) as PAEScaleBase;
                                    }
                                }

                                double scaleWeightAfterPumping = 0;

                                PAEScaleTotalizing totalScale = scaleBase as PAEScaleTotalizing;
                                if (totalScale != null)
                                {
                                    scaleWeightAfterPumping = totalScale.TotalActualWeight.ValueT;
                                }
                                else if (scaleBase != null)
                                {
                                    scaleWeightAfterPumping = scaleBase.ActualValue.ValueT;
                                }

                                double actualQuantity = ScaleWeightOnStart - scaleWeightAfterPumping;

                                acMethod.ResultValueList["ActualQuantity"] = actualQuantity;
                            }
                        }
                    }
                }
            }
            finally
            {
                _InCallback = false;
            }

            base.TaskCallback(sender, e, wrapObject);
        }

        public override Msg DoInwardBooking(double actualQuantity, DatabaseApp dbApp, RouteItem dischargingDest, Picking picking, PickingPos pickingPos, ACEventArgs e, bool isDischargingEnd)
        {
            Msg msg = base.DoInwardBooking(actualQuantity, dbApp, dischargingDest, picking, pickingPos, e, isDischargingEnd);

            if (msg != null && msg.MessageLevel >= eMsgLevel.Failure)
                return msg;

            MDDelivPosLoadState loadToTruck = DatabaseApp.s_cQry_GetMDDelivPosLoadState(dbApp, MDDelivPosLoadState.DelivPosLoadStates.LoadToTruck).FirstOrDefault();
            MDDelivPosState completed = DatabaseApp.s_cQry_GetMDDelivPosState(dbApp, MDDelivPosState.DelivPosStates.CompletelyAssigned).FirstOrDefault();

            if (loadToTruck != null)
            {
                pickingPos.MDDelivPosLoadState = loadToTruck;

                if (pickingPos.OutOrderPos != null)
                    pickingPos.OutOrderPos.MDDelivPosState = completed;

                if (pickingPos.InOrderPos != null)
                    pickingPos.InOrderPos.MDDelivPosState = completed;

                return dbApp.ACSaveChanges();
            }

            return msg;
        }

        #endregion
    }
}
