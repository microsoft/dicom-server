// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    public class StoreHandler : IRequestHandler<StoreRequest, StoreResponse>
    {
        private readonly IInstanceEntryReaderManager _instanceEntryReaderManager;
        private readonly IStoreService _storeService;

        public StoreHandler(
            IInstanceEntryReaderManager instanceEntryReaderManager,
            IStoreService storeService)
        {
            EnsureArg.IsNotNull(instanceEntryReaderManager, nameof(instanceEntryReaderManager));
            EnsureArg.IsNotNull(storeService, nameof(storeService));

            _instanceEntryReaderManager = instanceEntryReaderManager;
            _storeService = storeService;
        }

        /// <inheritdoc />
        public async Task<StoreResponse> Handle(
            StoreRequest message,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            StoreRequestValidator.ValidateRequest(message);

            // Find a reader that can parse the request body.
            IInstanceEntryReader instanceEntryReader = _instanceEntryReaderManager.FindReader(message.RequestContentType);

            if (instanceEntryReader == null)
            {
                throw new UnsupportedMediaTypeException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.UnsupportedContentType, message.RequestContentType));
            }

            // Read list of entries.
            IReadOnlyList<IInstanceEntry> instanceEntries = await instanceEntryReader.ReadAsync(
                    message.RequestContentType,
                    message.RequestBody,
                    cancellationToken);

            // Process list of entries.
            return await _storeService.ProcessAsync(instanceEntries, message.StudyInstanceUid, cancellationToken);
        }
    }
}
