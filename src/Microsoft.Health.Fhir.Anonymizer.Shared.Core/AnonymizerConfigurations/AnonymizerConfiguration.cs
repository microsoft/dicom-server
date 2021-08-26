using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    [DataContract]
    public class AnonymizerConfiguration
    {
        [DataMember(Name = "fhirVersion")]
        public string FhirVersion { get; set; }

        [DataMember(Name = "processingErrors")]
        public ProcessingErrorsOption processingErrors { get; set; } = ProcessingErrorsOption.Raise;

        [DataMember(Name = "fhirPathRules")]
        public Dictionary<string, object>[] FhirPathRules { get; set; }

        [DataMember(Name = "parameters")]
        public ParameterConfiguration ParameterConfiguration { get; set; }

        // Static default crypto hash key to provide a same default key for all engine instances
        private static readonly Lazy<string> s_defaultCryptoKey = new Lazy<string>(() => Guid.NewGuid().ToString("N"));

        public void GenerateDefaultParametersIfNotConfigured()
        {
            // if not configured, a random string will be generated as date shift key, others will keep their default values
            if (ParameterConfiguration == null)
            {
                ParameterConfiguration = new ParameterConfiguration
                {
                    DateShiftKey = Guid.NewGuid().ToString("N"),
                    CryptoHashKey = s_defaultCryptoKey.Value,
                    EncryptKey = s_defaultCryptoKey.Value
                };
                return;
            }

            if (string.IsNullOrEmpty(ParameterConfiguration.DateShiftKey))
            {
                ParameterConfiguration.DateShiftKey = Guid.NewGuid().ToString("N");
            }

            if (string.IsNullOrEmpty(ParameterConfiguration.CryptoHashKey))
            {
                ParameterConfiguration.CryptoHashKey = s_defaultCryptoKey.Value;
            }

            if (string.IsNullOrEmpty(ParameterConfiguration.EncryptKey))
            {
                ParameterConfiguration.EncryptKey = s_defaultCryptoKey.Value;
            }
        }
    }
}
