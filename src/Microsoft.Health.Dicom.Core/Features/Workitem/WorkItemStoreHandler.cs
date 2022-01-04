// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public class WorkitemStoreHandler : BaseHandler, IRequestHandler<WorkitemStoreRequest, WorkitemStoreResponse>
    {
        private readonly IDicomInstanceEntryReaderManager _dicomInstanceEntryReaderManager;
        private readonly IWorkitemService _workItemService;

        public WorkitemStoreHandler(
            IAuthorizationService<DataActions> authorizationService,
            IDicomInstanceEntryReaderManager dicomInstanceEntryReaderManager,
            IWorkitemService workItemService)
            : base(authorizationService)
        {
            _dicomInstanceEntryReaderManager = EnsureArg.IsNotNull(dicomInstanceEntryReaderManager, nameof(dicomInstanceEntryReaderManager));
            _workItemService = EnsureArg.IsNotNull(workItemService, nameof(workItemService));
        }

        /// <inheritdoc />
        public async Task<WorkitemStoreResponse> Handle(
            WorkitemStoreRequest request,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            if (await AuthorizationService.CheckAccess(DataActions.Write, cancellationToken) != DataActions.Write)
            {
                throw new UnauthorizedDicomActionException(DataActions.Write);
            }

            // TODO: Consider moving this into Service
            // StoreRequestValidator.ValidateRequest(request);

            // Find a reader that can parse the request body.
            var dicomInstanceEntryReader = _dicomInstanceEntryReaderManager.FindReader(request.RequestContentType);

            if (dicomInstanceEntryReader == null)
            {
                throw new UnsupportedMediaTypeException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.UnsupportedContentType, request.RequestContentType));
            }

            // Read list of entries, but we only expect a single entry to be present.
            var instanceEntries = await dicomInstanceEntryReader.ReadAsync(
                    request.RequestContentType,
                    request.RequestBody,
                    cancellationToken);

            // TODO: Validate/Check instance-entries is non-empty. Add test coverage.

            // Process the instance entry.
            return await _workItemService
                .ProcessAsync(instanceEntries?[0], request.WorkitemInstanceUid, cancellationToken);
        }
    }
}
