using gip.core.communication;
using gip.core.communication.ISOonTCP;
using gip.core.datamodel;
using gip2006.variobatch.processapplication;
using System;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Serializer for Pumping over'}de{'Serialisierer für Umpumpen'}", Global.ACKinds.TACDAClass, Global.ACStorableTypes.Required, false, false)]
    public class BakeryFuncSerial2006Pumping : ACSessionObjSerializer
    {
        public BakeryFuncSerial2006Pumping(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool IsSerializerFor(string typeOrACMethodName)
        {
            return MethodNameEquals(typeOrACMethodName, "BakeryPumping");
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

                iOffset += gip.core.communication.ISOonTCP.Types.Real.Length; // TargetQuantity
                iOffset += 30;
                iOffset += gip.core.communication.ISOonTCP.Types.Int.Length; //Power
                iOffset += 4;
                iOffset += gip.core.communication.ISOonTCP.Types.Int.Length; // Source
                iOffset += 18;
                iOffset += gip.core.communication.ISOonTCP.Types.Int.Length; // Destination

                OnReadObjectGetLength(response, dbNo, offset, miscParams, readParameter, ref iOffset);

                byte[] readPackage1 = new byte[iOffset];

                ErrorCode errCode = s7Session.PLCConn.ReadBytes(DataType.DataBlock, dbNo, offset, iOffset, out readPackage1);
                if (errCode != ErrorCode.NoError)
                    return null;

                iOffset = 0;
                if (s7Session.HashCodeValidation == HashCodeValidationEnum.Head || s7Session.HashCodeValidation == HashCodeValidationEnum.Head_WithRead)
                    iOffset += gip.core.communication.ISOonTCP.Types.DInt.Length;

                response.ParameterValueList.GetACValue("TargetQuantity").Value = gip.core.communication.ISOonTCP.Types.Real.FromByteArray(readPackage1, iOffset);
                iOffset += gip.core.communication.ISOonTCP.Types.Real.Length;
                iOffset += 30;

                response.ParameterValueList.GetACValue("Power").Value = gip.core.communication.ISOonTCP.Types.Int.FromByteArray(readPackage1, iOffset);
                iOffset += gip.core.communication.ISOonTCP.Types.Int.Length;
                iOffset += 4;

                response.ParameterValueList.GetACValue("Source").Value = gip.core.communication.ISOonTCP.Types.Int.FromByteArray(readPackage1, iOffset);
                iOffset += gip.core.communication.ISOonTCP.Types.Int.Length;
                iOffset += 18;

                response.ParameterValueList.GetACValue("Destination").Value = gip.core.communication.ISOonTCP.Types.Int.FromByteArray(readPackage1, iOffset);
                iOffset += gip.core.communication.ISOonTCP.Types.Int.Length;

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

        public override bool SendObject(object complexObj, int dbNo, int offset, object miscParams)
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

            iOffset += gip.core.communication.ISOonTCP.Types.Real.Length; // TargetQuantity
            iOffset += 30;

            iOffset += gip.core.communication.ISOonTCP.Types.Int.Length; //Power
            iOffset += 4;

            iOffset += gip.core.communication.ISOonTCP.Types.Int.Length; // Source
            iOffset += 18;

            iOffset += gip.core.communication.ISOonTCP.Types.Int.Length; // Destination

            if (s7Session.HashCodeValidation != HashCodeValidationEnum.Off)
                iOffset += gip.core.communication.ISOonTCP.Types.DInt.Length;

            byte[] sendPackage1 = new byte[iOffset];
            iOffset = 0;
            if (s7Session.HashCodeValidation == HashCodeValidationEnum.Head || s7Session.HashCodeValidation == HashCodeValidationEnum.Head_WithRead)
                iOffset += gip.core.communication.ISOonTCP.Types.DInt.Length;

            Array.Copy(gip.core.communication.ISOonTCP.Types.Real.ToByteArray(request.ParameterValueList.GetDouble("TargetQuantity")),
                0, sendPackage1, iOffset, gip.core.communication.ISOonTCP.Types.Real.Length);
            iOffset += gip.core.communication.ISOonTCP.Types.Real.Length;
            iOffset += 30;

            Array.Copy(gip.core.communication.ISOonTCP.Types.Int.ToByteArray(request.ParameterValueList.GetInt16("Power")),
                0, sendPackage1, iOffset, gip.core.communication.ISOonTCP.Types.Int.Length);
            iOffset += gip.core.communication.ISOonTCP.Types.Int.Length;
            iOffset += 4;

            Array.Copy(gip.core.communication.ISOonTCP.Types.Int.ToByteArray(request.ParameterValueList.GetInt16("Source")),
                0, sendPackage1, iOffset, gip.core.communication.ISOonTCP.Types.Int.Length);
            iOffset += gip.core.communication.ISOonTCP.Types.Int.Length;
            iOffset += 18;

            Array.Copy(gip.core.communication.ISOonTCP.Types.Int.ToByteArray(request.ParameterValueList.GetInt16("Destination")),
                0, sendPackage1, iOffset, gip.core.communication.ISOonTCP.Types.Int.Length);
            iOffset += gip.core.communication.ISOonTCP.Types.Int.Length;

            return this.SendObjectToPLC(s7Session, request, sendPackage1, dbNo, offset, iOffset);
        }
    }
}
