// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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

        IEnumerable<DicomTag> currentResponseTags = returnTags;
        // are the expected responses available in StudyResult
        var remainingTags = currentResponseTags.Except(StudyResult.AvailableTags).ToList();
        if (currentResponseTags.Count() > remainingTags.Count)
        {
            getStudyResponse = true;
        }
        // are the remaining expected responses available in SeriesResult
        if (remainingTags.Any())
        {
            currentResponseTags = remainingTags;
            remainingTags = currentResponseTags.Except(SeriesResult.AvailableTags).ToList();
            if (currentResponseTags.Count() > remainingTags.Count)
            {
                getSeriesResponse = true;
            }
        }
        // if still remaining response tags, just get full metadata
        if (remainingTags.Any())
        {
            _logger.LogInformation("QueryService tags returned from full metadata {FullMetadataTags}", string.Join(',', remainingTags));
            getFullMetadata = true;
            // exception of computed tags
            if (QueryLimit.ContainsComputedTag(queryExpression.IELevel, returnTags))
            {
                if (queryExpression.IELevel == Messages.ResourceType.Study)
                {
                    getStudyResponse = true;
                }
                else if (queryExpression.IELevel == Messages.ResourceType.Series)
                {
                    getSeriesResponse = true;
                }
            }
            else
            {
                getStudyResponse = false;
                getSeriesResponse = false;
            }
        }

        // logging to track usage
        _logger.LogInformation("QueryService result retrieval resultCount:{ResultCount}, studyResultRetrieved:{StudyResultRetrieved}, seriesResultRetrieved:{SeriesResultRetrieved}, fullMetadataRetrieved:{FullMetadataRetrieved}", queryResult.DicomInstances.Count(), getStudyResponse, getSeriesResponse, getFullMetadata);

        // start getting and merging the results based on the source.
        IEnumerable<DicomDataset> instanceMetadata = null;
        var versions = queryResult.DicomInstances.Select(i => i.Version).ToList();
        if (getFullMetadata)
        {
            instanceMetadata = await Task.WhenAll(
                queryResult.DicomInstances.Select(x => _metadataStore.GetInstanceMetadataAsync(x, cancellationToken)));
        }
        if (getSeriesResponse)
        {
            var seriesComputedResults = await _queryStore.GetSeriesResultAsync(partitionKey, versions, cancellationToken);

            if (instanceMetadata == null)
            {
                instanceMetadata = seriesComputedResults.Select(x => x.DicomDataset);
            }
            else
            {
                var map = seriesComputedResults.ToDictionary<SeriesResult, string>(a => a.StudyInstanceUid + "-" + a.SeriesInstanceUid, StringComparer.OrdinalIgnoreCase);
                instanceMetadata = instanceMetadata.Select(x =>
                {
                    var ds = new DicomDataset(x);
                    return ds.AddOrUpdate(map[x.GetSingleValue<string>(DicomTag.StudyInstanceUID) + "-" + x.GetSingleValue<string>(DicomTag.SeriesInstanceUID)].DicomDataset);
                });
            }
        }
        if (getStudyResponse)
        {
            var studyComputedResults = await _queryStore.GetStudyResultAsync(partitionKey, versions, cancellationToken);
            if (instanceMetadata == null)
            {
                instanceMetadata = studyComputedResults.Select(x => x.DicomDataset);
            }
            else
            {
                var map = studyComputedResults.ToDictionary<StudyResult, string>(a => a.StudyInstanceUid, StringComparer.OrdinalIgnoreCase);
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
