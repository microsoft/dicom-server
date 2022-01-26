// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public class WorkitemQueryService : IQueryService
    {
        private readonly IQueryParser _queryParser;
        private readonly IQueryStore _queryStore;
        private readonly IMetadataStore _metadataStore;
        private readonly IWorkitemQueryTagService _workitemQueryTagService;
        private readonly IDicomRequestContextAccessor _contextAccessor;

        public WorkitemQueryService(
            IQueryParser queryParser,
            IQueryStore queryStore,
            IMetadataStore metadataStore,
            IWorkitemQueryTagService workitemQueryTagService,
            IDicomRequestContextAccessor contextAccessor)
        {
            _queryParser = EnsureArg.IsNotNull(queryParser, nameof(queryParser));
            _queryStore = EnsureArg.IsNotNull(queryStore, nameof(queryStore));
            _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
            _workitemQueryTagService = EnsureArg.IsNotNull(workitemQueryTagService, nameof(workitemQueryTagService));
            _contextAccessor = EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));
        }

        public async Task<QueryResourceResponse> QueryAsync(
            QueryParameters parameters,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(parameters);

            var queryTags = await _workitemQueryTagService.GetQueryTagsAsync(cancellationToken: cancellationToken);

            QueryExpression queryExpression = _queryParser.Parse(parameters, queryTags);

            var partitionKey = _contextAccessor.RequestContext.GetPartitionKey();

            QueryResult queryResult = await _queryStore.QueryAsync(partitionKey, queryExpression, cancellationToken);

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
    }
}
