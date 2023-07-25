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
using FellowOakDicom;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Messages.ChangeFeed;
using Microsoft.Health.Dicom.Core.Messages.Delete;
using Microsoft.Health.Dicom.Core.Messages.Export;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Messages.Partitioning;
using Microsoft.Health.Dicom.Core.Messages.Query;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Store;
using Microsoft.Health.Dicom.Core.Messages.Update;
using Microsoft.Health.Dicom.Core.Messages.Workitem;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Core.Models.Update;
using ResourceType = Microsoft.Health.Dicom.Core.Messages.ResourceType;

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
        this IMediator mediator, string studyInstanceUid, IReadOnlyCollection<AcceptHeader> acceptHeaders, bool isOriginalVersionRequested, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(
            new RetrieveResourceRequest(studyInstanceUid, acceptHeaders, isOriginalVersionRequested),
            cancellationToken);
    }

    public static Task<RetrieveMetadataResponse> RetrieveDicomStudyMetadataAsync(
        this IMediator mediator, string studyInstanceUid, string ifNoneMatch, bool isOriginalVersionRequested, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new RetrieveMetadataRequest(studyInstanceUid, ifNoneMatch, isOriginalVersionRequested), cancellationToken);
    }

    public static Task<RetrieveResourceResponse> RetrieveDicomSeriesAsync(
        this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, IReadOnlyCollection<AcceptHeader> acceptHeaders, bool isOriginalVersionRequested, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(
            new RetrieveResourceRequest(studyInstanceUid, seriesInstanceUid, acceptHeaders, isOriginalVersionRequested),
            cancellationToken);
    }

    public static Task<RetrieveMetadataResponse> RetrieveDicomSeriesMetadataAsync(
       this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string ifNoneMatch, bool isOriginalVersionRequested, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, ifNoneMatch, isOriginalVersionRequested), cancellationToken);
    }

    public static Task<RetrieveResourceResponse> RetrieveDicomInstanceAsync(
        this IMediator mediator,
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        IReadOnlyCollection<AcceptHeader> acceptHeaders,
        bool isOriginalVersionRequested,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(
            new RetrieveResourceRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, acceptHeaders, isOriginalVersionRequested),
            cancellationToken);
    }

    public static Task<RetrieveRenderedResponse> RetrieveRenderedDicomInstanceAsync(
        this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, ResourceType resourceType, IReadOnlyCollection<AcceptHeader> acceptHeaders, int quality, CancellationToken cancellationToken, int frameNumber = 1)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(
            new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, resourceType, frameNumber, quality, acceptHeaders),
            cancellationToken);
    }

    public static Task<RetrieveMetadataResponse> RetrieveDicomInstanceMetadataAsync(
        this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string ifNoneMatch, bool isOriginalVersionRequested, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ifNoneMatch, isOriginalVersionRequested), cancellationToken);
    }

    public static Task<RetrieveResourceResponse> RetrieveDicomFramesAsync(
        this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, int[] frames, IReadOnlyCollection<AcceptHeader> acceptHeaders, CancellationToken cancellationToken)
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
        TimeRange range,
        long offset,
        int limit,
        ChangeFeedOrder order,
        bool includeMetadata,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new ChangeFeedRequest(range, offset, limit, order, includeMetadata), cancellationToken);
    }

    public static Task<ChangeFeedLatestResponse> GetChangeFeedLatest(
        this IMediator mediator,
        ChangeFeedOrder order,
        bool includeMetadata,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new ChangeFeedLatestRequest(order, includeMetadata), cancellationToken);
    }

    public static Task<AddExtendedQueryTagResponse> AddExtendedQueryTagsAsync(
        this IMediator mediator, IEnumerable<AddExtendedQueryTagEntry> extendedQueryTags, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new AddExtendedQueryTagRequest(extendedQueryTags), cancellationToken);
    }

    public static Task<DeleteExtendedQueryTagResponse> DeleteExtendedQueryTagAsync(
       this IMediator mediator, string tagPath, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new DeleteExtendedQueryTagRequest(tagPath), cancellationToken);
    }

    public static Task<GetExtendedQueryTagsResponse> GetExtendedQueryTagsAsync(
        this IMediator mediator, int limit, long offset, CancellationToken cancellationToken)
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
        this IMediator mediator, string extendedQueryTagPath, int limit, long offset, CancellationToken cancellationToken)
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

    public static Task<ExportResponse> ExportAsync(
       this IMediator mediator,
       ExportSpecification spec,
       CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new ExportRequest(spec), cancellationToken);
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
        this IMediator mediator, DicomDataset dicomDataSet, string requestContentType, string workitemInstanceUid, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));

        return mediator.Send(new AddWorkitemRequest(dicomDataSet, requestContentType, workitemInstanceUid), cancellationToken);
    }

    public static Task<CancelWorkitemResponse> CancelWorkitemAsync(
        this IMediator mediator, DicomDataset dicomDataSet, string requestContentType, string workitemUid, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));

        return mediator.Send(new CancelWorkitemRequest(dicomDataSet, requestContentType, workitemUid), cancellationToken);
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

    public static Task<ChangeWorkitemStateResponse> ChangeWorkitemStateAsync(
        this IMediator mediator,
        DicomDataset dicomDataSet,
        string requestContentType,
        string workitemUid,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        EnsureArg.IsNotEmptyOrWhiteSpace(workitemUid, nameof(workitemUid));

        return mediator.Send(
            new ChangeWorkitemStateRequest(dicomDataSet, requestContentType, workitemUid), cancellationToken);
    }

    public static Task<RetrieveWorkitemResponse> RetrieveWorkitemAsync(
        this IMediator mediator,
        string workitemInstanceUid,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        EnsureArg.IsNotEmptyOrWhiteSpace(workitemInstanceUid, nameof(workitemInstanceUid));

        return mediator.Send(new RetrieveWorkitemRequest(workitemInstanceUid), cancellationToken);
    }

    public static Task<UpdateWorkitemResponse> UpdateWorkitemAsync(
        this IMediator mediator,
        DicomDataset dicomDataset,
        string requestContentType,
        string workitemInstanceUid,
        string transactionUid = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        EnsureArg.IsNotEmptyOrWhiteSpace(workitemInstanceUid, nameof(workitemInstanceUid));

        // Not validating transaction Uid as it can be null if the procedure step state is in SCHEDULED state.
        return mediator.Send(new UpdateWorkitemRequest(dicomDataset, requestContentType, workitemInstanceUid, transactionUid), cancellationToken);
    }

    public static Task<UpdateInstanceResponse> UpdateInstanceAsync(
       this IMediator mediator,
       UpdateSpecification updateSpecification,
       CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        return mediator.Send(new UpdateInstanceRequest(updateSpecification), cancellationToken);
    }
}
