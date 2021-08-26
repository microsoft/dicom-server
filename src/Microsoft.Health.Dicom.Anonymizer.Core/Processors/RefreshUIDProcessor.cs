// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    /// <summary>
    /// Replaces the content of a UID with a random one.
    /// The processor makes sure the same original UID will be replaced with the same new UID.
    /// </summary>
    public class RefreshUIDProcessor : IAnonymizerProcessor
    {
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<RefreshUIDProcessor>();

        public static ConcurrentDictionary<string, DicomUID> ReplacedUIDs { get; } = new ConcurrentDictionary<string, DicomUID>();

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            if (item.ValueRepresentation == DicomVR.UI)
            {
                var oldUIDValue = ((DicomElement)item).Get<string>();

                DicomUID newUID = ReplacedUIDs.GetOrAdd(oldUIDValue, DicomUIDGenerator.GenerateDerivedFromUUID());
                var newItem = new DicomUniqueIdentifier(item.Tag, newUID);
                dicomDataset.AddOrUpdate(newItem);

                _logger.LogDebug($"The UID value of DICOM item '{item}' is refreshed.");
            }
            else
            {
                throw new AnonymizerOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, $"RefreshUID is not supported for {item.ValueRepresentation}.");
            }
        }

        public bool IsSupported(DicomItem item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            return DicomDataModel.RefreshUIDSupportedVR.Contains(item.ValueRepresentation);
        }
    }
}
