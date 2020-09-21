// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        private readonly IDicomInstanceEntryReaderManager _dicomInstanceEntryReaderManager;
        private readonly IStoreService _storeService;

        public StoreHandler(
            IDicomInstanceEntryReaderManager dicomInstanceEntryReaderManager,
            IStoreService storeService)
        {
            EnsureArg.IsNotNull(dicomInstanceEntryReaderManager, nameof(dicomInstanceEntryReaderManager));
            EnsureArg.IsNotNull(storeService, nameof(storeService));

            _dicomInstanceEntryReaderManager = dicomInstanceEntryReaderManager;
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
            IDicomInstanceEntryReader dicomInstanceEntryReader = _dicomInstanceEntryReaderManager.FindReader(message.RequestContentType);

            if (dicomInstanceEntryReader == null)
            {
                throw new UnsupportedMediaTypeException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.UnsupportedContentType, message.RequestContentType));
            }

            // Read list of entries.
            IReadOnlyList<IDicomInstanceEntry> instanceEntries = await dicomInstanceEntryReader.ReadAsync(
                    message.RequestContentType,
                    message.RequestBody,
                    cancellationToken);

            // Process list of entries.
            return await _storeService.ProcessAsync(instanceEntries, message.StudyInstanceUid, cancellationToken);
        }
    }
}
