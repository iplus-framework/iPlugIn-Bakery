using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'PWBakeryMixingPreProd'}de{'PWBakeryMixingPreProd'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryMixingPreProd : PWMixing
    {
        #region c'tors

        public new const string PWClassName = "PWBakeryPreProdMixing";

        static PWBakeryMixingPreProd()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();
            method.ParameterValueList.Add(new ACValue("SkipIfCountComp", typeof(int), 0, Global.ParamOption.Required));
            paramTranslation.Add("SkipIfCountComp", "en{'Skip if count components lower than'}de{'Überspringe wenn Komponentenanzahl kleiner als'}");

            method.ParameterValueList.Add(new ACValue("DosingGroupNo", typeof(int), 0, Global.ParamOption.Optional));
            paramTranslation.Add("DosingGroupNo", "en{'Dosing group No for wait when another dosing active with same group No'}de{'Dosiergruppennummer für Warten, wenn eine andere aktive Dosierung mit derselben Gruppennummer'}");

            var wrapper = new ACMethodWrapper(method, "en{'Configuration'}de{'Konfiguration'}", typeof(PWMixing), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWMixing), ACStateConst.SMStarting, wrapper);
            RegisterExecuteHandler(typeof(PWMixing), HandleExecuteACMethod_PWMixing);
        }

        public PWBakeryMixingPreProd(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        #endregion

        #region Properties

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

        [ACPropertyBindingSource]
        public IACContainerTNet<short?> StartingOrder
        {
            get;
            set;
        }

        #endregion

        #region Methods

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            bool canStart = VerifyCanStart();
            if (!canStart)
            {
                SubscribeToProjectWorkCycle();
                return;
            }

            base.SMStarting();
        }

        public override void SMCompleted()
        {
            StartingOrder.ValueT = null;
            base.SMCompleted();
        }

        private bool VerifyCanStart()
        {
            var possibleMixings = ApplicationManager.FindChildComponents<PWBakeryMixingPreProd>().Where(x => x.ParentPWGroup != ParentPWGroup && x.DosingGroupNo == DosingGroupNo);
            var dosings = possibleMixings.SelectMany(x => x.GetNextDosings());
            bool anyRun = dosings.Any(c => c.CurrentACState == ACStateEnum.SMRunning || c.CurrentACState == ACStateEnum.SMStarting) 
                       || possibleMixings.Any(c => c.CurrentACState == ACStateEnum.SMRunning);


            if (!anyRun)
            {
                var cleaningNodes = ApplicationManager.FindChildComponents<PWBakeryCleaning>();
                if (cleaningNodes != null && cleaningNodes.Any())
                {
                    anyRun = cleaningNodes.Any(c => c.CurrentACState == ACStateEnum.SMRunning || c.CurrentACState == ACStateEnum.SMStarting);
                }
            }

            if (StartingOrder.ValueT.HasValue && !anyRun)
            {
                short? nextRun = possibleMixings.Where(x => x.StartingOrder.ValueT.HasValue).Min(c => c.StartingOrder.ValueT);
                if (nextRun == null || nextRun >= StartingOrder.ValueT)
                    return true;

                return false;
            }
            else
            {
                if (anyRun)
                {
                    if (!StartingOrder.ValueT.HasValue)
                    {
                        var waitingMixings = possibleMixings.Where(c => c.StartingOrder.ValueT != null).Select(x => x.StartingOrder.ValueT);
                        if (waitingMixings != null && waitingMixings.Any())
                        {
                            short max = waitingMixings.Max(c => c.Value);
                            StartingOrder.ValueT = (short)(max + 1);
                        }
                        else
                        {
                            StartingOrder.ValueT = 1;
                        }
                    }
                    return false;
                }
            }

            return true;
        }

        public IEnumerable<PWBakeryDosing> GetNextDosings()
        {
            return FindSuccessors<PWBakeryDosing>(true, c => c is PWBakeryDosing, null, 1);
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

        #endregion
    }
}
