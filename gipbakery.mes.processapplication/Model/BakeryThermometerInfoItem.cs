using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    public class BakeryThermometerInfoItem : IACObject
    {


        public IACObject ParentACObject => throw new NotImplementedException();

        public IACType ACType => throw new NotImplementedException();

        public IEnumerable<IACObject> ACContentList => throw new NotImplementedException();

        public string ACIdentifier => throw new NotImplementedException();

        public string ACCaption => throw new NotImplementedException();

        public bool ACUrlBinding(string acUrl, ref IACType acTypeInfo, ref object source, ref string path, ref Global.ControlModes rightControlMode)
        {
            throw new NotImplementedException();
        }

        public object ACUrlCommand(string acUrl, params object[] acParameter)
        {
            throw new NotImplementedException();
        }

        public string GetACUrl(IACObject rootACObject = null)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabledACUrlCommand(string acUrl, params object[] acParameter)
        {
            throw new NotImplementedException();
        }
    }
}
