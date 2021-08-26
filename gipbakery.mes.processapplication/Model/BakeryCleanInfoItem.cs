using gip.core.datamodel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioSystem, "en{'BakeryCleanInfoItem'}de{'BakeryCleanInfoItem'}", Global.ACKinds.TACSimpleClass)]
    public class BakeryCleanInfoItem : INotifyPropertyChanged
    {
        public string ACCaption
        {
            get;
            set;
        }

        public int RouteItemID
        {
            get;
            set;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
