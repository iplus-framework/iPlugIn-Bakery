using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACSerializeableInfo]
    [ACClassInfo(Const.PackName_VarioSystem, "en{'Receiving point dosing info enum'}de{'Receiving point dosing info enum'}", Global.ACKinds.TACEnum, Global.ACStorableTypes.NotStorable, true, false)]
    [DataContract]
    public enum RecvPointDosingInfoEnum : short
    {
        [EnumMember(Value = "RPDI0")]
        NotPossible = 0,
        [EnumMember(Value = "RPDI10")]
        DosingIdle = 10,
        [EnumMember(Value = "RPDI20")]
        DosingActive = 20
    }
}
