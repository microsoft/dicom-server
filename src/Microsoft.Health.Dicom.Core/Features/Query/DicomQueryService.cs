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
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class DicomQueryService : IDicomQueryService
    {
        private readonly IDicomQueryParser _queryParser;
        private readonly IDicomQueryStore _queryStore;
        private readonly IDicomMetadataStore _metadataService;

        public DicomQueryService(
                    IDicomQueryParser queryParser,
                    IDicomQueryStore queryStore,
                    IDicomMetadataStore metadataService)
        {
            EnsureArg.IsNotNull(queryParser, nameof(queryParser));
            EnsureArg.IsNotNull(queryStore, nameof(queryStore));

            _queryParser = queryParser;
            _queryStore = queryStore;
            _metadataService = metadataService;
        }

        public async Task<QueryDicomResourceResponse> QueryAsync(
            QueryDicomResourceRequest message,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message);

            DicomQueryExpression dicomQueryExpression = _queryParser.Parse(message);

            DicomQueryResult queryResult = await _queryStore.QueryAsync(dicomQueryExpression, cancellationToken);

            if (!queryResult.DicomInstances.Any())
            {
                return new QueryDicomResourceResponse();
            }

            IEnumerable<DicomDataset> instanceMetadata = await Task.WhenAll(
                   queryResult.DicomInstances
                   .Select(x => _metadataService.GetInstanceMetadataAsync(x, cancellationToken)));

            var responseBuilder = new QueryResponseBuilder(dicomQueryExpression);
            IEnumerable<DicomDataset> responseMetadata = instanceMetadata.Select(m => responseBuilder.GenerateResponseDataset(m));

            return new QueryDicomResourceResponse(responseMetadata);
        }
    }
}
