using gip.core.communication;
using gip.core.communication.ISOonTCP;
using gip.core.datamodel;
using gip2006.variobatch.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Serializer for water temp check'}de{'Serialisierer für Wassertemp-prüfung'}", Global.ACKinds.TACDAClass, Global.ACStorableTypes.Required, false, false)]
    public class BakeryFuncSerial2006WaterTempCheck : ACSessionObjSerializer
    {
        public BakeryFuncSerial2006WaterTempCheck(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool IsSerializerFor(string typeOrACMethodName)
        {
            return MethodNameEquals(typeOrACMethodName, "WaterTempCheck");
        }

        public override object ReadObject(object complexObj, int dbNo, int offset, object miscParams)
        {
            S7TCPSession s7Session = ParentACComponent as S7TCPSession;
            if (s7Session == null || complexObj == null)
                return null;
            if (!s7Session.PLCConn.IsConnected)
                return null;
            ACMethod response = complexObj as ACMethod;
            if (response == null)
                return null;

            ACChildInstanceInfo childInfo = null;
            IACComponent invokerModule = null;
            bool readParameter = false;
            if (miscParams != null && miscParams is bool)
                readParameter = (bool)miscParams;
            else if (miscParams != null && miscParams is object[])
            {
                object[] paramArr = miscParams as object[];
                childInfo = paramArr[0] as ACChildInstanceInfo;
                if (childInfo != null)
                {
                    invokerModule = ACUrlCommand(childInfo.ACUrlParent) as IACComponent;
                }
                readParameter = (bool)paramArr[1];
            }

            if (readParameter)
            {
                int iOffset = 0;
                if (s7Session.HashCodeValidation == HashCodeValidationEnum.Head || s7Session.HashCodeValidation == HashCodeValidationEnum.Head_WithRead)
                    iOffset += gip.core.communication.ISOonTCP.Types.DInt.Length;
                iOffset += 36;
                iOffset += gip.core.communication.ISOonTCP.Types.Real.Length; // WaterTemp
                OnReadObjectGetLength(response, dbNo, offset, miscParams, readParameter, ref iOffset);

                byte[] readPackage1 = new byte[iOffset];

                PLC.Result errCode = s7Session.PLCConn.ReadBytes(DataTypeEnum.DataBlock, dbNo, offset, iOffset, out readPackage1);
                if (errCode != null && !errCode.IsSucceeded)
                    return null;

                iOffset = 0;
                if (s7Session.HashCodeValidation == HashCodeValidationEnum.Head || s7Session.HashCodeValidation == HashCodeValidationEnum.Head_WithRead)
                    iOffset += gip.core.communication.ISOonTCP.Types.DInt.Length;
                iOffset += 36;
                response.ParameterValueList.GetACValue("WaterTemp").Value = gip.core.communication.ISOonTCP.Types.Real.FromByteArray(readPackage1, iOffset);
                iOffset += gip.core.communication.ISOonTCP.Types.Real.Length;

                OnReadObjectAppend(response, dbNo, iOffset, miscParams, readPackage1, readParameter, ref iOffset);
            }

            return response;
        }

        protected virtual void OnReadObjectGetLength(ACMethod acMethod, int dbNo, int offset, object miscParams, bool readParameter, ref int iOffset, short telegramNo = 1)
        {

        }

        protected virtual void OnReadObjectAppend(ACMethod acMethod, int dbNo, int offset, object miscParams, byte[] readPackage, bool readParameter, ref int iOffset, short telegramNo = 1)
        {

        }

        public override bool SendObject(object complexObj, object prevComplexObj, int dbNo, int offset, int? routeOffset, object miscParams)
        {
            S7TCPSession s7Session = ParentACComponent as S7TCPSession;
            if (s7Session == null || complexObj == null)
                return false;
            if (!s7Session.PLCConn.IsConnected)
                return false;
            ACMethod request = complexObj as ACMethod;
            if (request == null)
                return false;

            int iOffset = 0;
            iOffset += 36;
            iOffset += gip.core.communication.ISOonTCP.Types.Real.Length; // WaterTemp

            if (s7Session.HashCodeValidation != HashCodeValidationEnum.Off)
                iOffset += gip.core.communication.ISOonTCP.Types.DInt.Length;

            byte[] sendPackage1 = new byte[iOffset];
            iOffset = 0;
            if (s7Session.HashCodeValidation == HashCodeValidationEnum.Head || s7Session.HashCodeValidation == HashCodeValidationEnum.Head_WithRead)
                iOffset += gip.core.communication.ISOonTCP.Types.DInt.Length;
            iOffset += 36;
            Array.Copy(gip.core.communication.ISOonTCP.Types.Real.ToByteArray(request.ParameterValueList.GetDouble("WaterTemp")),
                0, sendPackage1, iOffset, gip.core.communication.ISOonTCP.Types.Real.Length);
            iOffset += gip.core.communication.ISOonTCP.Types.Real.Length;
            return this.SendObjectToPLC(s7Session, request, sendPackage1, dbNo, offset, iOffset);
        }

    }
}
