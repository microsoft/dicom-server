// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class RetrieveMetadataService : IRetrieveMetadataService
    {
        private readonly IInstanceStore _instanceStore;
        private readonly IMetadataStore _metadataStore;
        private readonly IETagGenerator _eTagGenerator;
        private readonly IDicomRequestContextAccessor _contextAccessor;

        public RetrieveMetadataService(
            IInstanceStore instanceStore,
            IMetadataStore metadataStore,
            IETagGenerator eTagGenerator,
            IDicomRequestContextAccessor contextAccessor)
        {
            EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
            EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
            EnsureArg.IsNotNull(eTagGenerator, nameof(eTagGenerator));
            EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));

            _instanceStore = instanceStore;
            _metadataStore = metadataStore;
            _eTagGenerator = eTagGenerator;
            _contextAccessor = contextAccessor;
        }

        public async Task<RetrieveMetadataResponse> RetrieveStudyInstanceMetadataAsync(string studyInstanceUid, string ifNoneMatch = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<VersionedInstanceIdentifier> retrieveInstances = await _instanceStore.GetInstancesToRetrieve(
                ResourceType.Study,
                GetPartitionKey(),
                studyInstanceUid,
                seriesInstanceUid: null,
                sopInstanceUid: null,
                cancellationToken);

            string eTag = _eTagGenerator.GetETag(ResourceType.Study, retrieveInstances);
            bool isCacheValid = IsCacheValid(eTag, ifNoneMatch);
            return await RetrieveMetadata(retrieveInstances, isCacheValid, eTag, cancellationToken);
        }

        public async Task<RetrieveMetadataResponse> RetrieveSeriesInstanceMetadataAsync(string studyInstanceUid, string seriesInstanceUid, string ifNoneMatch = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<VersionedInstanceIdentifier> retrieveInstances = await _instanceStore.GetInstancesToRetrieve(
                    ResourceType.Series,
                    GetPartitionKey(),
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid: null,
                    cancellationToken);

            string eTag = _eTagGenerator.GetETag(ResourceType.Series, retrieveInstances);
            bool isCacheValid = IsCacheValid(eTag, ifNoneMatch);
            return await RetrieveMetadata(retrieveInstances, isCacheValid, eTag, cancellationToken);
        }

        public async Task<RetrieveMetadataResponse> RetrieveSopInstanceMetadataAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string ifNoneMatch = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<VersionedInstanceIdentifier> retrieveInstances = await _instanceStore.GetInstancesToRetrieve(
                ResourceType.Instance,
                GetPartitionKey(),
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid,
                cancellationToken);

            string eTag = _eTagGenerator.GetETag(ResourceType.Instance, retrieveInstances);
            bool isCacheValid = IsCacheValid(eTag, ifNoneMatch);
            return await RetrieveMetadata(retrieveInstances, isCacheValid, eTag, cancellationToken);
        }

        private async Task<RetrieveMetadataResponse> RetrieveMetadata(IEnumerable<VersionedInstanceIdentifier> instancesToRetrieve, bool isCacheValid, string eTag, CancellationToken cancellationToken)
        {
            IEnumerable<DicomDataset> instanceMetadata = Enumerable.Empty<DicomDataset>();

            // Retrieve metadata instances only if cache is not valid.
            if (!isCacheValid)
            {
                instanceMetadata = await Task.WhenAll(
                        instancesToRetrieve
                        .Select(x => _metadataStore.GetInstanceMetadataAsync(x, cancellationToken)));
            }

            return new RetrieveMetadataResponse(instanceMetadata, isCacheValid, eTag);
        }

        /// <summary>
        /// Check if cache is valid.
        /// Cache is regarded as valid if the following criteria passes:
        ///     1. User has passed If-None-Match in the header.
        ///     2. Calculated ETag is equals to the If-None-Match header field.
        /// </summary>
        /// <param name="eTag">ETag.</param>
        /// <param name="ifNoneMatch">If-None-Match</param>
        /// <returns>True if cache is valid, i.e. content has not modified, else returns false.</returns>
        private static bool IsCacheValid(string eTag, string ifNoneMatch)
        {
            bool isCacheValid = false;

            if (!string.IsNullOrEmpty(ifNoneMatch) && string.Equals(ifNoneMatch, eTag, StringComparison.OrdinalIgnoreCase))
            {
                isCacheValid = true;
            }

            return isCacheValid;
        }

        private int GetPartitionKey()
        {
            var partitionKey = _contextAccessor.RequestContext?.DataPartitionEntry.PartitionKey;
            EnsureArg.IsTrue(partitionKey.HasValue, nameof(partitionKey));
            return partitionKey.Value;
        }
    }
}
