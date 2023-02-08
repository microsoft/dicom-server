// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.Query;
using Microsoft.Health.Dicom.Core.Models.Common;

namespace Microsoft.Health.Dicom.Core.Features.Query;

public class QueryService : IQueryService
{
    private readonly IQueryParser<QueryExpression, QueryParameters> _queryParser;
    private readonly IQueryStore _queryStore;
    private readonly IMetadataStore _metadataStore;
    private readonly IQueryTagService _queryTagService;
    private readonly IDicomRequestContextAccessor _contextAccessor;
    private readonly ILogger<QueryService> _logger;

    public QueryService(
        IQueryParser<QueryExpression, QueryParameters> queryParser,
        IQueryStore queryStore,
        IMetadataStore metadataStore,
        IQueryTagService queryTagService,
        IDicomRequestContextAccessor contextAccessor,
        ILogger<QueryService> logger)
    {
        _queryParser = EnsureArg.IsNotNull(queryParser, nameof(queryParser));
        _queryStore = EnsureArg.IsNotNull(queryStore, nameof(queryStore));
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _queryTagService = EnsureArg.IsNotNull(queryTagService, nameof(queryTagService));
        _contextAccessor = EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
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

        Stopwatch stopwatch = new Stopwatch();

        stopwatch.Start();
        QueryResult queryResult = await _queryStore.QueryAsync(partitionKey, queryExpression, cancellationToken);
        stopwatch.Stop();
        var filterTime = stopwatch.ElapsedMilliseconds;

        if (!queryResult.DicomInstances.Any())
        {
            return new QueryResourceResponse(Array.Empty<DicomDataset>(), queryExpression.ErroneousTags);
        }

        var responseBuilder = new QueryResponseBuilder(queryExpression, ReturnNewTagDefaults(_contextAccessor.RequestContext.Version));

        stopwatch.Restart();
        IEnumerable<DicomDataset> instanceMetadata = await GetInstanceMetadataAsync(partitionKey, queryExpression, queryResult, responseBuilder.ReturnTags, cancellationToken);
        stopwatch.Stop();
        var resultTime = stopwatch.ElapsedMilliseconds;
        _logger.LogInformation("QueryService performance filterTimeMilliseconds:{FilterTime}, resultTimeMilliseconds:{ResultTime}", filterTime, resultTime);

        var responseMetadata = instanceMetadata.Select(m => responseBuilder.GenerateResponseDataset(m));
        return new QueryResourceResponse(responseMetadata, queryExpression.ErroneousTags);
    }

    private static bool ReturnNewTagDefaults(int? version)
    {
        bool useNewDefaults = false;
        if (version != null && version >= 2)
        {
            useNewDefaults = true;
        }
        return useNewDefaults;
    }

    // Does not handle retrieving the extendedQueryTag indexes right now. Logs are in place to evaluate it in the future.
    private async Task<IEnumerable<DicomDataset>> GetInstanceMetadataAsync(
        int partitionKey,
        QueryExpression queryExpression,
        QueryResult queryResult,
        IReadOnlyCollection<DicomTag> returnTags,
        CancellationToken cancellationToken)
    {
        bool getStudyResponse = false, getSeriesResponse = false, getFullMetadata = false;

        ImmutableHashSet<DicomTag> tags = returnTags.ToImmutableHashSet();
        ImmutableHashSet<DicomTag> remaining = tags.Except(
            StudyResult.AvailableTags.Union(SeriesResult.AvailableTags));

        if (remaining.Count > 0)
        {
            getFullMetadata = true;
            if (QueryLimit.ContainsComputedTag(queryExpression.IELevel, returnTags))
            {
                if (queryExpression.IELevel == Messages.ResourceType.Study)
                    getStudyResponse = true;
                else if (queryExpression.IELevel == Messages.ResourceType.Series)
                    getSeriesResponse = true;
            }
        }
        else
        {
            getStudyResponse = tags.Overlaps(StudyResult.AvailableTags);
            getSeriesResponse = tags.Overlaps(SeriesResult.AvailableTags);
        }

        // logging to track usage
        _logger.LogInformation("QueryService result retrieval resultCount:{ResultCount}, studyResultRetrieved:{StudyResultRetrieved}, seriesResultRetrieved:{SeriesResultRetrieved}, fullMetadataRetrieved:{FullMetadataRetrieved}", queryResult.DicomInstances.Count(), getStudyResponse, getSeriesResponse, getFullMetadata);

        // start getting and merging the results based on the source.
        IEnumerable<DicomDataset> instanceMetadata = null;
        List<long> versions = queryResult.DicomInstances.Select(i => i.Version).ToList();
        if (getFullMetadata)
        {
            instanceMetadata = await Task.WhenAll(
                queryResult.DicomInstances.Select(x => _metadataStore.GetInstanceMetadataAsync(x, cancellationToken)));
        }
        if (getSeriesResponse)
        {
            IReadOnlyCollection<SeriesResult> seriesComputedResults = await _queryStore.GetSeriesResultAsync(partitionKey, versions, cancellationToken);

            if (instanceMetadata == null)
            {
                instanceMetadata = seriesComputedResults.Select(x => x.DicomDataset);
            }
            else
            {
                Dictionary<DicomIdentifier, SeriesResult> map = seriesComputedResults.ToDictionary<SeriesResult, DicomIdentifier>(a => new DicomIdentifier(a.StudyInstanceUid, a.SeriesInstanceUid, default));
                instanceMetadata = instanceMetadata.Select(x =>
                {
                    var ds = new DicomDataset(x);
                    return ds.AddOrUpdate(map[new DicomIdentifier(x.GetSingleValue<string>(DicomTag.StudyInstanceUID), x.GetSingleValue<string>(DicomTag.SeriesInstanceUID), default)].DicomDataset);
                });
            }
        }
        if (getStudyResponse)
        {
            IReadOnlyCollection<StudyResult> studyComputedResults = await _queryStore.GetStudyResultAsync(partitionKey, versions, cancellationToken);
            if (instanceMetadata == null)
            {
                instanceMetadata = studyComputedResults.Select(x => x.DicomDataset);
            }
            else
            {
                Dictionary<string, StudyResult> map = studyComputedResults.ToDictionary<StudyResult, string>(a => a.StudyInstanceUid, StringComparer.OrdinalIgnoreCase);
                instanceMetadata = instanceMetadata.Select(x =>
                {
                    var ds = new DicomDataset(x);
                    return ds.AddOrUpdate(map[x.GetSingleValue<string>(DicomTag.StudyInstanceUID)].DicomDataset);
                });
            }
        }

        return instanceMetadata;
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
