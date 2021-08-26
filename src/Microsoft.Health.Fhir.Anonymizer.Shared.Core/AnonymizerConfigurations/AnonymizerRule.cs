using System.Collections.Generic;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    public class AnonymizerRule
    {
        public string Path { get; set; }

        public string Method { get; set; }

        public AnonymizerRuleType Type { get; set; }

        public string Source { get; set; }

        public Dictionary<string, object> RuleSettings { get; set; }

        public AnonymizerRule(string path, string method, AnonymizerRuleType type, string source)
        {
            Path = path;
            Method = method;
            Type = type;
            Source = source;
        }
    }
}
