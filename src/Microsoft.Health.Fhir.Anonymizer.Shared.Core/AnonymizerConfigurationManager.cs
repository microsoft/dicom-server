using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Anonymizer.Core
{
    public sealed class AnonymizerConfigurationManager
    {
        private readonly AnonymizerConfigurationValidator _validator = new AnonymizerConfigurationValidator();
#pragma warning disable IDE0032 // Use auto property
        private readonly AnonymizerConfiguration _configuration;
#pragma warning restore IDE0032 // Use auto property

        public AnonymizationFhirPathRule[] FhirPathRules { get; private set; } = null;
#pragma warning disable IDE0032 // Use auto property
        public AnonymizerConfiguration Configuration { get { return _configuration; } }
#pragma warning restore IDE0032 // Use auto property

        public AnonymizerConfigurationManager(AnonymizerConfiguration configuration)
        {
            _validator.Validate(configuration);
            configuration.GenerateDefaultParametersIfNotConfigured();

            _configuration = configuration;

            FhirPathRules = _configuration.FhirPathRules.Select(entry => AnonymizationFhirPathRule.CreateAnonymizationFhirPathRule(entry)).ToArray();
        }

        public static AnonymizerConfigurationManager CreateFromSettingsInJson(string settingsInJson)
        {
            try
            {
                JsonLoadSettings settings = new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                };
                var token = JToken.Parse(settingsInJson, settings);
                var configuration = token.ToObject<AnonymizerConfiguration>();
                return new AnonymizerConfigurationManager(configuration);
            }
            catch (JsonException innerException)
            {
                throw new AnonymizerConfigurationException($"Failed to parse configuration file", innerException);
            }
        }

        public static AnonymizerConfigurationManager CreateFromConfigurationFile(string configFilePath)
        {
            try
            {
                var content = File.ReadAllText(configFilePath);

                return CreateFromSettingsInJson(content);
            }
            catch (IOException innerException)
            {
                throw new AnonymizerConfigurationException($"Failed to read configuration file {configFilePath}", innerException);
            }
        }

#pragma warning disable CA1024 // Use properties where appropriate
        public ParameterConfiguration GetParameterConfiguration()
#pragma warning restore CA1024 // Use properties where appropriate
        {
            return _configuration.ParameterConfiguration;
        }

        public void SetDateShiftKeyPrefix(string prefix)
        {
            _configuration.ParameterConfiguration.DateShiftKeyPrefix = prefix;
        }
    }
}
