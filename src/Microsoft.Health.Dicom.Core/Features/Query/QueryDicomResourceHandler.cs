// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class QueryDicomResourceHandler : IRequestHandler<QueryDicomResourceRequest, QueryDicomResourceResponse>
    {
        private readonly IDicomQueryParser _queryParser;
        private readonly ILogger<QueryDicomResourceHandler> _logger;
        private readonly IDicomQueryService _queryService;

        public QueryDicomResourceHandler(
                    IDicomQueryParser queryParser,
                    IDicomQueryService queryService,
                    ILogger<QueryDicomResourceHandler> logger)
        {
            EnsureArg.IsNotNull(queryParser, nameof(queryParser));
            EnsureArg.IsNotNull(queryService, nameof(queryService));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _queryParser = queryParser;
            _logger = logger;
            _queryService = queryService;
        }

        public async Task<QueryDicomResourceResponse> Handle(QueryDicomResourceRequest message, CancellationToken cancellationToken)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser.Parse(message.RequestQuery, message.ResourceType);

            var queryOptions = new DicomQueryOptions(dicomQueryExpression, message.ResourceType, message.StudyInstanceUID, message.SeriesInstanceUID);

            // TODO convert result to DicomDataset and pass it to the Response
            DicomQueryResult result = await _queryService.QueryAsync(queryOptions, cancellationToken);

            return new QueryDicomResourceResponse(System.Net.HttpStatusCode.NotImplemented);
        }
    }
}
