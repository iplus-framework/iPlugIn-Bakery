﻿using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Receiving point'}de{'Abnahmestelle'}", Global.ACKinds.TPAProcessModule, Global.ACStorableTypes.Required, false, PWGroupVB.PWClassName, true)]
    public class BakeryReceivingPoint : PAMPlatformscale
    {
        #region c'tors
        static BakeryReceivingPoint()
        {
            RegisterExecuteHandler(typeof(BakeryReceivingPoint), HandleExecuteACMethod_BakeryReceivingPoint);
        }

        public BakeryReceivingPoint(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
            _PAPointMatIn2 = new PAPoint(this, "PAPointMatIn2");
            _PAPointMatIn3 = new PAPoint(this, "PAPointMatIn3");
            _PAPointMatIn4 = new PAPoint(this, "PAPointMatIn4");
            _PAPointMatIn5 = new PAPoint(this, "PAPointMatIn5");
        }

        public override bool ACInit(Global.ACStartTypes startChildMode = Global.ACStartTypes.Automatic)
        {
            if (!base.ACInit(startChildMode))
                return false;
            return true;
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            return base.ACDeInit(deleteACClassTask);
        }
        #endregion

        #region Properties 

        [ACPropertyBindingSource(IsPersistable = true)]
        public IACContainerTNet<double> DoughCorrTemp
        {
            get;
            set;
        }


        /// <summary>
        /// Represents the room temperature. It can be fix defined or can be bounded to the sensor.
        /// </summary>
        [ACPropertyBindingTarget(IsPersistable = true)]
        public IACContainerTNet<double> RoomTemperature
        {
            get;
            set;
        }

        private bool _SearchTempService = true;
        private ACRef<ACComponent> _TemperatureService;

        public ACComponent TemperatureService
        {
            get
            {
                //TODO: temp service from acurl config

                if (_TemperatureService != null)
                    return _TemperatureService.ValueT;

                if (_SearchTempService)
                {
                    var serviceProj = Root.ACUrlCommand("\\Service") as ACComponent;
                    if (serviceProj != null)
                    {
                        ACComponent comp = serviceProj.FindChildComponents<PABakeryTempService>(c => c is PABakeryTempService).FirstOrDefault();
                        if (comp != null)
                        {
                            _TemperatureService = new ACRef<ACComponent>(comp, this);
                            return _TemperatureService.ValueT;
                        }
                    }

                    _SearchTempService = false;
                }
                return null;
            }
        }

        #endregion

        #region Methods 

        /// <summary>
        /// Get component(material) temperatures from the temperature service.
        /// </summary>
        /// <returns>Returns the list of ACValue(ACIdentifier = MaterialNo; Value = MaterialTemperature)</returns>
        [ACMethodInfo("","",800)]
        public ACValueList GetComponentTemperatures()
        {
            if (TemperatureService == null)
                return null;

            return TemperatureService.ExecuteMethod(PABakeryTempService.MN_GetTemperaturesInfo, ComponentClass.ACClassID) as ACValueList;
        }


        #endregion

        #region Points
        PAPoint _PAPointMatIn2;
        /// <summary>
        /// City water Point
        /// </summary>
        /// <value>
        /// City water Point
        /// </value>
        [ACPropertyConnectionPoint(9999, "PointMaterial", "en{'ONLY for city water'}de{'NUR für Stadtwasser'}")]
        public PAPoint PAPointMatIn2
        {
            get
            {
                return _PAPointMatIn2;
            }
        }

        PAPoint _PAPointMatIn3;
        /// <summary>
        /// Cold water Point
        /// </summary>
        /// <value>
        /// Cold water Point
        /// </value>
        [ACPropertyConnectionPoint(9999, "PointMaterial", "en{'ONLY for Cold water'}de{'NUR für kaltes Wasser'}")]
        public PAPoint PAPointMatIn3
        {
            get
            {
                return _PAPointMatIn3;
            }
        }

        PAPoint _PAPointMatIn4;
        /// <summary>
        /// Warm water Point
        /// </summary>
        /// <value>
        /// Warm water Point
        /// </value>
        [ACPropertyConnectionPoint(9999, "PointMaterial", "en{'ONLY for warm water'}de{'NUR für Warmwasser'}")]
        public PAPoint PAPointMatIn4
        {
            get
            {
                return _PAPointMatIn4;
            }
        }

        PAPoint _PAPointMatIn5;
        /// <summary>
        /// Other liquids
        /// </summary>
        /// <value>
        /// Other liquids
        /// </value>
        [ACPropertyConnectionPoint(9999, "PointMaterial")]
        public PAPoint PAPointMatIn5
        {
            get
            {
                return _PAPointMatIn5;
            }
        }

        #endregion

        #region Execute-Helper-Handlers
        public static bool HandleExecuteACMethod_BakeryReceivingPoint(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PAMPlatformscale(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
        #endregion
    }
}
