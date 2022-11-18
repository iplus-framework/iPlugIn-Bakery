using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Proofing'}de{'Garen'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, PWBakeryWorkProofing.PWClassName, true)]
    public class PAFBakeryWorkProofing : PAFWorkTaskGeneric
    {
        #region Constructors

        static PAFBakeryWorkProofing()
        {
            ACMethod.RegisterVirtualMethod(typeof(PAFBakeryWorkProofing), ACStateConst.TMStart, CreateVirtualMethod("Proofing", "en{'Proofing'}de{'Garen'}", typeof(PWBakeryWorkProofing)));
            RegisterExecuteHandler(typeof(PAFBakeryWorkProofing), HandleExecuteACMethod_PAFBakeryWorkProofing);
        }

        public PAFBakeryWorkProofing(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        #endregion

        #region Properties
        #endregion

        #region override methods

        protected override bool PWWorkTaskScanSelector(IACComponent c)
        {
            return c is PWBakeryWorkProofing;
        }

        protected override bool PWWorkTaskScanDeSelector(IACComponent c)
        {
            return c is PWBakeryWorkProofing;
        }


        #region Execute-Helper-Handlers
        public static bool HandleExecuteACMethod_PAFBakeryWorkProofing(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
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

        protected override MsgWithDetails CompleteACMethodOnSMStarting(ACMethod acMethod)
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

        protected static ACMethodWrapper CreateVirtualMethod(string acIdentifier, string captionTranslation, Type pwClass)
        {
            ACMethod method = new ACMethod(acIdentifier);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();
            Dictionary<string, string> resultTranslation = new Dictionary<string, string>();

            method.ParameterValueList.Add(new ACValue("Temperature", typeof(double), (double)0.0, Global.ParamOption.Required));
            paramTranslation.Add("Temperature", "en{'Proofing temperature [°C]'}de{'Gartemperatur [°C]'}");
            method.ParameterValueList.Add(new ACValue("TempTolPlus", typeof(Double), (Double)0.0, Global.ParamOption.Optional));
            paramTranslation.Add("TempTolPlus", "en{'Tolerance + [+=°C/-=%]'}de{'Toleranz + [+=°C/-=%]'}");
            method.ParameterValueList.Add(new ACValue("TempTolMinus", typeof(Double), (Double)0.0, Global.ParamOption.Optional));
            paramTranslation.Add("TempTolMinus", "en{'Tolerance - [+=°C/-=%]'}de{'Toleranz - [+=°C/-=%]'}");
            method.ParameterValueList.Add(new ACValue("Humidity", typeof(double), (double)0.0, Global.ParamOption.Required));
            paramTranslation.Add("Humidity", "en{'Humidity [%]'}de{'Feuchtigkeit [%]'}");
            method.ParameterValueList.Add(new ACValue("HumTolPlus", typeof(Double), (Double)0.0, Global.ParamOption.Optional));
            paramTranslation.Add("HumTolPlus", "en{'Hum. Tolerance + [+=°C/-=%]'}de{'Feucht. Toleranz + [+=°C/-=%]'}");
            method.ParameterValueList.Add(new ACValue("HumTolMinus", typeof(Double), (Double)0.0, Global.ParamOption.Optional));
            paramTranslation.Add("HumTolMinus", "en{'Hum. Tolerance - [+=°C/-=%]'}de{'Feucht. Toleranz - [+=°C/-=%]'}");
            method.ParameterValueList.Add(new ACValue("Duration", typeof(TimeSpan), TimeSpan.Zero, Global.ParamOption.Required));
            paramTranslation.Add("Duration", "en{'Duration'}de{'Dauer'}");

            return new ACMethodWrapper(method, captionTranslation, pwClass, paramTranslation, resultTranslation);
        }

        #endregion
    }
}
