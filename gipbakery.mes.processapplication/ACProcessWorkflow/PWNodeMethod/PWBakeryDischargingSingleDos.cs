using gip.core.datamodel;
using gip.mes.processapplication;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    /// <summary>
    /// Represents the discharging node for water single dosing, provides discharging over hose or to container
    /// </summary>
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'PWDischarging Single dos.water'}de{'PWDischarging Single dos.water'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryDischargingSingleDos : PWDischarging
    {
        #region c'tors

        public PWBakeryDischargingSingleDos(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public new const string PWClassName = "PWBakeryDischargingSingleDos";

        #endregion

        #region Properties

        private short? _DischargingDestination = null;

        #endregion

        #region Methods

        public override bool GetConfigForACMethod(ACMethod paramMethod, bool isForPAF, params object[] acParameter)
        {
            _DischargingDestination = null;
            bool result = base.GetConfigForACMethod(paramMethod, isForPAF, acParameter);
            if (!result)
                return result;

            if (isForPAF)
            {
                ACValue dest = paramMethod.ParameterValueList.GetACValue("Destination");
                if (dest != null)
                {
                    _DischargingDestination = dest.ParamAsInt16;
                }
            }
            return result;
        }

        public override bool AfterConfigForACMethodIsSet(ACMethod paramMethod, bool isForPAF, params object[] acParameter)
        {
            bool result = base.AfterConfigForACMethodIsSet(paramMethod, isForPAF, acParameter);
            if (!result)
                return result;

            if (isForPAF && _DischargingDestination.HasValue)
            {
                BakeryReceivingPoint recvPoint = ParentPWGroup.AccessedProcessModule as BakeryReceivingPoint;
                if (recvPoint != null && recvPoint.HoseDestination == _DischargingDestination.Value)
                {
                    ACValue dest = paramMethod.ParameterValueList.GetACValue("Destination");
                    if (dest != null)
                    {
                        dest.Value = _DischargingDestination.Value;
                    }
                }
            }

            _DischargingDestination = null;
            return true;
        }

        protected override void DumpPropertyList(XmlDocument doc, XmlElement xmlACPropertyList)
        {
            base.DumpPropertyList(doc, xmlACPropertyList);

            XmlElement xmlChild = xmlACPropertyList["DischargingDestination"];
            if (xmlChild == null)
            {
                xmlChild = doc.CreateElement("DischargingDestination");
                if (xmlChild != null)
                    xmlChild.InnerText = _DischargingDestination?.ToString();
                xmlACPropertyList.AppendChild(xmlChild);
            }
        }

        #endregion
    }
}
