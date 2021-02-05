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
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    public class StoreHandler : BaseHandler, IRequestHandler<StoreRequest, StoreResponse>
    {
        private readonly IDicomInstanceEntryReaderManager _dicomInstanceEntryReaderManager;
        private readonly IStoreService _storeService;

        public StoreHandler(
            IDicomAuthorizationService dicomAuthorizationService,
            IDicomInstanceEntryReaderManager dicomInstanceEntryReaderManager,
            IStoreService storeService)
            : base(dicomAuthorizationService)
        {
            _dicomInstanceEntryReaderManager = EnsureArg.IsNotNull(dicomInstanceEntryReaderManager, nameof(dicomInstanceEntryReaderManager));
            _storeService = EnsureArg.IsNotNull(storeService, nameof(storeService));
        }

        /// <inheritdoc />
        public async Task<StoreResponse> Handle(
            StoreRequest message,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            if (await AuthorizationService.CheckAccess(DataActions.Write, cancellationToken) != DataActions.Write)
            {
                throw new UnauthorizedDicomActionException(DataActions.Write);
            }

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
