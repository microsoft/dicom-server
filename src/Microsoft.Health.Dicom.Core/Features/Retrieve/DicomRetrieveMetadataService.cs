// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class DicomRetrieveMetadataService : IDicomRetrieveMetadataService
    {
        private readonly IDicomInstanceStore _dicomInstanceStore;
        private readonly IDicomMetadataStore _dicomMetadataStore;
        private readonly ILogger<DicomRetrieveMetadataService> _logger;

        public DicomRetrieveMetadataService(
            IDicomInstanceStore dicomInstanceStore,
            IDicomMetadataStore dicomMetadataStore,
            ILogger<DicomRetrieveMetadataService> logger)
        {
            EnsureArg.IsNotNull(dicomInstanceStore, nameof(dicomInstanceStore));
            EnsureArg.IsNotNull(dicomMetadataStore, nameof(dicomMetadataStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _dicomInstanceStore = dicomInstanceStore;
            _dicomMetadataStore = dicomMetadataStore;
            _logger = logger;
        }

        public async Task<DicomRetrieveMetadataResponse> GetDicomInstanceMetadataAsync(
            DicomRetrieveMetadataRequest message,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            IEnumerable<DicomInstanceIdentifier> retrieveInstances = await _dicomInstanceStore.GetInstancesToRetrieve(
            message.ResourceType,
            message.StudyInstanceUid,
            message.SeriesInstanceUid,
            message.SopInstanceUid,
            cancellationToken);

            var responseCode = HttpStatusCode.OK;

            List<DicomDataset> dataset = new List<DicomDataset>();

            try
            {
                foreach (var id in retrieveInstances)
                {
                    DicomDataset ds = await _dicomMetadataStore.GetInstanceMetadataAsync(id, cancellationToken);
                    dataset.Add(ds);
                }
            }
            catch (DicomDataStoreException ex)
            {
                // If couldnot retrieve metadata for any instances
                if (!dataset.Any())
                {
                    _logger.LogError(ex, "Error retrieving dicom instance metadata.");
                    throw new DicomInstanceMetadataNotFoundException();
                }

                if (dataset.Count < retrieveInstances.Count())
                {
                    // Metadata was retireved only for some instances
                    _logger.LogWarning(ex, "Error retrieving metadata for all dicom instances.");
                    responseCode = HttpStatusCode.PartialContent;
                }
            }

            return new DicomRetrieveMetadataResponse(responseCode, dataset);
        }
    }
}
