using gip.core.autocomponent;
using gip.core.datamodel;
using gip.mes.datamodel;
using gip.mes.processapplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioAutomation, "en{'Kneading params'}de{'Knetparameter'}", Global.ACKinds.TPWNodeMethod, Global.ACStorableTypes.Optional, false, PWMethodVBBase.PWClassName, true)]
    public class PWBakeryKneading : PWMixing
    {
        new public const string PWClassName = "PWBakeryKneading";

        #region Constructors

        static PWBakeryKneading()
        {
            ACMethod method;
            method = new ACMethod(ACStateConst.SMStarting);
            Dictionary<string, string> paramTranslation = new Dictionary<string, string>();
            method.ParameterValueList.Add(new ACValue("SkipIfCountComp", typeof(int), 0, Global.ParamOption.Required));
            paramTranslation.Add("SkipIfCountComp", "en{'Skip if count components lower than'}de{'Überspringe wenn Komponentenanzahl kleiner als'}");

            method.ParameterValueList.Add(new ACValue("TempRiseFix", typeof(double), 0, Global.ParamOption.Optional));
            paramTranslation.Add("TempRiseFix", "en{'Temperature rising fix °C'}de{'Erwärmung Fix °C'}");
            method.ParameterValueList.Add(new ACValue("KneadingProgram", typeof(string), 0, Global.ParamOption.Optional));
            paramTranslation.Add("KneadingProgram", "en{'Kneading program no.'}de{'Knetprogramm'}");
            method.ParameterValueList.Add(new ACValue("KneadingTimeSlow", typeof(TimeSpan), TimeSpan.Zero, Global.ParamOption.Required));
            paramTranslation.Add("KneadingTimeSlow", "en{'Kneadingtime slow'}de{'Knetzeit langsam'}");
            method.ParameterValueList.Add(new ACValue("KneadingTimeFast", typeof(TimeSpan), TimeSpan.Zero, Global.ParamOption.Required));
            paramTranslation.Add("KneadingTimeFast", "en{'Kneadingtime fast'}de{'Knetzeit schnell'}");
            method.ParameterValueList.Add(new ACValue("TempRiseSlow", typeof(double), 0, Global.ParamOption.Required));
            paramTranslation.Add("TempRiseSlow", "en{'Temperature rising °C/Min slow'}de{'Erwärmung °C/Min langsam'}");
            method.ParameterValueList.Add(new ACValue("TempRiseFast", typeof(double), 0, Global.ParamOption.Required));
            paramTranslation.Add("TempRiseFast", "en{'Temperature rising °C/Min fast'}de{'Erwärmung °C/Min schnell'}");

            method.ParameterValueList.Add(new ACValue("KneadingTimeSlowHalf", typeof(TimeSpan), TimeSpan.Zero, Global.ParamOption.Required));
            paramTranslation.Add("KneadingTimeSlowHalf", "en{'Kneadingtime slow half quantity'}de{'Knetzeit langsam halbe Menge'}");
            method.ParameterValueList.Add(new ACValue("KneadingTimeFastHalf", typeof(TimeSpan), TimeSpan.Zero, Global.ParamOption.Required));
            paramTranslation.Add("KneadingTimeFastHalf", "en{'Kneadingtime fast half quantity'}de{'Knetzeit schnell halbe Menge'}");
            method.ParameterValueList.Add(new ACValue("TempRiseSlowHalf", typeof(double), 0, Global.ParamOption.Required));
            paramTranslation.Add("TempRiseSlowHalf", "en{'Temperature rising °C/Min slow half quantity'}de{'Erwärmung °C/Min langsam halbe Menge'}");
            method.ParameterValueList.Add(new ACValue("TempRiseFastHalf", typeof(double), 0, Global.ParamOption.Required));
            paramTranslation.Add("TempRiseFastHalf", "en{'Temperature rising °C/Min fast half quantity'}de{'Erwärmung °C/Min schnell halbe Menge'}");

            var wrapper = new ACMethodWrapper(method, "en{'Configuration'}de{'Konfiguration'}", typeof(PWBakeryKneading), paramTranslation, null);
            ACMethod.RegisterVirtualMethod(typeof(PWBakeryKneading), ACStateConst.SMStarting, wrapper);
            RegisterExecuteHandler(typeof(PWBakeryKneading), HandleExecuteACMethod_PWBakeryKneading);
        }

        public PWBakeryKneading(gip.core.datamodel.ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        public override bool ACDeInit(bool deleteACClassTask = false)
        {
            return base.ACDeInit(deleteACClassTask);
        }

        #endregion

        #region Properties
        protected TimeSpan KneadingTimeSlow
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("KneadingTimeSlow");
                    if (acValue != null)
                    {
                        TimeSpan KneadingTimeSlow = acValue.ParamAsTimeSpan;
                        if (KneadingTimeSlow < TimeSpan.Zero)
                            KneadingTimeSlow = TimeSpan.Zero;
                        return KneadingTimeSlow;
                    }
                }
                return TimeSpan.Zero;
            }
        }

        protected TimeSpan KneadingTimeFast
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("KneadingTimeFast");
                    if (acValue != null)
                    {
                        TimeSpan KneadingTimeFast = acValue.ParamAsTimeSpan;
                        if (KneadingTimeFast < TimeSpan.Zero)
                            KneadingTimeFast = TimeSpan.Zero;
                        return KneadingTimeFast;
                    }
                }
                return TimeSpan.Zero;
            }
        }

        protected double TempRiseSlow
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("TempRiseSlow");
                    if (acValue != null)
                    {
                        return acValue.ParamAsDouble;
                    }
                }
                return 0.0;
            }
        }

        protected double TempRiseFast
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("TempRiseFast");
                    if (acValue != null)
                    {
                        return acValue.ParamAsDouble;
                    }
                }
                return 0.0;
            }
        }

        protected double TempRiseFix
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("TempRiseFix");
                    if (acValue != null)
                    {
                        return acValue.ParamAsDouble;
                    }
                }
                return 0.0;
            }
        }

        protected string KneadingProgram
        {
            get
            {
                var method = MyConfiguration;
                if (method != null)
                {
                    var acValue = method.ParameterValueList.GetACValue("KneadingProgram");
                    if (acValue != null)
                    {
                        return acValue.ParamAsString;
                    }
                }
                return null;
            }
        }

        // KneadingProgram

        #endregion

        #region Methods

        #region Execute-Helper-Handlers

        public bool GetKneedingRiseTemperature(DatabaseApp dbApp, bool isForFullBatchSize, out double kneedingTemperature)
        {
            kneedingTemperature = 0;

            ACMethod kneedingNodeConfiguration = this.MyConfiguration;
            if (kneedingNodeConfiguration == null)
            {
                return true;
            }

            ACValue tempFix = kneedingNodeConfiguration.ParameterValueList.GetACValue("TempRiseFix");
            if (tempFix != null)
            {
                double fixTempRise = tempFix.ParamAsDouble;
                if (fixTempRise > 0.00001)
                {
                    kneedingTemperature = fixTempRise;
                    return true;
                }
            }
            
            double temperatureRiseSlow = 0, temperatureRiseFast = 0;
            TimeSpan kneedingSlow = TimeSpan.Zero, kneedingFast = TimeSpan.Zero;

            // Full quantity
            if (isForFullBatchSize)
            {
                ACValue tempRiseSlow = kneedingNodeConfiguration.ParameterValueList.GetACValue("TempRiseSlow");
                if (tempRiseSlow != null)
                    temperatureRiseSlow = tempRiseSlow.ParamAsDouble;

                ACValue tempRiseFast = kneedingNodeConfiguration.ParameterValueList.GetACValue("TempRiseFast");
                if (tempRiseFast != null)
                    temperatureRiseFast = tempRiseFast.ParamAsDouble;

                ACValue kTimeSlow = kneedingNodeConfiguration.ParameterValueList.GetACValue("KneadingTimeSlow");
                if (kTimeSlow != null)
                    kneedingSlow = kTimeSlow.ParamAsTimeSpan;

                ACValue kTimeFast = kneedingNodeConfiguration.ParameterValueList.GetACValue("KneadingTimeFast");
                if (kTimeFast != null)
                    kneedingFast = kTimeFast.ParamAsTimeSpan;

                kneedingTemperature = (kneedingSlow.TotalMinutes * temperatureRiseSlow) + (kneedingFast.TotalMinutes * temperatureRiseFast);
            }
            // Half quantity
            else
            {
                ACValue tempRiseSlow = kneedingNodeConfiguration.ParameterValueList.GetACValue("TempRiseSlowHalf");
                if (tempRiseSlow != null)
                    temperatureRiseSlow = tempRiseSlow.ParamAsDouble;

                ACValue tempRiseFast = kneedingNodeConfiguration.ParameterValueList.GetACValue("TempRiseFastHalf");
                if (tempRiseFast != null)
                    temperatureRiseFast = tempRiseFast.ParamAsDouble;

                ACValue kTimeSlow = kneedingNodeConfiguration.ParameterValueList.GetACValue("KneadingTimeSlowHalf");
                if (kTimeSlow != null)
                    kneedingSlow = kTimeSlow.ParamAsTimeSpan;

                ACValue kTimeFast = kneedingNodeConfiguration.ParameterValueList.GetACValue("KneadingTimeFastHalf");
                if (kTimeFast != null)
                    kneedingFast = kTimeFast.ParamAsTimeSpan;

                kneedingTemperature = (kneedingSlow.TotalMinutes * temperatureRiseSlow) + (kneedingFast.TotalMinutes * temperatureRiseFast);
            }
            return true;
        }

        protected override bool HandleExecuteACMethod(out object result, AsyncMethodInvocationMode invocationMode, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            //result = null;
            //switch (acMethodName)
            //{
            //}
            return base.HandleExecuteACMethod(out result, invocationMode, acMethodName, acClassMethod, acParameter);
        }

        public static bool HandleExecuteACMethod_PWBakeryKneading(out object result, IACComponent acComponent, string acMethodName, gip.core.datamodel.ACClassMethod acClassMethod, params object[] acParameter)
        {
            return HandleExecuteACMethod_PWMixing(out result, acComponent, acMethodName, acClassMethod, acParameter);
        }
        #endregion

        [ACMethodState("en{'Executing'}de{'Ausführend'}", 20, true)]
        public override void SMStarting()
        {
            base.SMStarting();
        }

        #endregion

    }
}
