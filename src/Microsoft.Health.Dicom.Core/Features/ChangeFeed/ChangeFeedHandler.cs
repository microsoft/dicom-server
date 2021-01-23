// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Messages.ChangeFeed;

namespace Microsoft.Health.Dicom.Core.Features.ChangeFeed
{
    public class ChangeFeedHandler : BaseHandler, IRequestHandler<ChangeFeedRequest, ChangeFeedResponse>
    {
        private readonly IChangeFeedService _changeFeedService;

        public ChangeFeedHandler(IDicomAuthorizationService dicomAuthorizationService, IChangeFeedService changeFeedService)
            : base(dicomAuthorizationService)
        {
            _changeFeedService = EnsureArg.IsNotNull(changeFeedService, nameof(changeFeedService));
        }

        public async Task<ChangeFeedResponse> Handle(ChangeFeedRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            if (await AuthorizationService.CheckAccess(DataActions.Read, cancellationToken) != DataActions.Read)
            {
                throw new UnauthorizedDicomActionException();
            }

            IReadOnlyCollection<ChangeFeedEntry> changeFeedEntries = await _changeFeedService.GetChangeFeedAsync(request.Offset, request.Limit, request.IncludeMetadata, cancellationToken);

            return new ChangeFeedResponse(changeFeedEntries);
        }
    }
}
