// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class RetrieveMetadataService : IRetrieveMetadataService
    {
        private readonly IInstanceStore _instanceStore;
        private readonly IMetadataStore _metadataStore;

        public RetrieveMetadataService(
            IInstanceStore instanceStore,
            IMetadataStore metadataStore)
        {
            EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
            EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));

            _instanceStore = instanceStore;
            _metadataStore = metadataStore;
        }

        public async Task<RetrieveMetadataResponse> RetrieveStudyInstanceMetadataAsync(string studyInstanceUid, CancellationToken cancellationToken = default)
        {
            IEnumerable<VersionedInstanceIdentifier> retrieveInstances = await _instanceStore.GetInstancesToRetrieve(
                ResourceType.Study,
                studyInstanceUid,
                seriesInstanceUid: null,
                sopInstanceUid: null,
                cancellationToken);

            return await RetrieveMetadata(retrieveInstances, cancellationToken);
        }

        public async Task<RetrieveMetadataResponse> RetrieveSeriesInstanceMetadataAsync(string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken = default)
        {
            IEnumerable<VersionedInstanceIdentifier> retrieveInstances = await _instanceStore.GetInstancesToRetrieve(
                 ResourceType.Series,
                 studyInstanceUid,
                 seriesInstanceUid,
                 sopInstanceUid: null,
                 cancellationToken);

            return await RetrieveMetadata(retrieveInstances, cancellationToken);
        }

        public async Task<RetrieveMetadataResponse> RetrieveSopInstanceMetadataAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken = default)
        {
            IEnumerable<VersionedInstanceIdentifier> retrieveInstances = await _instanceStore.GetInstancesToRetrieve(
                 ResourceType.Instance,
                 studyInstanceUid,
                 seriesInstanceUid,
                 sopInstanceUid,
                 cancellationToken);

            return await RetrieveMetadata(retrieveInstances, cancellationToken);
        }

        private async Task<RetrieveMetadataResponse> RetrieveMetadata(IEnumerable<VersionedInstanceIdentifier> instancesToRetrieve, CancellationToken cancellationToken)
        {
            IEnumerable<DicomDataset> instanceMetadata = await Task.WhenAll(
                        instancesToRetrieve
                        .Select(x => _metadataStore.GetInstanceMetadataAsync(x, cancellationToken)));

            return new RetrieveMetadataResponse(instanceMetadata);
        }
    }
}
