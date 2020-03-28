// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Persistence.Store
{
    public class StoreDicomResourcesHandler : IRequestHandler<StoreDicomResourcesRequest, StoreDicomResourcesResponse>
    {
        private readonly IDicomStoreService _dicomStoreService;

        public StoreDicomResourcesHandler(
            IDicomStoreService dicomStoreService)
        {
            EnsureArg.IsNotNull(dicomStoreService, nameof(dicomStoreService));
            _dicomStoreService = dicomStoreService;
        }

        /// <inheritdoc />
        public async Task<StoreDicomResourcesResponse> Handle(
            StoreDicomResourcesRequest message,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            StoreRequestValidator.ValidateRequest(message);

            return await _dicomStoreService.StoreMultiPartDicomResourceAsync(
                message.RequestBaseUri,
                message.RequestBody,
                message.RequestContentType,
                message.StudyInstanceUid,
                cancellationToken);
        }
    }
}
