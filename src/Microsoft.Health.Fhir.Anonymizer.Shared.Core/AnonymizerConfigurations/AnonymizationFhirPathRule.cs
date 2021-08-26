using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    public class AnonymizationFhirPathRule : AnonymizerRule
    {
        private static Regex s_pathRegex = new Regex(@"^(?<resourceType>[A-Z][a-zA-Z]*)?(\.)?(?<expression>.*?)$");

        public string Expression { get; set; }

        public string ResourceType { get; private set; }

        public bool IsResourceTypeRule { get { return Path.Equals(ResourceType); } }

        public static AnonymizationFhirPathRule CreateAnonymizationFhirPathRule(Dictionary<string, object> config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (!config.ContainsKey(Constants.PathKey))
            {
                throw new ArgumentException("Missing path in rule config");
            }

            if (!config.ContainsKey(Constants.MethodKey))
            {
                throw new ArgumentException("Missing method in rule config");
            }

            string path = config[Constants.PathKey].ToString();
            string method = config[Constants.MethodKey].ToString();

            // Parse expression and resource type from path
            string resourceType = null;
            string expression = null;
            var match = s_pathRegex.Match(path);
            if (match.Success)
            {
                resourceType = match.Groups["resourceType"].Value;
                expression = match.Groups["expression"].Value;
            }

            if (string.IsNullOrEmpty(expression))
            {
                // For case: Path == "Resource"
                expression = path;
            }

            return new AnonymizationFhirPathRule(path, expression, resourceType,
                method, AnonymizerRuleType.FhirPathRule, path, config);
        }

        public AnonymizationFhirPathRule(string path, string expression, string resourceType, string method,
            AnonymizerRuleType type, string source, Dictionary<string, object> settings = null)
            : base(path, method, type, source)
        {
            if (string.IsNullOrEmpty(expression))
            {
                throw new ArgumentNullException("expression");
            }

            Expression = expression;
            ResourceType = resourceType;
            RuleSettings = settings;
        }
    }
}
