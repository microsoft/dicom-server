// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    /// <summary>
    /// This processor is used to replace the target field with a fixed and valid value.
    /// </summary>
    public class SubstituteProcessor : IAnonymizerProcessor
    {
        private readonly string _replaceString = "Anonymous";
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<SubstituteProcessor>();

        public SubstituteProcessor(JObject settingObject)
        {
            EnsureArg.IsNotNull(settingObject, nameof(settingObject));

            if (settingObject.TryGetValue("replaceWith", StringComparison.OrdinalIgnoreCase, out JToken replaced))
            {
                _replaceString = replaced.ToString();
            }
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            try
            {
                if (item.ValueRepresentation == DicomVR.OW && !(item is DicomFragmentSequence))
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, ushort.Parse(_replaceString));
                }
                else if (item.ValueRepresentation == DicomVR.OL)
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, uint.Parse(_replaceString));
                }
                else if (item.ValueRepresentation == DicomVR.OD)
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, double.Parse(_replaceString));
                }
                else if (item.ValueRepresentation == DicomVR.OF)
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, float.Parse(_replaceString));
                }
                else if (item.ValueRepresentation == DicomVR.AT)
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, DicomTag.Parse(_replaceString));
                }
                else
                {
                    dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, _replaceString);
                }

                _logger.LogDebug($"The value of DICOM item '{item}' is substituted.");
            }
            catch (Exception ex)
            {
                throw new AnonymizerConfigurationException(DicomAnonymizationErrorCode.InvalidConfigurationValues, "Substitute process failed. Invalid replace value.", ex);
            }
        }

        public bool IsSupported(DicomItem item)
        {
            if (item is DicomOtherByte || item is DicomSequence || item is DicomFragmentSequence)
            {
                return false;
            }

            return true;
        }
    }
}
