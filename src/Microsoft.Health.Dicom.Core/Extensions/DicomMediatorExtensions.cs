// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    public static class DicomMediatorExtensions
    {
        public static Task<StoreDicomResourcesResponse> StoreDicomResourcesAsync(
            this IMediator mediator, string baseAddress, Stream requestBody, string requestContentType, string studyInstanceUID, CancellationToken cancellationToken = default)
        {
            return mediator.Send(new StoreDicomResourcesRequest(baseAddress, requestBody, requestContentType, studyInstanceUID), cancellationToken);
        }
    }
}
