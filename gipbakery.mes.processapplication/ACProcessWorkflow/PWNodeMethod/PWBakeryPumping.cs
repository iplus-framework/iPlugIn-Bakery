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
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'PWBakeryPumping'}de{'PWBakeryPumping'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryPumping : PWDischarging
    {
        public PWBakeryPumping(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public new const string PWClassName = "PWBakeryPumping";

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
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

            paramMethod["Source"] = sModule.RouteItemIDAsNum;
            paramMethod["Destination"] = tModule.RouteItemIDAsNum;
            paramMethod["TargetQuantity"] = pickingPos.TargetQuantityUOM;

            return true;

        }
        public override void TaskCallback(IACPointNetBase sender, ACEventArgs e, IACObject wrapObject)
        {
            base.TaskCallback(sender, e, wrapObject);
        }
    }
}
