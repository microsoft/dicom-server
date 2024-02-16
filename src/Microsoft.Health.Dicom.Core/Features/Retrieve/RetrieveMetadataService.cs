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
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;

public class RetrieveMetadataService : IRetrieveMetadataService
{
    private readonly IInstanceStore _instanceStore;
    private readonly IMetadataStore _metadataStore;
    private readonly IETagGenerator _eTagGenerator;
    private readonly IDicomRequestContextAccessor _contextAccessor;
    private readonly RetrieveConfiguration _options;
    private readonly RetrieveMeter _retrieveMeter;

    public RetrieveMetadataService(
        IInstanceStore instanceStore,
        IMetadataStore metadataStore,
        IETagGenerator eTagGenerator,
        IDicomRequestContextAccessor contextAccessor,
        RetrieveMeter retrieveMeter,
        IOptions<RetrieveConfiguration> options)
    {
        _instanceStore = EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _eTagGenerator = EnsureArg.IsNotNull(eTagGenerator, nameof(eTagGenerator));
        _contextAccessor = EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));
        _retrieveMeter = EnsureArg.IsNotNull(retrieveMeter, nameof(retrieveMeter));
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
    }

    public async Task<RetrieveMetadataResponse> RetrieveStudyInstanceMetadataAsync(string studyInstanceUid, string ifNoneMatch = null, bool isOriginalVersionRequested = false, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<InstanceMetadata> retrieveInstances = await _instanceStore.GetInstancesWithProperties(
            ResourceType.Study,
            GetPartition(),
            studyInstanceUid,
            seriesInstanceUid: null,
            sopInstanceUid: null,
            isOriginalVersionRequested,
            cancellationToken);

        string eTag = _eTagGenerator.GetETag(ResourceType.Study, retrieveInstances);
        bool isCacheValid = IsCacheValid(eTag, ifNoneMatch);
        return RetrieveMetadata(retrieveInstances, isCacheValid, eTag, isOriginalVersionRequested, cancellationToken);
    }

    public async Task<RetrieveMetadataResponse> RetrieveSeriesInstanceMetadataAsync(string studyInstanceUid, string seriesInstanceUid, string ifNoneMatch = null, bool isOriginalVersionRequested = false, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<InstanceMetadata> retrieveInstances = await _instanceStore.GetInstancesWithProperties(
                ResourceType.Series,
                GetPartition(),
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid: null,
                isOriginalVersionRequested,
                cancellationToken);

        string eTag = _eTagGenerator.GetETag(ResourceType.Series, retrieveInstances);
        bool isCacheValid = IsCacheValid(eTag, ifNoneMatch);
        return RetrieveMetadata(retrieveInstances, isCacheValid, eTag, isOriginalVersionRequested, cancellationToken);
    }

    public async Task<RetrieveMetadataResponse> RetrieveSopInstanceMetadataAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string ifNoneMatch = null, bool isOriginalVersionRequested = false, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<InstanceMetadata> retrieveInstances = await _instanceStore.GetInstancesWithProperties(
            ResourceType.Instance,
            GetPartition(),
            studyInstanceUid,
            seriesInstanceUid,
            sopInstanceUid,
            isOriginalVersionRequested,
            cancellationToken);

        string eTag = _eTagGenerator.GetETag(ResourceType.Instance, retrieveInstances);
        bool isCacheValid = IsCacheValid(eTag, ifNoneMatch);
        return RetrieveMetadata(retrieveInstances, isCacheValid, eTag, isOriginalVersionRequested, cancellationToken);
    }

    private RetrieveMetadataResponse RetrieveMetadata(IReadOnlyList<InstanceMetadata> instancesToRetrieve, bool isCacheValid, string eTag, bool isOriginalVersionRequested, CancellationToken cancellationToken)
    {
        _contextAccessor.RequestContext.PartCount = instancesToRetrieve.Count;
        _retrieveMeter.RetrieveInstanceMetadataCount.Add(instancesToRetrieve.Count);

        // Retrieve metadata instances only if cache is not valid.
        IAsyncEnumerable<DicomDataset> instanceMetadata = isCacheValid
            ? AsyncEnumerable.Empty<DicomDataset>()
            : instancesToRetrieve.SelectParallel(
                (x, t) => new ValueTask<DicomDataset>(
                    _metadataStore.GetInstanceMetadataAsync(
                        x.GetVersion(isOriginalVersionRequested), t)),
                new ParallelEnumerationOptions { MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism },
                cancellationToken);

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
        => !string.IsNullOrEmpty(ifNoneMatch) && string.Equals(ifNoneMatch, eTag, StringComparison.OrdinalIgnoreCase);

    private Partition GetPartition()
        => _contextAccessor.RequestContext.GetPartition();
}
