// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Messages.ChangeFeed;
using Microsoft.Health.Dicom.Core.Messages.Delete;
using Microsoft.Health.Dicom.Core.Messages.Query;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    public static class DicomMediatorExtensions
    {
        public static Task<StoreResponse> StoreDicomResourcesAsync(
            this IMediator mediator, Stream requestBody, string requestContentType, string studyInstanceUid, CancellationToken cancellationToken)
        {
            return mediator.Send(new StoreRequest(requestBody, requestContentType, studyInstanceUid), cancellationToken);
        }

        public static Task<RetrieveResourceResponse> RetrieveDicomStudyAsync(
            this IMediator mediator, string studyInstanceUid, string requestedTransferSyntax, CancellationToken cancellationToken)
        {
            return mediator.Send(
                new RetrieveResourceRequest(requestedTransferSyntax, studyInstanceUid),
                cancellationToken);
        }

        public static Task<RetrieveMetadataResponse> RetrieveDicomStudyMetadataAsync(
            this IMediator mediator, string studyInstanceUid, CancellationToken cancellationToken, string ifNoneMatch = null)
        {
            return mediator.Send(new RetrieveMetadataRequest(studyInstanceUid, ifNoneMatch), cancellationToken);
        }

        public static Task<RetrieveResourceResponse> RetrieveDicomSeriesAsync(
            this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string requestedTransferSyntax, CancellationToken cancellationToken)
        {
            return mediator.Send(
                new RetrieveResourceRequest(requestedTransferSyntax, studyInstanceUid, seriesInstanceUid),
                cancellationToken);
        }

        public static Task<RetrieveMetadataResponse> RetrieveDicomSeriesMetadataAsync(
           this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken, string ifNoneMatch = null)
        {
            return mediator.Send(new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, ifNoneMatch), cancellationToken);
        }

        public static Task<RetrieveResourceResponse> RetrieveDicomInstanceAsync(
            this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string requestedTransferSyntax, CancellationToken cancellationToken)
        {
            return mediator.Send(
                new RetrieveResourceRequest(requestedTransferSyntax, studyInstanceUid, seriesInstanceUid, sopInstanceUid),
                cancellationToken);
        }

        public static Task<RetrieveMetadataResponse> RetrieveDicomInstanceMetadataAsync(
            this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken, string ifNoneMatch = null)
        {
            return mediator.Send(new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ifNoneMatch), cancellationToken);
        }

        public static Task<RetrieveResourceResponse> RetrieveDicomFramesAsync(
            this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, int[] frames, string requestedTransferSyntax, CancellationToken cancellationToken)
        {
            return mediator.Send(
                new RetrieveResourceRequest(requestedTransferSyntax, studyInstanceUid, seriesInstanceUid, sopInstanceUid, frames),
                cancellationToken);
        }

        public static Task<DeleteResourcesResponse> DeleteDicomStudyAsync(
            this IMediator mediator, string studyInstanceUid, CancellationToken cancellationToken = default)
        {
            return mediator.Send(new DeleteResourcesRequest(studyInstanceUid), cancellationToken);
        }

        public static Task<DeleteResourcesResponse> DeleteDicomSeriesAsync(
            this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken = default)
        {
            return mediator.Send(new DeleteResourcesRequest(studyInstanceUid, seriesInstanceUid), cancellationToken);
        }

        public static Task<DeleteResourcesResponse> DeleteDicomInstanceAsync(
            this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken = default)
        {
            return mediator.Send(new DeleteResourcesRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid), cancellationToken);
        }

        public static Task<QueryResourceResponse> QueryDicomResourcesAsync(
            this IMediator mediator,
            IEnumerable<KeyValuePair<string, StringValues>> requestQuery,
            QueryResource resourceType,
            string studyInstanceUid = null,
            string seriesInstanceUid = null,
            CancellationToken cancellationToken = default)
        {
            return mediator.Send(new QueryResourceRequest(requestQuery, resourceType, studyInstanceUid, seriesInstanceUid), cancellationToken);
        }

        public static Task<ChangeFeedResponse> GetChangeFeed(
            this IMediator mediator,
            long offset,
            int limit,
            bool includeMetadata,
            CancellationToken cancellationToken = default)
        {
            return mediator.Send(new ChangeFeedRequest(offset, limit, includeMetadata), cancellationToken);
        }

        public static Task<ChangeFeedLatestResponse> GetChangeFeedLatest(
            this IMediator mediator,
            bool includeMetadata,
            CancellationToken cancellationToken = default)
        {
            return mediator.Send(new ChangeFeedLatestRequest(includeMetadata), cancellationToken);
        }
    }
}
