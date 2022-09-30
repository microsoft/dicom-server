// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Benchmark.Retrieve;

public class LegacyRetrieveMetadataService
{
    private readonly IInstanceStore _instanceStore;
    private readonly IMetadataStore _metadataStore;
    private readonly IETagGenerator _eTagGenerator;
    private readonly IDicomRequestContextAccessor _contextAccessor;

    public LegacyRetrieveMetadataService(
        IInstanceStore instanceStore,
        IMetadataStore metadataStore,
        IETagGenerator eTagGenerator,
        IDicomRequestContextAccessor contextAccessor)
    {
        _instanceStore = EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _eTagGenerator = EnsureArg.IsNotNull(eTagGenerator, nameof(eTagGenerator));
        _contextAccessor = EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));
    }

    public async Task<LegacyRetrieveMetadataResponse> RetrieveStudyInstanceMetadataAsync(string studyInstanceUid, string ifNoneMatch = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<VersionedInstanceIdentifier> retrieveInstances = await _instanceStore.GetInstancesToRetrieve(
            ResourceType.Study,
            GetPartitionKey(),
            studyInstanceUid,
            seriesInstanceUid: null,
            sopInstanceUid: null,
            cancellationToken);

        string eTag = _eTagGenerator.GetETag(ResourceType.Study, retrieveInstances);
        bool isCacheValid = IsCacheValid(eTag, ifNoneMatch);
        return await RetrieveOldMetadata(retrieveInstances, isCacheValid, eTag, cancellationToken);
    }

    private async Task<LegacyRetrieveMetadataResponse> RetrieveOldMetadata(IEnumerable<VersionedInstanceIdentifier> instancesToRetrieve, bool isCacheValid, string eTag, CancellationToken cancellationToken)
    {
        IEnumerable<DicomDataset> instanceMetadata = Enumerable.Empty<DicomDataset>();
        _contextAccessor.RequestContext.PartCount = instancesToRetrieve.Count();

        // Retrieve metadata instances only if cache is not valid.
        if (!isCacheValid)
        {
            instanceMetadata = await Task.WhenAll(
                    instancesToRetrieve
                    .Select(x => _metadataStore.GetInstanceMetadataAsync(x, cancellationToken)));
        }

        return new LegacyRetrieveMetadataResponse(instanceMetadata, isCacheValid, eTag);
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
        => !string.IsNullOrEmpty(ifNoneMatch) && string.Equals(ifNoneMatch, eTag, StringComparison.OrdinalIgnoreCase);

    private int GetPartitionKey()
        => _contextAccessor.RequestContext.GetPartitionKey();
}
