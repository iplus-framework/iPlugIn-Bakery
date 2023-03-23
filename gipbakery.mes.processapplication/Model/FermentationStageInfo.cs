using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [DataContract]
    [ACSerializeableInfo]
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'FermentationStageInfo'}de{'Fermentationsstufeninfo'}", Global.ACKinds.TACClass, Global.ACStorableTypes.NotStorable, true, false)]
    public class FermentationStageInfo : EntityBase
    {
        public FermentationStageInfo(DateTime endTime, ushort state, ushort stage)
        {
            NextStart = endTime;
            State = state;
            Stage = stage;
        }

        #region Properties
        [DataMember]
        private DateTime _NextStart;

        [IgnoreDataMember]
        [ACPropertyInfo(1, "", "en{'Nächster Start'}de{'Next Start'}")]
        public DateTime NextStart
        {
            get
            {
                return _NextStart;
            }
            set
            {
                SetProperty<DateTime>(ref _NextStart, value);
            }
        }


        [DataMember]
        private ushort _State;

        [IgnoreDataMember]
        [ACPropertyInfo(2, "", "en{'State'}de{'Status'}")]
        public ushort State
        {
            get
            {
                return _State;
            }
            set
            {
                SetProperty<ushort>(ref _State, value);
            }
        }

        [DataMember]
        private ushort _Stage;

        [IgnoreDataMember]
        [ACPropertyInfo(3, "", "en{'Stage'}de{'Stufe'}")]
        public ushort Stage
        {
            get
            {
                return _Stage;
            }
            set
            {
                SetProperty<ushort>(ref _Stage, value);
            }
        }

        #endregion
    }


    [CollectionDataContract]
    [ACSerializeableInfo]
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'FermentationStageInfos'}de{'Fermentationsstufeninfo'}", Global.ACKinds.TACClass, Global.ACStorableTypes.NotStorable, true, false)]
    public class FermentationStageInfos : List<FermentationStageInfo>
    {
        public FermentationStageInfos()
            : base()
        {
        }
    }

}
