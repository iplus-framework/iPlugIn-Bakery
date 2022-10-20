using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'PWBakeryCleaning'}de{'PWBakeryCleaning'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryCleaning : PWNodeProcessMethod
    {
        #region c'tors

        static PWBakeryCleaning()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("DosingGroupNo", typeof(int), 0, Global.ParamOption.Optional));
            paramTranslation.Add("DosingGroupNo", "en{'Dosing group No for wait when another dosing active with same group No'}de{'Dosiergruppennummer für Warten, wenn eine andere aktive Dosierung mit derselben Gruppennummer'}");

            var wrapper = new ACMethodWrapper(method, "en{'Configuration'}de{'Konfiguration'}", typeof(PWBakeryCleaning), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryCleaning), ACStateConst.SMStarting, wrapper);
            RegisterExecuteHandler(typeof(PWBakeryCleaning), HandleExecuteACMethod_PWBakeryCleaning);
        }



        public PWBakeryCleaning(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public const string PWClassName = "PWBakeryCleaning";

        #endregion

        #region Properties

        public bool IsTransport
        {
            get
            {
                return ParentPWMethod<PWMethodTransportBase>() != null;
            }
        }

        public int DosingGroupNo
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("DosingGroupNo");
                    if (acValue != null)
                    {
                        return acValue.ParamAsInt32;
                    }
                }
                return 0;
            }
        }

        #endregion

        #region Methods

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
                        if (ParentPWGroup == null)
                        {
                            Messages.LogError(this.GetACUrl(), "TaskCallback()", "ParentPWGroup is null");
                            return;
                        }

                        bool success = false;

                        PAProcessFunction cleaning = ParentPWGroup.GetExecutingFunction<PAProcessFunction>(taskEntry.RequestID);
                        if (cleaning != null)
                        {
                            using (var dbIPlus = new Database())
                            using (var dbApp = new DatabaseApp(dbIPlus))
                            {
                                if (IsTransport)
                                {
                                    var pwMethod = ParentPWMethod<PWMethodRelocation>();
                                    Picking picking = null;
                                    if (pwMethod.CurrentPicking != null)
                                    {
                                        picking = pwMethod.CurrentPicking.FromAppContext<Picking>(dbApp);
                                        PickingPos pickingPos = pwMethod.CurrentPickingPos != null ? pwMethod.CurrentPickingPos.FromAppContext<PickingPos>(dbApp) : null;
                                        if (pickingPos != null)
                                        {
                                            MDDelivPosLoadState loadToTruck = DatabaseApp.s_cQry_GetMDDelivPosLoadState(dbApp, MDDelivPosLoadState.DelivPosLoadStates.LoadToTruck).FirstOrDefault();
                                            if (loadToTruck != null)
                                            {
                                                pickingPos.MDDelivPosLoadState = loadToTruck;
                                                Msg msg = dbApp.ACSaveChanges();
                                                if (msg == null)
                                                    success = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        UnSubscribeToProjectWorkCycle();
                        _LastCallbackResult = e;
                        if (success)
                            CurrentACState = ACStateEnum.SMCompleted;
                    }
                    else if (PWPointRunning != null && eM != null && eM.ResultState == Global.ACMethodResultState.InProcess && taskEntry.State == PointProcessingState.Accepted)
                    {
                        PAProcessModule module = sender.ParentACComponent as PAProcessModule;
                        if (module != null)
                        {
                            PAProcessFunction function = module.GetExecutingFunction<PAProcessFunction>(eM.ACRequestID);
                            if (function != null)
                            {
                                if (function.CurrentACState == ACStateEnum.SMRunning)
                                {
                                    ACEventArgs eventArgs = ACEventArgs.GetVirtualEventArgs("PWPointRunning", VirtualEventArgs);
                                    eventArgs.GetACValue("TimeInfo").Value = RecalcTimeInfo();
                                    PWPointRunning.Raise(eventArgs);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                _InCallback = false;
            }
        }

        protected override void DumpPropertyList(XmlDocument doc, XmlElement xmlACPropertyList)
        {
            base.DumpPropertyList(doc, xmlACPropertyList);

            XmlElement xmlChild = xmlACPropertyList["DosingGroupNo"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("DosingGroupNo");
                if (xmlChild != null)
                    xmlChild.InnerText = DosingGroupNo.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }
        }

        private static bool HandleExecuteACMethod_PWBakeryCleaning(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, object[] acParameter)
        {
            return HandleExecuteACMethod_PWNodeProcessMethod(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

        #endregion
    }
}
