// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    /// <summary>
    /// Keep the original value.
    /// </summary>
    public class KeepProcessor : IAnonymizerProcessor
    {
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<KeepProcessor>();

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            _logger.LogDebug($"The value of DICOM item '{item}' is kept.");
        }

        public bool IsSupported(DicomItem item)
        {
            return true;
        }
    }
}
