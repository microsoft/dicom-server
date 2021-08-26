using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings
{
    public class GeneralizeSetting
    {
        public JObject Cases { get; set; }
        public GeneralizationOtherValuesOperation OtherValues { get; set; }

        private static FhirPathCompiler _compiler = new FhirPathCompiler();

        public static GeneralizeSetting CreateFromRuleSettings(Dictionary<string, object> ruleSettings)
        {
            EnsureArg.IsNotNull(ruleSettings);

            GeneralizationOtherValuesOperation otherValues;

            JObject cases = JObject.Parse(ruleSettings.GetValueOrDefault(RuleKeys.Cases)?.ToString());

            if (!Enum.TryParse(ruleSettings.GetValueOrDefault(RuleKeys.OtherValues)?.ToString(), true, out otherValues))
            {
                otherValues = GeneralizationOtherValuesOperation.Redact;
            }

            return new GeneralizeSetting
            {
                OtherValues = otherValues,
                Cases = cases
            };
        }

        public static void ValidateRuleSettings(Dictionary<string, object> ruleSettings)
        {
            if (ruleSettings == null)
            {
                throw new AnonymizerConfigurationException("Generalize rule should not be null.");
            }

            if (!ruleSettings.ContainsKey(Constants.PathKey))
            {
                throw new AnonymizerConfigurationException("Missing path in FHIR path rule config.");
            }

            if (!ruleSettings.ContainsKey(Constants.MethodKey))
            {
                throw new AnonymizerConfigurationException("Missing method in FHIR path rule config.");
            }

            if (!ruleSettings.ContainsKey(RuleKeys.Cases))
            {
                throw new AnonymizerConfigurationException("Missing cases in FHIR path rule config.");
            }

            ValidateCases(ruleSettings);

            var supportedOtherValuesOperations = Enum.GetNames(typeof(GeneralizationOtherValuesOperation)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            if (ruleSettings.ContainsKey(RuleKeys.OtherValues) && !supportedOtherValuesOperations.Contains(ruleSettings[RuleKeys.OtherValues].ToString()))
            {
                throw new AnonymizerConfigurationException($"OtherValues setting is invalid at {ruleSettings[RuleKeys.OtherValues]}.");
            }
        }

        private static void ValidateCases(Dictionary<string, object> ruleSettings)
        {
            JObject cases;
            try
            {
                cases = JObject.Parse(ruleSettings.GetValueOrDefault(RuleKeys.Cases)?.ToString());

            }
            catch (JsonReaderException ex)
            {
                throw new AnonymizerConfigurationException($"Invalid Json format {ruleSettings.GetValueOrDefault(RuleKeys.Cases)?.ToString()}", ex);
            }

            foreach (var eachCase in cases)
            {
                try
                {
                    _compiler.Compile(eachCase.Key.ToString());
                    _compiler.Compile(eachCase.Value.ToString());
                }
                catch (Exception ex)
                {
                    throw new AnonymizerConfigurationException($"Invalid cases expression {eachCase}", ex);
                }
            }
        }
    }
}