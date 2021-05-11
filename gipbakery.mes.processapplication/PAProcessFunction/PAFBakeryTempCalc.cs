using gip.core.autocomponent;
using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Bakery temperature calculation'}de{'Bäckerei-Temperaturberechnung'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, true, PWInfoACClass = PWBakeryTempCalc.PWClassName, BSOConfig = "BakeryBSOTemperature", SortIndex = 200)]
    public class PAFBakeryTempCalc : PAProcessFunction
    {
        static PAFBakeryTempCalc()
        {
            ACMethod.RegisterVirtualMethod(typeof(PAFBakeryTempCalc), ACStateConst.TMStart, CreateVirtualMethod("Temperature calculation", "en{'Temperature calculation'}de{'Berechnung der Temperatur'}", typeof(PWBakeryTempCalc)));
            RegisterExecuteHandler(typeof(PAFBakeryTempCalc), HandleExecuteACMethod_PAFBakeryTempCalc);
        }

        private static bool HandleExecuteACMethod_PAFBakeryTempCalc(out object result, IACComponent acComponent, string acMethodName, ACClassMethod acClassMethod, object[] acParameter)
        {
            return PAProcessFunction.HandleExecuteACMethod_PAProcessFunction(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

        public PAFBakeryTempCalc(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        [ACMethodAsync("Process", "en{'Start'}de{'Start'}", (short)MISort.Start, false)]
        public override ACMethodEventArgs Start(ACMethod acMethod)
        {
            return base.Start(acMethod);
        }

        public override void SMStarting()
        {
            base.SMStarting();
        }

        public override void SMRunning()
        {
            base.SMRunning();
        }

        public override void SMCompleted()
        {
            base.SMCompleted();
        }


        #region ACMethod

        protected static ACMethodWrapper CreateVirtualMethod(string acIdentifier, string captionTranslation, Type pwClass)
        {
            ACMethod method = new ACMethod(acIdentifier);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();
            Dictionary<string, string> resultTranslation = new Dictionary<string, string>();

            return new ACMethodWrapper(method, captionTranslation, pwClass, paramTranslation, resultTranslation);
        }

        #endregion

    }
}
