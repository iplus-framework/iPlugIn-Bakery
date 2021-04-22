using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Bakery temperature service'}de{'Bäckerei-Temperaturservice'}", Global.ACKinds.TPAModule, IsRightmanagement = true)]
    public class PABakeryTempService : PAClassAlarmingBase
    {
        #region c'tors

        public PABakeryTempService(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACInit(Global.ACStartTypes startChildMode = Global.ACStartTypes.Automatic)
        {
            return base.ACInit(startChildMode);
        }

        public override bool ACPostInit()
        {
            bool result = base.ACPostInit();
            if (!result)
                return result;

            InitializeService();

            return result;
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            return base.ACDeInit(deleteACClassTask);
        }

        #endregion

        #region Properties

        public List<PAMSilo> SilosWithPTC
        {
            get;
            set;
        }

        #endregion

        #region Methods

        private void InitializeService()
        {
            SilosWithPTC = new List<PAMSilo>();

            var projects = Root.ACComponentChilds.Where(c => c.ComponentClass.ACProject.ACProjectTypeIndex == (short)Global.ACProjectTypes.Application
                                                          && c.ACIdentifier != "DataAccess");

            foreach (var project in projects)
            {
                SilosWithPTC.AddRange(project.FindChildComponents<PAMSilo>(c => c.ACComponentChilds.Any(x => x is PAEThermometer)));
            }

        }

        #endregion
    }

}
