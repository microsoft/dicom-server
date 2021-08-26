using System.Collections.Generic;
using System.IO;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.FhirPath;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Microsoft.Health.Fhir.Anonymizer.Core.Validation;

namespace Microsoft.Health.Fhir.Anonymizer.Core
{
    public class AnonymizerEngine
    {
        private readonly FhirJsonParser _parser = new FhirJsonParser();
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizerEngine>();
        private readonly ResourceValidator _validator = new ResourceValidator();
        private readonly AnonymizerConfigurationManager _configurationManager;
        private readonly Dictionary<string, IAnonymizerProcessor> _processors;
        private readonly AnonymizationFhirPathRule[] _rules;
        private readonly IStructureDefinitionSummaryProvider _provider = new PocoStructureDefinitionSummaryProvider();

        public static void InitializeFhirPathExtensionSymbols()
        {
            FhirPathCompiler.DefaultSymbolTable.AddExtensionSymbols();
        }

        public AnonymizerEngine(string configFilePath) : this(AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath))
        {

        }

        public AnonymizerEngine(AnonymizerConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
            _processors = new Dictionary<string, IAnonymizerProcessor>();

            InitializeProcessors(_configurationManager);

            _rules = _configurationManager.FhirPathRules;

            _logger.LogDebug("AnonymizerEngine initialized successfully");
        }

        public static AnonymizerEngine CreateWithFileContext(string configFilePath)
        {
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath);
            var dateShiftKeyPrefix = string.Empty;
            configurationManager.SetDateShiftKeyPrefix(dateShiftKeyPrefix);
            return new AnonymizerEngine(configurationManager);
        }

        public ITypedElement AnonymizeElement(ITypedElement element, AnonymizerSettings settings = null)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            try
            {
                ElementNode resourceNode = ElementNode.FromElement(element);
                return resourceNode.Anonymize(_rules, _processors);
            }
            catch (AnonymizerProcessingException)
            {
                if (_configurationManager.Configuration.processingErrors == ProcessingErrorsOption.Skip)
                {
                    // Return empty resource.
                    return new EmptyElement(element.InstanceType);
                }

                throw;
            }
        }

        public Resource AnonymizeResource(Resource resource, AnonymizerSettings settings = null)
        {
            EnsureArg.IsNotNull(resource, nameof(resource));

            ValidateInput(settings, resource);
            var anonymizedResource = AnonymizeElement(resource.ToTypedElement()).ToPoco<Resource>();
            ValidateOutput(settings, anonymizedResource);

            return anonymizedResource;
        }

        public string AnonymizeJson(string json, AnonymizerSettings settings = null)
        {
            EnsureArg.IsNotNullOrEmpty(json, nameof(json));

            var resource = _parser.Parse<Resource>(json);
            ITypedElement anonymizedElement = AnonymizeElement(resource.ToTypedElement(), settings);

            FhirJsonSerializationSettings serializationSettings = new FhirJsonSerializationSettings
            {
                Pretty = settings != null && settings.IsPrettyOutput
            };
            return anonymizedElement.ToJson(serializationSettings);
        }

        private void ValidateInput(AnonymizerSettings settings, Resource resource)
        {
            if (settings != null && settings.ValidateInput)
            {
                _validator.ValidateInput(resource);
            }
        }

        private void ValidateOutput(AnonymizerSettings settings, Resource anonymizedNode)
        {
            if (settings != null && settings.ValidateOutput)
            {
                _validator.ValidateOutput(anonymizedNode);
            }
        }

        private void InitializeProcessors(AnonymizerConfigurationManager configurationManager)
        {
            _processors[AnonymizerMethod.DateShift.ToString().ToUpperInvariant()] = DateShiftProcessor.Create(configurationManager);
            _processors[AnonymizerMethod.Redact.ToString().ToUpperInvariant()] = RedactProcessor.Create(configurationManager);
            _processors[AnonymizerMethod.CryptoHash.ToString().ToUpperInvariant()] = new CryptoHashProcessor(configurationManager.GetParameterConfiguration().CryptoHashKey);
            _processors[AnonymizerMethod.Encrypt.ToString().ToUpperInvariant()] = new EncryptProcessor(configurationManager.GetParameterConfiguration().EncryptKey);
            _processors[AnonymizerMethod.Substitute.ToString().ToUpperInvariant()] = new SubstituteProcessor();
            _processors[AnonymizerMethod.Perturb.ToString().ToUpperInvariant()] = new PerturbProcessor();
            _processors[AnonymizerMethod.Keep.ToString().ToUpperInvariant()] = new KeepProcessor();
            _processors[AnonymizerMethod.Generalize.ToString().ToUpperInvariant()] = new GeneralizeProcessor();
        }
    }
}
