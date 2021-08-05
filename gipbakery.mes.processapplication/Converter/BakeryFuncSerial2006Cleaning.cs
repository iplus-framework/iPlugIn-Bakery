using gip.core.communication;
using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    public class BakeryFuncSerial2006Cleaning : ACSessionObjSerializer
    {
        public BakeryFuncSerial2006Cleaning(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool IsSerializerFor(string typeOrACMethodName)
        {
            return MethodNameEquals(typeOrACMethodName, "BakeryCleaning");
        }

        public override object ReadObject(object complexObj, int dbNo, int offset, object miscParams)
        {
            throw new NotImplementedException();
        }

        public override bool SendObject(object complexObj, int dbNo, int offset, object miscParams)
        {
            throw new NotImplementedException();
        }
    }
}
