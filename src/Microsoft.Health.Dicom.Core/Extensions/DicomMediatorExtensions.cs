// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Messages.ChangeFeed;
using Microsoft.Health.Dicom.Core.Messages.Delete;
using Microsoft.Health.Dicom.Core.Messages.Export;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Messages.Partition;
using Microsoft.Health.Dicom.Core.Messages.Query;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Store;
using Microsoft.Health.Dicom.Core.Messages.Workitem;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Extensions;

public static class DicomMediatorExtensions
{
    public static Task<StoreResponse> StoreDicomResourcesAsync(
        this IMediator mediator, Stream requestBody, string requestContentType, string studyInstanceUid, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new StoreRequest(requestBody, requestContentType, studyInstanceUid), cancellationToken);
    }

    public static Task<RetrieveResourceResponse> RetrieveDicomStudyAsync(
        this IMediator mediator, string studyInstanceUid, IEnumerable<AcceptHeader> acceptHeaders, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(
            new RetrieveResourceRequest(studyInstanceUid, acceptHeaders),
            cancellationToken);
    }

    public static Task<RetrieveMetadataResponse> RetrieveDicomStudyMetadataAsync(
        this IMediator mediator, string studyInstanceUid, string ifNoneMatch, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new RetrieveMetadataRequest(studyInstanceUid, ifNoneMatch), cancellationToken);
    }

    public static Task<RetrieveResourceResponse> RetrieveDicomSeriesAsync(
        this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, IEnumerable<AcceptHeader> acceptHeaders, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(
            new RetrieveResourceRequest(studyInstanceUid, seriesInstanceUid, acceptHeaders),
            cancellationToken);
    }

    public static Task<RetrieveMetadataResponse> RetrieveDicomSeriesMetadataAsync(
       this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string ifNoneMatch, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, ifNoneMatch), cancellationToken);
    }

    public static Task<RetrieveResourceResponse> RetrieveDicomInstanceAsync(
        this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, IEnumerable<AcceptHeader> acceptHeaders, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(
            new RetrieveResourceRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, acceptHeaders),
            cancellationToken);
    }

    public static Task<RetrieveMetadataResponse> RetrieveDicomInstanceMetadataAsync(
        this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string ifNoneMatch, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ifNoneMatch), cancellationToken);
    }

    public static Task<RetrieveResourceResponse> RetrieveDicomFramesAsync(
        this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, int[] frames, IEnumerable<AcceptHeader> acceptHeaders, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(
            new RetrieveResourceRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, frames, acceptHeaders),
            cancellationToken);
    }

    public static Task<DeleteResourcesResponse> DeleteDicomStudyAsync(
        this IMediator mediator, string studyInstanceUid, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new DeleteResourcesRequest(studyInstanceUid), cancellationToken);
    }

    public static Task<DeleteResourcesResponse> DeleteDicomSeriesAsync(
        this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new DeleteResourcesRequest(studyInstanceUid, seriesInstanceUid), cancellationToken);
    }

    public static Task<DeleteResourcesResponse> DeleteDicomInstanceAsync(
        this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new DeleteResourcesRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid), cancellationToken);
    }

    public static Task<QueryResourceResponse> QueryDicomResourcesAsync(
        this IMediator mediator,
        QueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        EnsureArg.IsNotNull(parameters, nameof(parameters));
        return mediator.Send(new QueryResourceRequest(parameters), cancellationToken);
    }

    public static Task<ChangeFeedResponse> GetChangeFeed(
        this IMediator mediator,
        long offset,
        int limit,
        bool includeMetadata,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new ChangeFeedRequest(offset, limit, includeMetadata), cancellationToken);
    }

    public static Task<ChangeFeedLatestResponse> GetChangeFeedLatest(
        this IMediator mediator,
        bool includeMetadata,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new ChangeFeedLatestRequest(includeMetadata), cancellationToken);
    }

    public static Task<AddExtendedQueryTagResponse> AddExtendedQueryTagsAsync(
        this IMediator mediator, IEnumerable<AddExtendedQueryTagEntry> extendedQueryTags, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new AddExtendedQueryTagRequest(extendedQueryTags), cancellationToken);
    }

    public static Task<ExportIdentifiersResponse> ExportIdentifiersAsync(
      this IMediator mediator, ExportIdentifiersInput input, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new ExportIdentifiersRequest(input), cancellationToken);
    }

    public static Task<DeleteExtendedQueryTagResponse> DeleteExtendedQueryTagAsync(
       this IMediator mediator, string tagPath, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new DeleteExtendedQueryTagRequest(tagPath), cancellationToken);
    }

    public static Task<GetExtendedQueryTagsResponse> GetExtendedQueryTagsAsync(
        this IMediator mediator, int limit, int offset, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new GetExtendedQueryTagsRequest(limit, offset), cancellationToken);
    }

    public static Task<GetExtendedQueryTagResponse> GetExtendedQueryTagAsync(
        this IMediator mediator, string extendedQueryTagPath, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new GetExtendedQueryTagRequest(extendedQueryTagPath), cancellationToken);
    }

    public static Task<GetExtendedQueryTagErrorsResponse> GetExtendedQueryTagErrorsAsync(
        this IMediator mediator, string extendedQueryTagPath, int limit, int offset, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new GetExtendedQueryTagErrorsRequest(extendedQueryTagPath, limit, offset), cancellationToken);
    }

    public static Task<UpdateExtendedQueryTagResponse> UpdateExtendedQueryTagAsync(
        this IMediator mediator, string tagPath, UpdateExtendedQueryTagEntry newValue, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new UpdateExtendedQueryTagRequest(tagPath, newValue), cancellationToken);
    }

    public static Task<OperationStateResponse> GetOperationStateAsync(
       this IMediator mediator,
       Guid operationId,
       CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new OperationStateRequest(operationId), cancellationToken);
    }

    public static Task<GetPartitionResponse> GetPartitionAsync(
       this IMediator mediator,
       string partitionName,
       CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new GetPartitionRequest(partitionName), cancellationToken);
    }

    public static Task<AddPartitionResponse> AddPartitionAsync(
       this IMediator mediator,
       string partitionName,
       CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new AddPartitionRequest(partitionName), cancellationToken);
    }

    public static Task<GetPartitionsResponse> GetPartitionsAsync(
       this IMediator mediator,
       CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new GetPartitionsRequest(), cancellationToken);
    }

    public static Task<AddWorkitemResponse> AddWorkitemAsync(
        this IMediator mediator, Stream requestBody, string requestContentType, string workitemInstanceUid, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new AddWorkitemRequest(requestBody, requestContentType, workitemInstanceUid), cancellationToken);
    }

    public static Task<CancelWorkitemResponse> CancelWorkitemAsync(
        this IMediator mediator, Stream requestBody, string requestContentType, string workitemUid, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new CancelWorkitemRequest(requestBody, requestContentType, workitemUid), cancellationToken);
    }

    public static Task<QueryWorkitemResourceResponse> QueryWorkitemsAsync(
        this IMediator mediator,
        BaseQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        EnsureArg.IsNotNull(parameters, nameof(parameters));
        return mediator.Send(new QueryWorkitemResourceRequest(parameters), cancellationToken);
    }
}
