// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Messages.ChangeFeed;

namespace Microsoft.Health.Dicom.Core.Features.ChangeFeed
{
    public class ChangeFeedHandler : IRequestHandler<ChangeFeedRequest, ChangeFeedResponse>
    {
        private readonly IChangeFeedService _changeFeedService;

        public ChangeFeedHandler(IChangeFeedService changeFeedService)
        {
            EnsureArg.IsNotNull(changeFeedService, nameof(changeFeedService));

            _changeFeedService = changeFeedService;
        }

        public async Task<ChangeFeedResponse> Handle(ChangeFeedRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            IReadOnlyCollection<ChangeFeedEntry> changeFeedEntries = await _changeFeedService.GetChangeFeedAsync(request.Offset, request.Limit, request.IncludeMetadata, cancellationToken);

            return new ChangeFeedResponse(changeFeedEntries);
        }
    }
}
