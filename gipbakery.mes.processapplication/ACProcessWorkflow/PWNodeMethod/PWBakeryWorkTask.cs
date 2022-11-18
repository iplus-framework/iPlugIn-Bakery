using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.datamodel;
using gip.mes.facility;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Backen'}de{'Backen'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryWorkTask : PWWorkTaskGeneric
    {
        new public const string PWClassName = nameof(PWBakeryWorkTask);

        #region Constructors

        protected static new ACMethodWrapper CreateACMethodWrapper(Type thisType)
        {
            ACMethodWrapper wrapper = PWWorkTaskGeneric.CreateACMethodWrapper(thisType);
            wrapper.Method.ParameterValueList.Add(new ACValue(nameof(QuantityPerRack), typeof(double), 0, Global.ParamOption.Required));
            wrapper.ParameterTranslation.Add(nameof(QuantityPerRack), "en{'Capacity: Quantity per oven rack'}de{'Kapazität: Menge pro Stikkenwagen'}");
            wrapper.Method.ParameterValueList.Add(new ACValue(nameof(InwardAutoSplitQuant), typeof(int), 0, Global.ParamOption.Optional));
            wrapper.ParameterTranslation.Add(nameof(InwardAutoSplitQuant), "en{'Auto split quant on inward posting increment num.'}de{'Splitte quant bei Zugangsbuchung mit fortlaufender Nummer'}");
            return wrapper;
        }

        static PWBakeryWorkTask()
        {
            Type thisType = typeof(PWBakeryWorkTask);
            ACMethodWrapper wrapper = PWBakeryWorkTask.CreateACMethodWrapper(thisType);
            ACMethod.RegisterVirtualMethod(thisType, ACStateConst.SMStarting, wrapper);
            RegisterExecuteHandler(thisType, HandleExecuteACMethod_PWBakeryWorkTask);
        }

        public PWBakeryWorkTask(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            return base.ACDeInit(deleteACClassTask);
        }

        #endregion

        #region Properties
        public double QuantityPerRack
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue(nameof(QuantityPerRack));
                    if (acValue != null)
                        return (double)acValue.Value;
                }
                return 0.0;
            }
        }

        public int InwardAutoSplitQuant
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue(nameof(InwardAutoSplitQuant));
                    if (acValue != null)
                        return (int)acValue.Value;
                }
                return 0;
            }
        }
        #endregion

        #region Methods

        #region Execute-Helper-Handlers
        public static bool HandleExecuteACMethod_PWBakeryWorkTask(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PWWorkTaskGeneric(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
        #endregion

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        #endregion

    }
}
