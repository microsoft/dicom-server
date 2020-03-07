// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Delete;
using Microsoft.Health.Dicom.Core.Messages.Query;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    public static class DicomMediatorExtensions
    {
        public static Task<StoreDicomResourcesResponse> StoreDicomResourcesAsync(
            this IMediator mediator, Uri requestBaseUri, Stream requestBody, string requestContentType, string studyInstanceUID, CancellationToken cancellationToken)
        {
            return mediator.Send(new StoreDicomResourcesRequest(requestBaseUri, requestBody, requestContentType, studyInstanceUID), cancellationToken);
        }

        public static Task<RetrieveDicomResourceResponse> RetrieveDicomStudyAsync(
            this IMediator mediator, string studyInstanceUID, string requestedTransferSyntax, CancellationToken cancellationToken)
        {
            return mediator.Send(
                new RetrieveDicomResourceRequest(requestedTransferSyntax, studyInstanceUID),
                cancellationToken);
        }

        public static Task<RetrieveDicomMetadataResponse> RetrieveDicomStudyMetadataAsync(
            this IMediator mediator, string studyInstanceUID, CancellationToken cancellationToken)
        {
            return mediator.Send(new RetrieveDicomMetadataRequest(studyInstanceUID), cancellationToken);
        }

        public static Task<RetrieveDicomResourceResponse> RetrieveDicomSeriesAsync(
            this IMediator mediator, string studyInstanceUID, string seriesInstanceUID, string requestedTransferSyntax, CancellationToken cancellationToken)
        {
            return mediator.Send(
                new RetrieveDicomResourceRequest(requestedTransferSyntax, studyInstanceUID, seriesInstanceUID),
                cancellationToken);
        }

        public static Task<RetrieveDicomMetadataResponse> RetrieveDicomSeriesMetadataAsync(
            this IMediator mediator, string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken)
        {
            return mediator.Send(new RetrieveDicomMetadataRequest(studyInstanceUID, seriesInstanceUID), cancellationToken);
        }

        public static Task<RetrieveDicomResourceResponse> RetrieveDicomInstanceAsync(
            this IMediator mediator, string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, string requestedTransferSyntax, CancellationToken cancellationToken)
        {
            return mediator.Send(
                new RetrieveDicomResourceRequest(requestedTransferSyntax, studyInstanceUID, seriesInstanceUID, sopInstanceUID),
                cancellationToken);
        }

        public static Task<RetrieveDicomMetadataResponse> RetrieveDicomInstanceMetadataAsync(
            this IMediator mediator, string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, CancellationToken cancellationToken)
        {
            return mediator.Send(new RetrieveDicomMetadataRequest(studyInstanceUID, seriesInstanceUID, sopInstanceUID), cancellationToken);
        }

        public static Task<RetrieveDicomResourceResponse> RetrieveDicomFramesAsync(
            this IMediator mediator, string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, int[] frames, string requestedTransferSyntax, CancellationToken cancellationToken)
        {
            return mediator.Send(
                new RetrieveDicomResourceRequest(requestedTransferSyntax, studyInstanceUID, seriesInstanceUID, sopInstanceUID, frames),
                cancellationToken);
        }

        public static Task<DeleteDicomResourcesResponse> DeleteDicomResourcesAsync(
            this IMediator mediator, string studyInstanceUID, CancellationToken cancellationToken = default)
        {
            return mediator.Send(new DeleteDicomResourcesRequest(studyInstanceUID), cancellationToken);
        }

        public static Task<DeleteDicomResourcesResponse> DeleteDicomResourcesAsync(
            this IMediator mediator, string studyInstanceUID, string seriesUID, CancellationToken cancellationToken = default)
        {
            return mediator.Send(new DeleteDicomResourcesRequest(studyInstanceUID, seriesUID), cancellationToken);
        }

        public static Task<DeleteDicomResourcesResponse> DeleteDicomResourcesAsync(
            this IMediator mediator, string studyInstanceUID, string seriesUID, string instanceUID, CancellationToken cancellationToken = default)
        {
            return mediator.Send(new DeleteDicomResourcesRequest(studyInstanceUID, seriesUID, instanceUID), cancellationToken);
        }

        public static Task<QueryDicomResourceResponse> QueryDicomResourcesAsync(
            this IMediator mediator,
            IEnumerable<KeyValuePair<string, StringValues>> requestQuery,
            ResourceType resourceType,
            string studyInstanceUID = null,
            string seriesInstanceUID = null,
            CancellationToken cancellationToken = default)
        {
            return mediator.Send(new QueryDicomResourceRequest(requestQuery, resourceType, studyInstanceUID, seriesInstanceUID), cancellationToken);
        }
    }
}
