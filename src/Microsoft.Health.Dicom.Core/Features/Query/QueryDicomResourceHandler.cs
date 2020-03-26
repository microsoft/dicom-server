// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class QueryDicomResourceHandler : IRequestHandler<QueryDicomResourceRequest, QueryDicomResourceResponse>
    {
        private readonly IDicomQueryParser _queryParser;
        private readonly ILogger<QueryDicomResourceHandler> _logger;
        private readonly IDicomQueryService _queryService;
        private readonly IDicomMetadataService _dicomMetadataService;

        public QueryDicomResourceHandler(
                    IDicomQueryParser queryParser,
                    IDicomQueryService queryService,
                    IDicomMetadataService dicomMetadataService,
                    ILogger<QueryDicomResourceHandler> logger)
        {
            EnsureArg.IsNotNull(queryParser, nameof(queryParser));
            EnsureArg.IsNotNull(queryService, nameof(queryService));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _queryParser = queryParser;
            _logger = logger;
            _queryService = queryService;
            _dicomMetadataService = dicomMetadataService;
        }

        public async Task<QueryDicomResourceResponse> Handle(QueryDicomResourceRequest message, CancellationToken cancellationToken)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser.Parse(message);

            DicomQueryResult queryResult = await _queryService.QueryAsync(dicomQueryExpression, cancellationToken);

            if (!queryResult.DicomInstances.Any())
            {
                return new QueryDicomResourceResponse();
            }

            IEnumerable<DicomDataset> instanceMetadata = await Task.WhenAll(
                   queryResult.DicomInstances
                   .Select(x => _dicomMetadataService.GetInstanceMetadataAsync(x, cancellationToken)));

            var responseBuilder = new QueryResponseBuilder(dicomQueryExpression);
            IEnumerable<DicomDataset> responseMetadata = instanceMetadata.Select(m => responseBuilder.GenerateResponseDataset(m));

            return new QueryDicomResourceResponse(responseMetadata);
        }
    }
}
