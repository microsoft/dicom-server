// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Resources.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class DicomRetrieveMetadataService : IDicomRetrieveMetadataService
    {
        private readonly IDicomInstanceStore _dicomInstanceStore;
        private readonly IDicomMetadataStore _dicomMetadataStore;

        public DicomRetrieveMetadataService(IDicomInstanceStore dicomInstanceStore, IDicomMetadataStore dicomMetadataStore)
        {
            EnsureArg.IsNotNull(dicomInstanceStore, nameof(dicomInstanceStore));
            EnsureArg.IsNotNull(dicomMetadataStore, nameof(dicomMetadataStore));

            _dicomInstanceStore = dicomInstanceStore;
            _dicomMetadataStore = dicomMetadataStore;
        }

        public async Task<IEnumerable<DicomDataset>> GetDicomInstanceMetadataAsync(
            ResourceType resourceType,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            CancellationToken cancellationToken)
        {
            IEnumerable<DicomInstanceIdentifier> retrieveInstances = await GetInstancesToRetrieve(
                resourceType,
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid,
                cancellationToken);

            return await Task.WhenAll(
                retrieveInstances.Select(x => _dicomMetadataStore.GetInstanceMetadataAsync(x, cancellationToken)));
        }

        private async Task<IEnumerable<DicomInstanceIdentifier>> GetInstancesToRetrieve(
            ResourceType resourceType,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            CancellationToken cancellationToken)
        {
            IEnumerable<DicomInstanceIdentifier> instancesToRetrieve = Enumerable.Empty<DicomInstanceIdentifier>();
            switch (resourceType)
            {
                case ResourceType.Frames:
                case ResourceType.Instance:
                    instancesToRetrieve = await _dicomInstanceStore.GetInstanceIdentifierAsync(
                        studyInstanceUid,
                        seriesInstanceUid,
                        sopInstanceUid,
                        cancellationToken);
                    break;
                case ResourceType.Series:
                    instancesToRetrieve = await _dicomInstanceStore.GetInstanceIdentifiersInSeriesAsync(
                        studyInstanceUid,
                        seriesInstanceUid,
                        cancellationToken);
                    break;
                case ResourceType.Study:
                    instancesToRetrieve = await _dicomInstanceStore.GetInstanceIdentifiersInStudyAsync(
                        studyInstanceUid,
                        cancellationToken);
                    break;
                default:
                    Debug.Fail($"Unknown retrieve transaction type: {resourceType}", nameof(resourceType));
                    break;
            }

            if (!instancesToRetrieve.Any())
            {
                throw new DicomInstanceNotFoundException();
            }

            return instancesToRetrieve;
        }
    }
}
