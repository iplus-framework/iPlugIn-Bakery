using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.datamodel;
using gip.mes.facility;
using gip.mes.processapplication;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Bin is empty acknowledge'}de{'Behälter leer Quittung'}", Global.ACKinds.TPWNodeStatic, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryYeastEmptyAck : PWNodeUserAck
    {
        #region c'tors
        static PWBakeryYeastEmptyAck()
        {
            RegisterExecuteHandler(typeof(PWBakeryYeastEmptyAck), HandleExecuteACMethod_PWBakeryYeastEmptyAck);

            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("MessageText", typeof(string), "", Global.ParamOption.Required));
            paramTranslation.Add("MessageText", "en{'Question text'}de{'Abfragetext'}");

            method.ParameterValueList.Add(new ACValue("PasswordDlg", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("PasswordDlg", "en{'With password dialogue'}de{'Mit Passwort-Dialog'}");

            method.ParameterValueList.Add(new ACValue("SkipMode", typeof(ushort), 0, Global.ParamOption.Optional));
            paramTranslation.Add("SkipMode", "en{'Skipmode: 1=Always, 2=From the second run'}de{'Überspringen: 1=Ständig, 2=Ab zweitem Durchlauf'}");

            var wrapper = new ACMethodWrapper(method, "en{'Flour discharging acknowledge'}de{'Mehlaustragsquittung'}", typeof(PWBakeryYeastEmptyAck), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryYeastEmptyAck), ACStateConst.SMStarting, wrapper);
        }

        public new const string PWClassName = "PWBakeryYeastEmptyAck";

        public PWBakeryYeastEmptyAck(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }
        #endregion


        #region Methods
        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        public T ParentPWMethod<T>() where T : PWMethodVBBase
        {
            if (ParentRootWFNode == null)
                return null;
            return ParentRootWFNode as T;
        }

        protected FacilityManager ACFacilityManager
        {
            get
            {
                PWMethodVBBase pwMethodTransport = ParentPWMethod<PWMethodVBBase>();
                return pwMethodTransport != null ? pwMethodTransport.ACFacilityManager : null;
            }
        }

        public override void AckStart()
        {
            PWGroup pwParentGroup = ParentPWGroup;
            if (pwParentGroup != null && pwParentGroup.AccessedProcessModule != null && ACFacilityManager != null)
            {
                PAProcessModule tModule = null;
                using (Database db = new Database())
                using (DatabaseApp dbApp = new DatabaseApp(db))
                {
                    RoutingResult rResult = ACRoutingService.FindSuccessors(RoutingService, db, false, pwParentGroup.AccessedProcessModule,
                                                              PAMSilo.SelRuleID_Storage, RouteDirections.Forwards, null, null, null, 0,
                                                              true, true);

                    tModule = rResult?.Routes?.FirstOrDefault()?.GetRouteTarget()?.TargetACComponent as PAProcessModule;
                    if (tModule != null)
                    {
                        Facility virtDestStore = dbApp.Facility.Where(c => c.VBiFacilityACClassID == tModule.ComponentClass.ACClassID).FirstOrDefault();
                        if (virtDestStore != null 
                            && virtDestStore.MaterialID.HasValue 
                            && virtDestStore.CurrentFacilityStock != null 
                            && virtDestStore.CurrentFacilityStock.StockQuantity != double.Epsilon)
                        {
                            ACMethodBooking zeroBooking = ACFacilityManager.ACUrlACTypeSignature("!" + GlobalApp.FBT_ZeroStock_Facility_BulkMaterial, gip.core.datamodel.Database.GlobalDatabase) as ACMethodBooking;
                            zeroBooking = zeroBooking.Clone() as ACMethodBooking;
                            zeroBooking.MDZeroStockState = MDZeroStockState.DefaultMDZeroStockState(dbApp, MDZeroStockState.ZeroStockStates.SetNotAvailable);
                            zeroBooking.InwardFacility = virtDestStore;
                            if (ParentPWGroup != null && ParentPWGroup.AccessedProcessModule != null)
                                zeroBooking.PropertyACUrl = ParentPWGroup.AccessedProcessModule.GetACUrl();
                            //zeroBooking.OutwardFacility = outwardFacility;
                            zeroBooking.IgnoreIsEnabled = true;
                            ACMethodEventArgs resultBooking = ACFacilityManager.BookFacilityWithRetry(ref zeroBooking, dbApp);
                            if (resultBooking.ResultState == Global.ACMethodResultState.Failed || resultBooking.ResultState == Global.ACMethodResultState.Notpossible)
                            {
                                Msg msg = new Msg(resultBooking.ValidMessage.InnerMessage, this, eMsgLevel.Error, PWClassName, "AckStart(20)", 629);
                                ActivateProcessAlarm(msg, false);
                            }
                            else
                            {
                                virtDestStore.Material = null;
                                virtDestStore.Partslist = null;
                                dbApp.ACSaveChangesWithRetry();
                            }
                        }
                    }
                }

            }
            base.AckStart();
        }

        public static bool HandleExecuteACMethod_PWBakeryYeastEmptyAck(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            //switch (acMethodName)
            //{
            //    case MN_AckStartClient:
            //        AckStartClient(acComponent);
            //        return true;
            //    case Const.IsEnabledPrefix + MN_AckStartClient:
            //        result = IsEnabledAckStartClient(acComponent);
            //        return true;
            //}
            return HandleExecuteACMethod_PWNodeUserAck(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
        #endregion

    }
}
