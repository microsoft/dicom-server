// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
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

        public static Task<RetrieveDicomResourceResponse> RetrieveDicomResourcesAsync(
            this IMediator mediator, string studyInstanceUID, string requestedTransferSyntax, CancellationToken cancellationToken)
        {
            return mediator.Send(new RetrieveDicomResourceRequest(studyInstanceUID, requestedTransferSyntax), cancellationToken);
        }

        public static Task<RetrieveDicomResourceResponse> RetrieveDicomResourcesAsync(
            this IMediator mediator, string studyInstanceUID, string seriesInstanceUID, string requestedTransferSyntax, CancellationToken cancellationToken)
        {
            return mediator.Send(
                new RetrieveDicomResourceRequest(studyInstanceUID, seriesInstanceUID, requestedTransferSyntax),
                cancellationToken);
        }

        public static Task<RetrieveDicomResourceResponse> RetrieveDicomResourceAsync(
            this IMediator mediator, string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, string requestedTransferSyntax, CancellationToken cancellationToken)
        {
            return mediator.Send(
                new RetrieveDicomResourceRequest(studyInstanceUID, seriesInstanceUID, sopInstanceUID, requestedTransferSyntax),
                cancellationToken);
        }

        public static Task<RetrieveDicomResourceResponse> RetrieveDicomFramesAsync(
            this IMediator mediator, string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, int[] frames, string requestedTransferSyntax, CancellationToken cancellationToken)
        {
            return mediator.Send(
                new RetrieveDicomResourceRequest(studyInstanceUID, seriesInstanceUID, sopInstanceUID, frames, requestedTransferSyntax),
                cancellationToken);
        }
    }
}
