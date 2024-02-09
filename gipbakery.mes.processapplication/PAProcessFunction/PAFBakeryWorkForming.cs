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
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Forming and loading'}de{'Formen und Bestücken'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, PWBakeryWorkForming.PWClassName, true)]
    public class PAFBakeryWorkForming : PAFWorkTaskGeneric
    {
        #region Constructors

        static PAFBakeryWorkForming()
        {
            ACMethod.RegisterVirtualMethod(typeof(PAFBakeryWorkForming), ACStateConst.TMStart, CreateVirtualMethod("Forming", "en{'Forming'}de{'Formen'}", typeof(PWBakeryWorkForming)));
            RegisterExecuteHandler(typeof(PAFBakeryWorkForming), HandleExecuteACMethod_PAFBakeryWorkForming);
        }

        public PAFBakeryWorkForming(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        #endregion

        #region Properties
        #endregion

        #region override methods

        protected override bool PWWorkTaskScanSelector(IACComponent c)
        {
            return c is PWBakeryWorkForming;
        }

        protected override bool PWWorkTaskScanDeSelector(IACComponent c)
        {
            return c is PWBakeryWorkForming;
        }

        #region Execute-Helper-Handlers
        public static bool HandleExecuteACMethod_PAFBakeryWorkForming(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
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

            method.ParameterValueList.Add(new ACValue("Weight", typeof(double), (double)0.0, Global.ParamOption.Required));
            paramTranslation.Add("Weight", "en{'Piece weight [kg]'}de{'Stückgewicht [kg]'}");
            method.ParameterValueList.Add(new ACValue("WeightTolPlus", typeof(Double), (Double)0.0, Global.ParamOption.Optional));
            paramTranslation.Add("WeightTolPlus", "en{'Tolerance + [+=kg/-=%]'}de{'Toleranz + [+=kg/-=%]'}");
            method.ParameterValueList.Add(new ACValue("WeightTolMinus", typeof(Double), (Double)0.0, Global.ParamOption.Optional));
            paramTranslation.Add("WeightTolMinus", "en{'Tolerance - [+=kg/-=%]'}de{'Toleranz - [+=kg/-=%]'}");
            method.ParameterValueList.Add(new ACValue("Throughput", typeof(int), 0, Global.ParamOption.Required));
            paramTranslation.Add("Throughput", "en{'Throughput Pcs/min'}de{'Durchsatz Stück/min'}");
            method.ParameterValueList.Add(new ACValue("ThroughputTolPlus", typeof(Double), (Double)0.0, Global.ParamOption.Optional));
            paramTranslation.Add("ThroughputTolPlus", "en{'Tolerance + [+=Pcs/-=%]'}de{'Toleranz + [+=Stück/-=%]'}");
            method.ParameterValueList.Add(new ACValue("ThroughputTolMinus", typeof(Double), (Double)0.0, Global.ParamOption.Optional));
            paramTranslation.Add("ThroughputTolMinus", "en{'Tolerance - [+=Pcs/-=%]'}de{'Toleranz - [+=Stück/-=%]'}");

            return new ACMethodWrapper(method, captionTranslation, pwClass, paramTranslation, resultTranslation);
        }


        #endregion
    }
}
