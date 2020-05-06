// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class QueryService : IQueryService
    {
        private readonly IQueryParser _queryParser;
        private readonly IQueryStore _queryStore;
        private readonly IMetadataStore _metadataService;

        public QueryService(
            IQueryParser queryParser,
            IQueryStore queryStore,
            IMetadataStore metadataService)
        {
            EnsureArg.IsNotNull(queryParser, nameof(queryParser));
            EnsureArg.IsNotNull(queryStore, nameof(queryStore));

            _queryParser = queryParser;
            _queryStore = queryStore;
            _metadataService = metadataService;
        }

        public async Task<QueryResourceResponse> QueryAsync(
            QueryResourceRequest message,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message);

            ValidateRequestIdentifiers(message);

            QueryExpression queryExpression = _queryParser.Parse(message);

            QueryResult queryResult = await _queryStore.QueryAsync(queryExpression, cancellationToken);

            if (!queryResult.DicomInstances.Any())
            {
                return new QueryResourceResponse();
            }

            var responseBuilder = new QueryResponseBuilder(queryExpression);
            return new QueryResourceResponse(GetAsyncEnumerableResponse(queryResult.DicomInstances, responseBuilder, cancellationToken));
        }

        private async IAsyncEnumerable<DicomDataset> GetAsyncEnumerableResponse(
            IEnumerable<VersionedInstanceIdentifier> ids,
            QueryResponseBuilder responseBuilder,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var id in ids)
            {
                DicomDataset ds = await _metadataService.GetInstanceMetadataAsync(id, cancellationToken);
                yield return responseBuilder.GenerateResponseDataset(ds);
            }
        }

        private void ValidateRequestIdentifiers(QueryResourceRequest message)
        {
            switch (message.QueryResourceType)
            {
                case QueryResource.StudySeries:
                case QueryResource.StudyInstances:
                    UidValidator.ValidateAndThrow(message.StudyInstanceUid, nameof(message.StudyInstanceUid));
                    break;
                case QueryResource.StudySeriesInstances:
                    UidValidator.ValidateAndThrow(message.StudyInstanceUid, nameof(message.StudyInstanceUid));
                    UidValidator.ValidateAndThrow(message.SeriesInstanceUid, nameof(message.SeriesInstanceUid));
                    break;
                case QueryResource.AllStudies:
                case QueryResource.AllSeries:
                case QueryResource.AllInstances:
                    break;
                default:
                    Debug.Fail("A newly added query resource is not handled.");
                    break;
            }
        }
    }
}
