using gip.bso.masterdata;
using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.datamodel;
using System.Linq;
using System.Runtime.Remoting.Contexts;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioMaterial, "en{'Bill of Materials'}de{'Stückliste'}", Global.ACKinds.TACBSO, Global.ACStorableTypes.NotStorable, true, true, Const.QueryPrefix + gip.mes.datamodel.Partslist.ClassName)]
    public class BakeryBSOPartslist : BSOPartslist
    {
        public BakeryBSOPartslist(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        [ACMethodInfo("", "en{'Recalculate specific heat capacities'}de{'Berechne spezifische Wärmekapazitäten'}", 801)]
        public void CalcSpecHeatCapacity()
        {
            using (DatabaseApp dbApp = new DatabaseApp())
            {
                string sqlCmd = @"update pMat set pMat.SpecHeatCapacity = 2000 "
                + @"from Material pMat "
                + @"inner join PartslistPos pos on pMat.MaterialID = pos.MaterialID left "
                + @" join Partslist pl on pl.MaterialID = pMat.MaterialID "
                + @" where pl.MaterialID is null and pMat.IsIntermediate = 0 and pMat.SpecHeatCapacity < 0.001 ";
                dbApp.ExecuteStoreCommand(sqlCmd);

                MsgWithDetails msgWithDetails = new MsgWithDetails();
                for (int i = 0; i < 10; i++)
                {
                    foreach (Partslist partslist in dbApp.Partslist.Where(c =>    c.Material.SpecHeatCapacity < 0.001
                                                                               && c.PartslistPos_Partslist
                                                                                    .Where(p => p.AlternativePartslistPosID == null
                                                                                                && p.MaterialPosTypeIndex == (short)(gip.mes.datamodel.GlobalApp.MaterialPosTypes.OutwardRoot)
                                                                                                && p.ParentPartslistPosID == null
                                                                                                && p.MaterialID != c.MaterialID)
                                                                                    .All(p => p.Material.SpecHeatCapacity > 0.001))
                                                                    .ToArray())
                    {
                        double sumSpecHeat = partslist.PartslistPos_Partslist.Sum(c => (c.TargetQuantityUOM / partslist.TargetQuantityUOM) * c.Material.SpecHeatCapacity);
                        if (sumSpecHeat > PWBakeryTempCalc.C_WaterSpecHeatCapacity
                            || double.IsNaN(sumSpecHeat))
                            sumSpecHeat = PWBakeryTempCalc.C_WaterSpecHeatCapacity;
                        else if (sumSpecHeat < 1000)
                            sumSpecHeat = 1000;
                        partslist.Material.SpecHeatCapacity = sumSpecHeat;
                        Msg msg = dbApp.ACSaveChanges();
                        if (msg != null)
                        {
                            msg.Message += " " + partslist.PartslistNo;
                            msgWithDetails.AddDetailMessage(msg);
                            dbApp.ACUndoChanges();
                        }
                    }
                }
                if (msgWithDetails.MsgDetails.Any()) 
                {
                    Messages.Msg(msgWithDetails);
                }
            }
        }

        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            switch (acMethodName)
            {
                case nameof(CalcSpecHeatCapacity):
                    CalcSpecHeatCapacity();
                    return true;
            }

            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }
    }
}
