using System;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Exceptions
{
    public class AnonymizerRuleNotApplicableException : AnonymizerConfigurationException
    {
        public AnonymizerRuleNotApplicableException(string message) : base(message)
        {
        }

        public AnonymizerRuleNotApplicableException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
