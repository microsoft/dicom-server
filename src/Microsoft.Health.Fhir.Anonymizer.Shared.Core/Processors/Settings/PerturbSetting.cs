using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings
{
    public class PerturbSetting
    {
        public double Span { get; set; }
        public PerturbRangeType RangeType { get; set; }
        public int RoundTo { get; set; }

        public static PerturbSetting CreateFromRuleSettings(Dictionary<string, object> ruleSettings)
        {
            EnsureArg.IsNotNull(ruleSettings);

            var roundTo = 2;
            if (ruleSettings.ContainsKey(RuleKeys.RoundTo))
            {
                roundTo = Convert.ToInt32(ruleSettings.GetValueOrDefault(RuleKeys.RoundTo)?.ToString());
            }

            double span = 0;
            if (ruleSettings.ContainsKey(RuleKeys.Span))
            {
                span = Convert.ToDouble(ruleSettings.GetValueOrDefault(RuleKeys.Span)?.ToString());
            }

            var rangeType = PerturbRangeType.Fixed;
            if (string.Equals(PerturbRangeType.Proportional.ToString(),
                ruleSettings.GetValueOrDefault(RuleKeys.RangeType)?.ToString(),
                StringComparison.OrdinalIgnoreCase))
            {
                rangeType = PerturbRangeType.Proportional;
            }

            return new PerturbSetting
            {
                Span = span,
                RangeType = rangeType,
                RoundTo = roundTo
            };
        }

        public static void ValidateRuleSettings(Dictionary<string, object> ruleSettings)
        {
            if (ruleSettings == null)
            {
                throw new AnonymizerConfigurationException($"Perturb rule should not be null.");
            }

            if (!ruleSettings.ContainsKey(Constants.PathKey))
            {
                throw new AnonymizerConfigurationException("Missing path in FHIR path rule config.");
            }

            if (ruleSettings.ContainsKey(RuleKeys.RoundTo))
            {
                try
                {
                    var roundTo = Convert.ToInt32(ruleSettings.GetValueOrDefault(RuleKeys.RoundTo)?.ToString());
                    if (roundTo < 0 || roundTo > 28)
                    {
                        throw new ArgumentException();
                    }
                }
                catch
                {
                    throw new AnonymizerConfigurationException($"RoundTo value is invalid at {ruleSettings[Constants.PathKey]}.");
                }
            }

            if (ruleSettings.ContainsKey(RuleKeys.Span))
            {
                try
                {
                    var span = Convert.ToDouble(ruleSettings.GetValueOrDefault(RuleKeys.Span)?.ToString());
                    if (span < 0)
                    {
                        throw new ArgumentException();
                    }
                }
                catch
                {
                    throw new AnonymizerConfigurationException($"Span value is invalid at {ruleSettings[Constants.PathKey]}.");
                }
            }
            else
            {
                throw new AnonymizerConfigurationException($"Span value is required in perturb rule at {ruleSettings[Constants.PathKey]}.");
            }

            var supportedRangeTypes = Enum.GetNames(typeof(PerturbRangeType)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            if (ruleSettings.ContainsKey(RuleKeys.RangeType)
                && !supportedRangeTypes.Contains(ruleSettings[RuleKeys.RangeType]?.ToString()))
            {
                throw new AnonymizerConfigurationException($"RangeType value is invalid at {ruleSettings[Constants.PathKey]}.");
            }
        }
    }
}
