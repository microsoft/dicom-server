// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class QueryDicomResourceHandler : IRequestHandler<QueryDicomResourceRequest, QueryDicomResourceResponse>
    {
        private readonly ILogger<QueryDicomResourceHandler> _logger;

        public QueryDicomResourceHandler(
                    ILogger<QueryDicomResourceHandler> logger)
        {
            _logger = logger;
        }

        public async Task<QueryDicomResourceResponse> Handle(QueryDicomResourceRequest message, CancellationToken cancellationToken)
        {
            var dicomQueryExpression = DicomQueryExpression.Parse(message.RequestQuery, message.ResourceType);
            return await Task.FromResult(new QueryDicomResourceResponse(System.Net.HttpStatusCode.OK));
        }
    }
}
