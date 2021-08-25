// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Rules
{
    public class AnonymizerRuleFactory : IAnonymizerRuleFactory
    {
        private readonly AnonymizerDefaultSettings _defaultSettings;

        private readonly Dictionary<string, JObject> _customSettings;

        private readonly IAnonymizerProcessorFactory _processorFactory;

        private static readonly HashSet<string> _supportedMethods = Enum.GetNames(typeof(AnonymizerMethod)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);

        public AnonymizerRuleFactory(AnonymizerConfiguration configuration, IAnonymizerProcessorFactory processorFactory)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(processorFactory, nameof(processorFactory));

            _defaultSettings = configuration.DefaultSettings;
            _customSettings = configuration.CustomSettings;
            _processorFactory = processorFactory;
        }

        public AnonymizerRule[] CreateDicomAnonymizationRules(JObject[] ruleContents)
        {
            return ruleContents?.Select(entry => CreateDicomAnonymizationRule(entry)).ToArray();
        }

        public AnonymizerRule CreateDicomAnonymizationRule(JObject ruleContent)
        {
            EnsureArg.IsNotNull(ruleContent, nameof(ruleContent));

            // Parse and validate method
            if (!ruleContent.ContainsKey(Constants.MethodKey))
            {
                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.MissingConfigurationFields, "Missing a required field 'method' in rule config.");
            }

            var method = ruleContent[Constants.MethodKey].ToString();
            if (!_supportedMethods.Contains(method))
            {
                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationRule, $"Anonymization method '{method}' is not supported.");
            }

            // Parse and validate settings
            JObject ruleSetting = ExtractRuleSetting(ruleContent, method);

            // Parse and validate tag
            if (ruleContent.ContainsKey(Constants.TagKey))
            {
                var createRuleFuncs =
                    new Func<string, string, string, IAnonymizerProcessorFactory, JObject, AnonymizerRule>[]
                {
                    TryCreateRule<DicomTag, AnonymizerTagRule>,
                    TryCreateRule<DicomMaskedTag, AnonymizerMaskedTagRule>,
                    TryCreateRule<DicomVR, AnonymizerVRRule>,
                    TryCreateTagNameRule,
                };

                var tagContent = ruleContent[Constants.TagKey].ToString();
                foreach (var func in createRuleFuncs)
                {
                    var rule = func(tagContent, method, ruleContent.ToString(), _processorFactory, ruleSetting);
                    if (rule != null)
                    {
                        return rule;
                    }
                }

                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.InvalidConfigurationValues, $"Invalid tag '{tagContent}' in rule config.");
            }
            else
            {
                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.MissingConfigurationFields, "Missing a required field 'tag' in rule config.");
            }
        }

        private JObject ExtractRuleSetting(JObject ruleContent, string method)
        {
            JObject parameters = null;
            if (ruleContent.ContainsKey(Constants.Parameters))
            {
                parameters = ruleContent[Constants.Parameters].ToObject<JObject>();
            }

            JObject ruleSetting = _defaultSettings.GetDefaultSetting(method);
            if (ruleContent.ContainsKey(Constants.RuleSetting))
            {
                if (_customSettings == null || !_customSettings.ContainsKey(ruleContent[Constants.RuleSetting].ToString()))
                {
                    throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.MissingRuleSettings, $"Customized setting {ruleContent[Constants.RuleSetting]} not defined.");
                }

                ruleSetting = _customSettings[ruleContent[Constants.RuleSetting].ToString()];
            }

            if (ruleSetting == null)
            {
                ruleSetting = parameters;
            }
            else
            {
                ruleSetting.Merge(parameters, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Concat });
            }

            return ruleSetting;
        }

        private static AnonymizerRule TryCreateRule<TItem, TResult>(
            string tagContent,
            string method,
            string description,
            IAnonymizerProcessorFactory processorFactory,
            JObject ruleSetting)
        {
            object outputTag;
            try
            {
                outputTag = (TItem)typeof(TItem).GetMethod("Parse", new Type[] { typeof(string) }).Invoke(null, new object[] { tagContent });
            }
            catch
            {
                return null;
            }

            try
            {
                return (AnonymizerRule)Activator.CreateInstance(
                    typeof(TResult),
                    new object[] { outputTag, method, description, processorFactory, ruleSetting });
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }
        }

        private static AnonymizerRule TryCreateTagNameRule(string tagContent, string method, string description, IAnonymizerProcessorFactory processorFactory, JObject ruleSetting)
        {
            var nameField = typeof(DicomTag).GetField(tagContent);
            if (nameField != null)
            {
                var tag = (DicomTag)nameField.GetValue(null);
                return new AnonymizerTagRule(tag, method, description, processorFactory, ruleSetting);
            }

            var retiredNameField = typeof(DicomTag).GetField(tagContent + "RETIRED");
            if (retiredNameField != null)
            {
                var tag = (DicomTag)retiredNameField.GetValue(null);
                return new AnonymizerTagRule(tag, method, description, processorFactory, ruleSetting);
            }

            return null;
        }
    }
}
