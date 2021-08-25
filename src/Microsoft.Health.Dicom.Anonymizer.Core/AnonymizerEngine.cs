// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Microsoft.Health.Dicom.Anonymizer.Core.Rules;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    public class AnonymizerEngine
    {
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizerEngine>();
        private readonly AnonymizerEngineOptions _anonymizerSettings;
        private readonly AnonymizerRule[] _rules;

        public AnonymizerEngine(string configFilePath = "configuration.json", AnonymizerEngineOptions anonymizerSettings = null, IAnonymizerRuleFactory ruleFactory = null)
            : this(AnonymizerConfigurationManager.CreateFromJsonFile(configFilePath), anonymizerSettings, ruleFactory)
        {
        }

        public AnonymizerEngine(AnonymizerConfigurationManager configurationManager, AnonymizerEngineOptions anonymizerSettings = null, IAnonymizerRuleFactory ruleFactory = null)
        {
            EnsureArg.IsNotNull(configurationManager, nameof(configurationManager));

            _anonymizerSettings = anonymizerSettings ?? new AnonymizerEngineOptions();
            ruleFactory ??= new AnonymizerRuleFactory(configurationManager.Configuration, new DicomProcessorFactory());
            _rules = ruleFactory.CreateDicomAnonymizationRules(configurationManager.Configuration.RuleContent);
            _logger.LogDebug("Successfully initialized anonymizer engine.");
        }

        public void AnonymizeDataset(DicomDataset dataset)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            // Validate input dataset.
            if (_anonymizerSettings.ValidateInput)
            {
                dataset.Validate();
            }

            var context = InitContext(dataset);
            DicomUtility.DisableAutoValidation(dataset);

            foreach (var rule in _rules)
            {
                rule.Handle(dataset, context);
                _logger.LogDebug($"Successfully handled rule {rule.Description}.");
            }

            // Validate output dataset.
            if (_anonymizerSettings.ValidateOutput)
            {
                dataset.Validate();
            }
        }

        private ProcessContext InitContext(DicomDataset dataset)
        {
            var context = new ProcessContext
            {
                StudyInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                SopInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
                SeriesInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
            };
            return context;
        }
    }
}
