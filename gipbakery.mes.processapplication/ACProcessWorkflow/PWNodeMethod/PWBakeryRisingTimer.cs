﻿using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Dough rising timer'}de{'Teigruhezeit'}", Global.ACKinds.TPWNodeStatic, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryRisingTimer : PWNodeWait
    {
        new public const string PWClassName = "PWBakeryRisingTimer";

        #region Constructors

        static PWBakeryRisingTimer()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();
            method.ParameterValueList.Add(new ACValue("Duration", typeof(TimeSpan), TimeSpan.Zero, Global.ParamOption.Required));
            paramTranslation.Add("Duration", "en{'Waitingtime'}de{'Wartezeit'}");
            method.ParameterValueList.Add(new ACValue("SkipWaiting", typeof(bool), false, Global.ParamOption.Required));
            paramTranslation.Add("SkipWaiting", "en{'Skip waitingtime'}de{'Wartezeit überspringen'}");
            var wrapper = new ACMethodWrapper(method, "en{'Wait'}de{'Warten'}", typeof(PWBakeryRisingTimer), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryRisingTimer), ACStateConst.SMStarting, wrapper);

            RegisterExecuteHandler(typeof(PWBakeryRisingTimer), HandleExecuteACMethod_PWBakeryRisingTimer);
        }

        public PWBakeryRisingTimer(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            return base.ACDeInit(deleteACClassTask);
        }

        #endregion

        #region Properties
        protected bool SkipWaiting
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("SkipWaiting");
                    if (acValue != null)
                    {
                        return acValue.ParamAsBoolean;
                    }
                }
                return false;
            }
        }
        #endregion

        #region Methods

        #region Execute-Helper-Handlers
        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            //result = null;
            //switch (acMethodName)
            //{
            //}
            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        public static bool HandleExecuteACMethod_PWBakeryRisingTimer(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PWNodeWait(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
        #endregion

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            if (SkipWaiting)
                CurrentACState = ACStateEnum.SMCompleted;
            else
                base.SMStarting();
        }


        #endregion

    }
}
