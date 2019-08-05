// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Health.Dicom.Core.Messages.Delete;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    public static class DicomMediatorExtensions
    {
        public static Task<StoreDicomResourcesResponse> StoreDicomResourcesAsync(
            this IMediator mediator, Uri requestBaseUri, Stream requestBody, string requestContentType, string studyInstanceUID, CancellationToken cancellationToken = default)
        {
            return mediator.Send(new StoreDicomResourcesRequest(requestBaseUri, requestBody, requestContentType, studyInstanceUID), cancellationToken);
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
    }
}
