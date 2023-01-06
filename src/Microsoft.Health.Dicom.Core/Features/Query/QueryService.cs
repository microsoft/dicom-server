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
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Core.Features.Query;

public class QueryService : IQueryService
{
    private readonly IQueryParser<QueryExpression, QueryParameters> _queryParser;
    private readonly IQueryStore _queryStore;
    private readonly IMetadataStore _metadataStore;
    private readonly IQueryTagService _queryTagService;
    private readonly IDicomRequestContextAccessor _contextAccessor;

    public QueryService(
        IQueryParser<QueryExpression, QueryParameters> queryParser,
        IQueryStore queryStore,
        IMetadataStore metadataStore,
        IQueryTagService queryTagService,
        IDicomRequestContextAccessor contextAccessor)
    {
        _queryParser = EnsureArg.IsNotNull(queryParser, nameof(queryParser));
        _queryStore = EnsureArg.IsNotNull(queryStore, nameof(queryStore));
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _queryTagService = EnsureArg.IsNotNull(queryTagService, nameof(queryTagService));
        _contextAccessor = EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));
    }

    public async Task<QueryResourceResponse> QueryAsync(
        QueryParameters parameters,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(parameters);

        ValidateRequestIdentifiers(parameters);

        var queryTags = await _queryTagService.GetQueryTagsAsync(cancellationToken: cancellationToken);

        QueryExpression queryExpression = _queryParser.Parse(parameters, queryTags);

        var partitionKey = _contextAccessor.RequestContext.GetPartitionKey();

        QueryResult queryResult = await _queryStore.QueryAsync(partitionKey, queryExpression, cancellationToken);

        if (!queryResult.DicomInstances.Any())
        {
            return new QueryResourceResponse(Array.Empty<DicomDataset>(), queryExpression.ErroneousTags);
        }

        var responseBuilder = new QueryResponseBuilder(queryExpression);
        IEnumerable<DicomDataset> responseMetadata = null;

        if (QueryLimit.ContainsComputedTag(queryExpression.IELevel, queryExpression.IncludeFields.DicomTags))
        {
            var versions = queryResult.DicomInstances.Select(i => i.Version).ToList();

            if (queryExpression.IELevel == Messages.ResourceType.Study)
            {
                var studyComputedResults = await _queryStore.GetStudyResultAsync(partitionKey, versions, cancellationToken);
                var studyResultUidMap = studyComputedResults.ToDictionary<StudyResult, string>(a => a.StudyInstanceUid, StringComparer.OrdinalIgnoreCase);
                responseMetadata = await Task.WhenAll(queryResult.DicomInstances.Select(x =>
                GenerateQueryResult(x, studyResultUidMap[x.StudyInstanceUid].DicomDataset, responseBuilder, getFullMetadata: true, cancellationToken)));
            }
            else if (queryExpression.IELevel == Messages.ResourceType.Series)
            {
                var seriesComputedResults = await _queryStore.GetSeriesResultAsync(partitionKey, versions, cancellationToken);
                var seriesResultMap = seriesComputedResults.ToDictionary<SeriesResult, string>(a => a.StudyInstanceUid + "-" + a.SeriesInstanceUid, StringComparer.OrdinalIgnoreCase);
                responseMetadata = await Task.WhenAll(queryResult.DicomInstances.Select(x =>
                GenerateQueryResult(x, seriesResultMap[x.StudyInstanceUid + "-" + x.SeriesInstanceUid].DicomDataset, responseBuilder, getFullMetadata: true, cancellationToken)));
            }
        }
        if (responseMetadata == null)
        {
            IEnumerable<DicomDataset> instanceMetadata = await Task.WhenAll(
                queryResult.DicomInstances.Select(x => _metadataStore.GetInstanceMetadataAsync(x, cancellationToken)));

            responseMetadata = instanceMetadata.Select(m => responseBuilder.GenerateResponseDataset(m));
        }

        return new QueryResourceResponse(responseMetadata, queryExpression.ErroneousTags);
    }

    // TODO version the API to change the default results to only include indexed columns and control the getFullMetadata param
    private async Task<DicomDataset> GenerateQueryResult(
        VersionedInstanceIdentifier versionedInstance,
        DicomDataset computedMetadata,
        QueryResponseBuilder queryResponseBuilder,
        bool getFullMetadata,
        CancellationToken cancellationToken)
    {
        if (getFullMetadata)
        {
            DicomDataset fullMetadata = await _metadataStore.GetInstanceMetadataAsync(versionedInstance, cancellationToken);
            return queryResponseBuilder.GenerateResponseDataset(fullMetadata, computedMetadata);
        }
        return queryResponseBuilder.GenerateResponseDataset(computedMetadata);
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
