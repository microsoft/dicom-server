// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Messages.ChangeFeed;

namespace Microsoft.Health.Dicom.Core.Features.ChangeFeed
{
    public class ChangeFeedLatestHandler : IRequestHandler<ChangeFeedLatestRequest, ChangeFeedLatestResponse>
    {
        private readonly IChangeFeedService _changeFeedService;

        public ChangeFeedLatestHandler(IChangeFeedService changeFeedService)
        {
            EnsureArg.IsNotNull(changeFeedService, nameof(changeFeedService));

            _changeFeedService = changeFeedService;
        }

        public async Task<ChangeFeedLatestResponse> Handle(ChangeFeedLatestRequest request, CancellationToken cancellationToken)
        {
            ChangeFeedEntry latestEntry = await _changeFeedService.GetChangeFeedLatestAsync(request.IncludeMetadata, cancellationToken);
            return new ChangeFeedLatestResponse(latestEntry);
        }
    }
}
