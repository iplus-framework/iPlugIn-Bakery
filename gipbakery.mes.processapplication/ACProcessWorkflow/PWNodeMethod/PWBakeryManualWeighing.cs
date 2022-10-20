using gip.core.datamodel;
using gip.mes.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Data.Objects.DataClasses;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'PWManualWeighing Bakery'}de{'PWManualWeighing Bakery'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryManualWeighing : PWManualWeighing
    {
        #region c'tors

        public PWBakeryManualWeighing(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") :
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        #endregion

        #region Methods

        protected override gip.mes.datamodel.ProdOrderPartslistPosRelation[] OnGetAllMaterials(Database dbIPlus, gip.mes.datamodel.DatabaseApp dbApp, gip.mes.datamodel.ProdOrderPartslistPos intermediateChildPos)
        {
            ProdOrderPartslistPosRelation[] materials = base.OnGetAllMaterials(dbIPlus, dbApp, intermediateChildPos);

            PAFBakeryTempMeasuring tempFunc = ParentPWGroup?.AccessedProcessModule?.FindChildComponents<PAFBakeryTempMeasuring>().FirstOrDefault();
            if (tempFunc != null)
            {
                bool anyConfig = false;

                foreach (ProdOrderPartslistPosRelation rel in materials)
                {
                    PartslistPos partslistPos = rel.SourceProdOrderPartslistPos.BasedOnPartslistPos;

                    bool isTempMeasurementConfigured = IsTempMeasurementConfigured(partslistPos);

                    if (isTempMeasurementConfigured)
                    {
                        InsertOrModifyConfig(partslistPos.Material.MaterialConfig_Material, dbApp, partslistPos.Material, partslistPos.PartslistID, isTempMeasurementConfigured);
                        anyConfig = true;
                        continue;
                    }

                    isTempMeasurementConfigured = IsTempMeasurementConfigured(partslistPos?.Material);

                    if (isTempMeasurementConfigured && partslistPos != null)
                    {
                        InsertOrModifyConfig(partslistPos.Material.MaterialConfig_Material, dbApp, partslistPos.Material, null, isTempMeasurementConfigured);
                        anyConfig = true;
                        continue;
                    }

                    if (IsTemperatureConfigured(partslistPos))
                    {
                        InsertOrModifyConfig(partslistPos.Material.MaterialConfig_Material, dbApp, partslistPos.Material, partslistPos.PartslistID, isTempMeasurementConfigured);
                        anyConfig = true;
                        continue;
                    }

                    Material material = partslistPos?.Material;
                    if (material == null)
                    {
                        material = rel.SourceProdOrderPartslistPos?.Material;
                        isTempMeasurementConfigured = IsTempMeasurementConfigured(material);
                    }

                    if (IsTemperatureConfigured(material))
                    {
                        InsertOrModifyConfig(material.MaterialConfig_Material, dbApp, material, null, isTempMeasurementConfigured);
                        anyConfig = true;
                    }
                }

                if (anyConfig)
                {
                    Msg msg = dbApp.ACSaveChanges();
                    if (msg != null)
                    {
                        ActivateProcessAlarmWithLog(msg);
                    }

                    tempFunc.RefreshMeasureItems();
                }
            }

            return materials;
        }

        private bool IsTempMeasurementConfigured(VBEntityObject entityObject)
        {
            if (entityObject == null)
                return false;

            var prop = entityObject.ACProperties?.GetOrCreateACPropertyExtByName("CyclicMeasurement", false);
            if (prop == null)
                return false;

            TimeSpan? ts = prop.Value as TimeSpan?;
            if (!ts.HasValue)
                return false;

            if (ts.Value.TotalMinutes > 1)
                return true;

            return false;
        }

        private bool IsTemperatureConfigured(VBEntityObject entityObject)
        {
            if (entityObject == null)
                return false;

            var prop = entityObject.ACProperties?.GetOrCreateACPropertyExtByName("Temperature", false);
            if (prop == null)
                return false;

            double? temp = prop.Value as double?;
            if (temp.HasValue)
                return true;

            return false;
        }

        private void InsertOrModifyConfig(EntityCollection<MaterialConfig> materialConfigs, DatabaseApp dbApp, Material material, Guid? partslistPosID, bool isTempMeasurement)
        {
            Guid? processModuleID = ParentPWGroup?.AccessedProcessModule?.ComponentClass.ACClassID;

            if (!processModuleID.HasValue)
                return;

            MaterialConfig materialConfig = materialConfigs.FirstOrDefault(c => c.VBiACClassID == processModuleID && c.KeyACUrl == PABakeryTempService.MaterialTempertureConfigKeyACUrl);

            if (materialConfig == null)
            {
                materialConfig = MaterialConfig.NewACObject(dbApp, material);
                materialConfig.VBiACClassID = processModuleID;
                materialConfig.KeyACUrl = PABakeryTempService.MaterialTempertureConfigKeyACUrl;
                materialConfig.SetValueTypeACClass(dbApp.ContextIPlus.GetACType("double"));
                materialConfig.Comment = partslistPosID.HasValue ? partslistPosID.ToString() : null;

                materialConfigs.Add(materialConfig);
                dbApp.MaterialConfig.AddObject(materialConfig);
            }
            else if (materialConfig.Expression == TempMeasurementModeEnum.Off.ToString())
            {
                return;
            }

            if (isTempMeasurement && materialConfig.Expression != TempMeasurementModeEnum.On.ToString())
                materialConfig.Expression = TempMeasurementModeEnum.On.ToString();

            if (!partslistPosID.HasValue && !string.IsNullOrEmpty(materialConfig.Comment))
                materialConfig.Comment = null;
            else if (partslistPosID.HasValue && materialConfig.Comment != partslistPosID.ToString())
                materialConfig.Comment = partslistPosID.ToString();
        }

        #endregion
    }
}
