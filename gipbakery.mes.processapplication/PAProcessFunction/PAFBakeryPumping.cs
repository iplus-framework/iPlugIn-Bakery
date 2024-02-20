using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Pump over'}de{'Umpumpen'}", Global.ACKinds.TPAProcessFunction, Global.ACStorableTypes.Required, false, PWBakeryPumping.PWClassName, true)]
    public class PAFBakeryPumping : PAProcessFunction
    {
        #region c'tors

        static PAFBakeryPumping()
        {
            ACMethod.RegisterVirtualMethod(typeof(PAFBakeryPumping), ACStateConst.TMStart, CreateVirtualMethod(VMethodName_BakeryPumping, "en{'Pump over'}de{'Umpumpen'}", typeof(PWBakeryPumping)));
            RegisterExecuteHandler(typeof(PAFBakeryPumping), HandleExecuteACMethod_PAFBakeryPumping);
        }

        public PAFBakeryPumping(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "") : 
            base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public const string ClassName = "PAFBakeryPumping";
        public const string VMethodName_BakeryPumping = "BakeryPumping";

        #endregion

        #region Methods

        [ACMethodAsync("Process", "en{'Start'}de{'Start'}", (short)MISort.Start, false)]
        public override ACMethodEventArgs Start(ACMethod acMethod)
        {
            return base.Start(acMethod);
        }

        private static bool HandleExecuteACMethod_PAFBakeryPumping(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, object[] acParameter)
        {
            return HandleExecuteACMethod_PAProcessFunction(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }

        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            result = null;
            switch (acMethodName)
            {
                case "InheritParamsFromConfig":
                    InheritParamsFromConfig(acParameter[0] as ACMethod, acParameter[1] as ACMethod, (bool) acParameter[2]);
                    return true;

                case "SetDefaultACMethodValues":
                    SetDefaultACMethodValues(acParameter[0] as ACMethod);
                    return true;
            }

            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        protected override MsgWithDetails CompleteACMethodOnSMStarting(ACMethod acMethod, ACMethod previousParams)
        {
            return base.CompleteACMethodOnSMStarting(acMethod, previousParams);
        }

        public override void InitializeRouteAndConfig(Database dbIPlus)
        {
            gip.core.datamodel.ACClass thisACClass = this.ComponentClass;
            gip.core.datamodel.ACClass parentACClass = ParentACComponent.ComponentClass;
            try
            {
                ACRoutingParameters routingParameters = new ACRoutingParameters()
                {
                    Database = dbIPlus,
                    DBSelector = (c, p, r) => c.ACKind == Global.ACKinds.TPAProcessModule && c.ACClassID != parentACClass.ACClassID,
                    Direction = RouteDirections.Forwards,
                    DBIncludeInternalConnections = true
                };

                var routes = ACRoutingService.DbSelectRoutesFromPoint(thisACClass, this.PAPointMatOut1.PropertyInfo, routingParameters);
                if (routes != null && routes.Any())
                {
                    string virtMethodName = VMethodName_BakeryPumping;
                    IReadOnlyList<ACMethodWrapper> virtualMethods = ACMethod.GetVirtualMethodInfos(this.GetType(), ACStateConst.TMStart);
                    if (virtualMethods != null && virtualMethods.Any())
                        virtMethodName = virtualMethods.FirstOrDefault().Method.ACIdentifier;
                    virtMethodName = OnGetVMethodNameForRouteInitialization(virtMethodName);

                    foreach (Route route in routes)
                    {
                        ACMethod acMethod = ACUrlACTypeSignature("!" + virtMethodName);
                        GetACMethodFromConfig(dbIPlus, route, acMethod, true);
                    }
                }
            }
            catch (Exception e)
            {
                Messages.LogException(this.GetACUrl(), "InitializeRouteAndConfig(0)", e.Message);
            }
        }

        protected MsgWithDetails GetACMethodFromConfig(Database db, Route route, ACMethod acMethod, bool isConfigInitialization = false)
        {
            if (route == null || route.Count < 1 || ParentACComponent == null)
                return new MsgWithDetails();
            if (IsMethodChangedFromClient)
                return null;
            RouteItem sourceRouteItem = route.Where(c => c.SourceGuid == ParentACComponent.ComponentClass.ACClassID).FirstOrDefault();
            if (sourceRouteItem == null)
                sourceRouteItem = route.FirstOrDefault();
            if (sourceRouteItem.Source.ACKind == Global.ACKinds.TPAProcessFunction)
            {
                if (route.Count < 2)
                    return new MsgWithDetails();
                sourceRouteItem = route[1];
            }
            RouteItem targetRouteItem = route.LastOrDefault();

            ACValue valueDest = acMethod.ParameterValueList.GetACValue("Destination");
            if (valueDest != null && targetRouteItem != null)
            {
                try
                {
                    bool setTarget = false;
                    if (valueDest.Value is Int16)
                    {
                        setTarget = valueDest.ParamAsInt16 <= 0;
                    }
                    else if (valueDest.Value is UInt16)
                    {
                        setTarget = valueDest.ParamAsUInt16 <= 0;
                    }

                    if (setTarget)
                    {
                        PAProcessModule pamTarget = sourceRouteItem.TargetACComponent as PAProcessModule;
                        if (pamTarget != null)
                        {
                            if (valueDest.Value is Int16)
                                valueDest.Value = Convert.ToInt16(pamTarget.RouteItemIDAsNum);
                            else if (valueDest.Value is UInt16)
                                valueDest.Value = Convert.ToUInt16(pamTarget.RouteItemIDAsNum);
                        }
                    }
                }
                catch (Exception ec)
                {
                    string msg = ec.Message;
                    if (ec.InnerException != null && ec.InnerException.Message != null)
                        msg += " Inner:" + ec.InnerException.Message;

                    Messages.LogException("PAFBakeryPumping", "GetACMethodFromConfig", msg);
                }
            }


            List<MaterialConfig> materialConfigList = null;
            gip.core.datamodel.ACClass thisACClass = ComponentClass.FromIPlusContext<gip.core.datamodel.ACClass>(db);
            gip.core.datamodel.ACClassConfig config = null;
            gip.core.datamodel.ACClassPropertyRelation logicalRelation = db.ACClassPropertyRelation
                .Where(c => c.SourceACClassID == sourceRouteItem.Source.ACClassID
                            && c.SourceACClassPropertyID == sourceRouteItem.SourceProperty.ACClassPropertyID
                            && c.TargetACClassID == targetRouteItem.Target.ACClassID
                            && c.TargetACClassPropertyID == targetRouteItem.TargetProperty.ACClassPropertyID)
                .FirstOrDefault();
            if (logicalRelation == null)
            {
                logicalRelation = gip.core.datamodel.ACClassPropertyRelation.NewACObject(db, null);
                logicalRelation.SourceACClass = sourceRouteItem.Source;
                logicalRelation.SourceACClassProperty = sourceRouteItem.SourceProperty;
                logicalRelation.TargetACClass = targetRouteItem.Target;
                logicalRelation.TargetACClassPropertyID = targetRouteItem.TargetProperty.ACClassPropertyID;
                logicalRelation.ConnectionType = Global.ConnectionTypes.DynamicConnection;
            }
            else
            {
                config = logicalRelation.ACClassConfig_ACClassPropertyRelation.FirstOrDefault();
                if (!isConfigInitialization)
                {
                    PAMSilo pamSilo = targetRouteItem.TargetACComponent as PAMSilo;
                    if (pamSilo != null)
                    {
                        if (pamSilo.Facility != null && pamSilo.Facility.ValueT != null && pamSilo.Facility.ValueT.ValueT != null)
                        {
                            Guid? materialID = pamSilo.Facility.ValueT.ValueT.MaterialID;
                            if (materialID.HasValue && materialID != Guid.Empty)
                            {
                                Guid acClassIDOfParent = ParentACComponent.ComponentClass.ACClassID;
                                using (var dbApp = new DatabaseApp())
                                {
                                    // 1. Hole Material-Konfiguration spezielle fü diesen Weg
                                    materialConfigList = dbApp.MaterialConfig.Where(c => c.VBiACClassPropertyRelationID == logicalRelation.ACClassPropertyRelationID && c.MaterialID == materialID.Value).SetMergeOption(System.Data.Objects.MergeOption.NoTracking).ToList();
                                    var wayIndependent = dbApp.MaterialConfig.Where(c => c.MaterialID == materialID.Value && c.VBiACClassID == acClassIDOfParent).SetMergeOption(System.Data.Objects.MergeOption.NoTracking);
                                    foreach (var matConfigIndepedent in wayIndependent)
                                    {
                                        if (!materialConfigList.Where(c => c.LocalConfigACUrl == matConfigIndepedent.LocalConfigACUrl).Any())
                                            materialConfigList.Add(matConfigIndepedent);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            ACMethod storedACMethod = null;
            if (config == null)
            {
                config = thisACClass.NewACConfig(null, db.GetACType(typeof(ACMethod))) as gip.core.datamodel.ACClassConfig;
                config.KeyACUrl = logicalRelation.GetKey();
                config.ACClassPropertyRelation = logicalRelation;
            }
            else
                storedACMethod = config.Value as ACMethod;

            bool isNewDefaultedMethod = false;
            bool differentVirtualMethod = false;
            if (storedACMethod == null || storedACMethod.ACIdentifier != acMethod.ACIdentifier)
            {
                if (storedACMethod != null && storedACMethod.ACIdentifier != acMethod.ACIdentifier)
                {
                    differentVirtualMethod = true;
                    var clonedMethod = acMethod.Clone() as ACMethod;
                    clonedMethod.CopyParamValuesFrom(storedACMethod);
                    storedACMethod = clonedMethod;
                }
                else
                {
                    isNewDefaultedMethod = true;
                    storedACMethod = acMethod.Clone() as ACMethod;
                    ACUrlCommand("!SetDefaultACMethodValues", storedACMethod);
                }
            }
            // Überschreibe Parameter mit materialabhängigen Einstellungen
            if (!isConfigInitialization
                && config.EntityState != System.Data.EntityState.Added
                && materialConfigList != null
                && materialConfigList.Any())
            {
                foreach (var matConfig in materialConfigList)
                {
                    ACValue acValue = acMethod.ParameterValueList.Where(c => c.ACIdentifier == matConfig.LocalConfigACUrl).FirstOrDefault();
                    if (acValue != null/* && acValue.HasDefaultValue*/)
                        acValue.Value = matConfig.Value;
                    if (storedACMethod != null)
                    {
                        acValue = storedACMethod.ParameterValueList.Where(c => c.ACIdentifier == matConfig.LocalConfigACUrl).FirstOrDefault();
                        if (acValue != null/* && acValue.HasDefaultValue*/)
                            acValue.Value = matConfig.Value;
                    }
                }
            }
            if (!isNewDefaultedMethod)
                ACUrlCommand("!InheritParamsFromConfig", acMethod, storedACMethod, isConfigInitialization);
            if (config.EntityState == System.Data.EntityState.Added || isNewDefaultedMethod)
                config.Value = storedACMethod;
            else if (isConfigInitialization)
            {
                if (differentVirtualMethod)
                    config.Value = storedACMethod;
                else
                    config.Value = acMethod;
            }
            if (config.EntityState == System.Data.EntityState.Added || logicalRelation.EntityState == System.Data.EntityState.Added || isNewDefaultedMethod || isConfigInitialization || differentVirtualMethod)
            {
                MsgWithDetails msg = db.ACSaveChanges();
                if (msg != null)
                    return msg;
            }
            return null;
        }

        [ACMethodInfo("Function", "en{'Inherirt params from config'}de{'Übernehme Dosierparameter aus Konfiguration'}", 9999)]
        public virtual void InheritParamsFromConfig(ACMethod newACMethod, ACMethod configACMethod, bool isConfigInitialization)
        {
            if (isConfigInitialization)
            {
                object valueSource = null;
                ACValue acValueSource = newACMethod.ParameterValueList.GetACValue("Source");
                if (acValueSource != null)
                    valueSource = acValueSource.Value;

                object valueDestination = null;
                ACValue acValueDestination = newACMethod.ParameterValueList.GetACValue("Destination");
                if (acValueDestination != null)
                    valueDestination = acValueDestination.Value;

                newACMethod.ParameterValueList.CopyValues(configACMethod.ParameterValueList);

                try
                {
                    if (acValueSource != null)
                        newACMethod.ParameterValueList["Source"] = valueSource;
                    newACMethod.ParameterValueList["Route"] = null;
                    if (acValueDestination != null)
                        newACMethod.ParameterValueList["Destination"] = valueDestination;
                    newACMethod.ParameterValueList["TargetQuantity"] = 0.0;
                    newACMethod.ParameterValueList["ScaleACUrl"] = "";
                }
                catch (Exception ec)
                {
                    string msg = ec.Message;
                    if (ec.InnerException != null && ec.InnerException.Message != null)
                        msg += " Inner:" + ec.InnerException.Message;

                    Messages.LogException("PAFBakeryPumping", "InheritParamsFromConfig", msg);
                }
            }
            else
            {
                if (newACMethod.ParameterValueList.GetInt16("Power") <= 0)
                    newACMethod["Power"] = configACMethod.ParameterValueList.GetInt16("Power");
            }
        }

        [ACMethodInfo("Function", "en{'Default dosing parameters'}de{'Standard Dosierparameter'}", 9999)]
        public virtual void SetDefaultACMethodValues(ACMethod newACMethod)
        {
            newACMethod["Power"] = (Int16)0;
        }

        protected static ACMethodWrapper CreateVirtualMethod(string acIdentifier, string captionTranslation, Type pwClass)
        {
            ACMethod method = new ACMethod(acIdentifier);

            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();
            method.ParameterValueList.Add(new ACValue("Source", typeof(Int16), (Int16)0, Global.ParamOption.Optional));
            paramTranslation.Add("Source", "en{'Source'}de{'Quelle'}");
            method.ParameterValueList.Add(new ACValue("Route", typeof(Route), null, Global.ParamOption.Required));
            paramTranslation.Add("Route", "en{'Route'}de{'Route'}");
            method.ParameterValueList.Add(new ACValue("Destination", typeof(Int16), (Int16)0, Global.ParamOption.Required));
            paramTranslation.Add("Destination", "en{'Destination'}de{'Ziel'}");
            method.ParameterValueList.Add(new ACValue("TargetQuantity", typeof(Double), (Double)0.0, Global.ParamOption.Optional));
            paramTranslation.Add("TargetQuantity", "en{'Target quantity'}de{'Sollmenge'}");
            method.ParameterValueList.Add(new ACValue("Power", typeof(Int16), (Int16)0, Global.ParamOption.Optional));
            paramTranslation.Add("Power", "en{'Power'}de{'Leistung'}");
            method.ParameterValueList.Add(new ACValue("ScaleACUrl", typeof(string), "", Global.ParamOption.Optional));
            paramTranslation.Add("ScaleACUrl", "en{'Scale ACUrl'}de{'Waage ACUrl'}");

            Dictionary<string, string> resultTranslation = new Dictionary<string, string>();
            method.ResultValueList.Add(new ACValue("ActualQuantity", typeof(Double), (Double)0.0, Global.ParamOption.Optional));
            resultTranslation.Add("ActualQuantity", "en{'Actual quantity'}de{'Istmenge'}");

            return new ACMethodWrapper(method, captionTranslation, pwClass, paramTranslation, resultTranslation);
        }

        #endregion
    }
}
