// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class QueryService : IQueryService
    {
        private readonly IQueryParser _queryParser;
        private readonly IQueryStore _queryStore;
        private readonly IMetadataStore _metadataStore;
        private readonly IQueryTagService _queryTagService;

        public QueryService(
            IQueryParser queryParser,
            IQueryStore queryStore,
            IMetadataStore metadataStore,
            IQueryTagService queryTagService)
        {
            EnsureArg.IsNotNull(queryParser, nameof(queryParser));
            EnsureArg.IsNotNull(queryStore, nameof(queryStore));
            EnsureArg.IsNotNull(queryTagService, nameof(queryTagService));

            _queryParser = queryParser;
            _queryStore = queryStore;
            _metadataStore = metadataStore;
            _queryTagService = queryTagService;
        }

        public async Task<QueryResourceResponse> QueryAsync(
            QueryParameters parameters,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(parameters);

            ValidateRequestIdentifiers(parameters);

            var queryTags = await _queryTagService.GetQueryTagsAsync(cancellationToken: cancellationToken);

            QueryExpression queryExpression = _queryParser.Parse(parameters, queryTags);

            QueryResult queryResult = await _queryStore.QueryAsync(queryExpression, cancellationToken);

            if (!queryResult.DicomInstances.Any())
            {
                return new QueryResourceResponse(Array.Empty<DicomDataset>(), queryExpression.ErroneousTags);
            }

            IEnumerable<DicomDataset> instanceMetadata = await Task.WhenAll(
                queryResult.DicomInstances.Select(x => _metadataStore.GetInstanceMetadataAsync(x, cancellationToken)));

            var responseBuilder = new QueryResponseBuilder(queryExpression);
            IEnumerable<DicomDataset> responseMetadata = instanceMetadata.Select(m => responseBuilder.GenerateResponseDataset(m));

            return new QueryResourceResponse(responseMetadata, queryExpression.ErroneousTags);
        }

        private static void ValidateRequestIdentifiers(QueryParameters parameters)
        {
            switch (parameters.QueryResourceType)
            {
                case QueryResource.StudySeries:
                case QueryResource.StudyInstances:
                    UidValidation.Validate(parameters.StudyInstanceUid, nameof(parameters.StudyInstanceUid));
                    break;
                case QueryResource.StudySeriesInstances:
                    UidValidation.Validate(parameters.StudyInstanceUid, nameof(parameters.StudyInstanceUid));
                    UidValidation.Validate(parameters.SeriesInstanceUid, nameof(parameters.SeriesInstanceUid));
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
