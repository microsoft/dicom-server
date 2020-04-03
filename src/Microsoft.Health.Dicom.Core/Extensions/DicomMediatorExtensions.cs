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
using Microsoft.Health.Dicom.Core.Messages.Delete;
using Microsoft.Health.Dicom.Core.Messages.Query;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    public static class DicomMediatorExtensions
    {
        public static Task<StoreDicomResponse> StoreDicomResourcesAsync(
            this IMediator mediator, Stream requestBody, string requestContentType, string studyInstanceUid, CancellationToken cancellationToken)
        {
            return mediator.Send(new StoreDicomRequest(requestBody, requestContentType, studyInstanceUid), cancellationToken);
        }

        public static Task<RetrieveDicomResourceResponse> RetrieveDicomStudyAsync(
            this IMediator mediator, string studyInstanceUid, string requestedTransferSyntax, CancellationToken cancellationToken)
        {
            return mediator.Send(
                new RetrieveDicomResourceRequest(requestedTransferSyntax, studyInstanceUid),
                cancellationToken);
        }

        public static Task<RetrieveDicomMetadataResponse> RetrieveDicomStudyMetadataAsync(
            this IMediator mediator, string studyInstanceUid, CancellationToken cancellationToken)
        {
            return mediator.Send(new RetrieveDicomMetadataRequest(studyInstanceUid), cancellationToken);
        }

        public static Task<RetrieveDicomResourceResponse> RetrieveDicomSeriesAsync(
            this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string requestedTransferSyntax, CancellationToken cancellationToken)
        {
            return mediator.Send(
                new RetrieveDicomResourceRequest(requestedTransferSyntax, studyInstanceUid, seriesInstanceUid),
                cancellationToken);
        }

        public static Task<RetrieveDicomMetadataResponse> RetrieveDicomSeriesMetadataAsync(
            this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken)
        {
            return mediator.Send(new RetrieveDicomMetadataRequest(studyInstanceUid, seriesInstanceUid), cancellationToken);
        }

        public static Task<RetrieveDicomResourceResponse> RetrieveDicomInstanceAsync(
            this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string requestedTransferSyntax, CancellationToken cancellationToken)
        {
            return mediator.Send(
                new RetrieveDicomResourceRequest(requestedTransferSyntax, studyInstanceUid, seriesInstanceUid, sopInstanceUid),
                cancellationToken);
        }

        public static Task<RetrieveDicomMetadataResponse> RetrieveDicomInstanceMetadataAsync(
            this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken)
        {
            return mediator.Send(new RetrieveDicomMetadataRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid), cancellationToken);
        }

        public static Task<RetrieveDicomResourceResponse> RetrieveDicomFramesAsync(
            this IMediator mediator, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, int[] frames, string requestedTransferSyntax, CancellationToken cancellationToken)
        {
            return mediator.Send(
                new RetrieveDicomResourceRequest(requestedTransferSyntax, studyInstanceUid, seriesInstanceUid, sopInstanceUid, frames),
                cancellationToken);
        }

        public static Task<DeleteDicomResourcesResponse> DeleteDicomResourcesAsync(
            this IMediator mediator, string studyInstanceUid, CancellationToken cancellationToken = default)
        {
            return mediator.Send(new DeleteDicomResourcesRequest(studyInstanceUid), cancellationToken);
        }

        public static Task<DeleteDicomResourcesResponse> DeleteDicomResourcesAsync(
            this IMediator mediator, string studyInstanceUid, string seriesUid, CancellationToken cancellationToken = default)
        {
            return mediator.Send(new DeleteDicomResourcesRequest(studyInstanceUid, seriesUid), cancellationToken);
        }

        public static Task<DeleteDicomResourcesResponse> DeleteDicomResourcesAsync(
            this IMediator mediator, string studyInstanceUid, string seriesid, string sopInstanceUid, CancellationToken cancellationToken = default)
        {
            return mediator.Send(new DeleteDicomResourcesRequest(studyInstanceUid, seriesid, sopInstanceUid), cancellationToken);
        }

        public static Task<DicomQueryResourceResponse> QueryDicomResourcesAsync(
            this IMediator mediator,
            IEnumerable<KeyValuePair<string, StringValues>> requestQuery,
            QueryResource resourceType,
            string studyInstanceUid = null,
            string seriesInstanceUid = null,
            CancellationToken cancellationToken = default)
        {
            return mediator.Send(new DicomQueryResourceRequest(requestQuery, resourceType, studyInstanceUid, seriesInstanceUid), cancellationToken);
        }
    }
}
