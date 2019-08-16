// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Query
{
    public class QueryDicomResourcesHandler : IRequestHandler<QueryDicomResourcesRequest, QueryDicomResourcesResponse>
    {
        private const string MoreResultsWarningMessage = "299 {+service}: There are additional results that can be requested.";
        private const string FuzzyMatchingNotSupportedWarningMessage = "299 {+service}: The fuzzymatching parameter is not supported. Only literal matching has been performed.";
        private const string ResultsCoalescedWarningMessage = "299 {+service}: The results of this query have been coalesced because the underlying data has inconsistencies across the queried instances.";
        private readonly IDicomIndexDataStore _dicomIndexDataStore;
        private readonly IDicomMetadataStore _dicomMetadataStore;
        private readonly IDicomInstanceMetadataStore _dicomInstanceMetadataStore;

        public QueryDicomResourcesHandler(
            IDicomIndexDataStore dicomIndexDataStore, IDicomMetadataStore dicomMetadataStore, IDicomInstanceMetadataStore dicomInstanceMetadataStore)
        {
            EnsureArg.IsNotNull(dicomIndexDataStore, nameof(dicomIndexDataStore));
            EnsureArg.IsNotNull(dicomMetadataStore, nameof(dicomMetadataStore));
            EnsureArg.IsNotNull(dicomInstanceMetadataStore, nameof(dicomInstanceMetadataStore));

            _dicomIndexDataStore = dicomIndexDataStore;
            _dicomMetadataStore = dicomMetadataStore;
            _dicomInstanceMetadataStore = dicomInstanceMetadataStore;
        }

        public async Task<QueryDicomResourcesResponse> Handle(QueryDicomResourcesRequest message, CancellationToken cancellationToken)
        {
            try
            {
                switch (message.ResourceType)
                {
                    case Messages.ResourceType.Study:
                        return await HandleStudyQueryAsync(message, cancellationToken);

                    case Messages.ResourceType.Series:
                        return await HandleSeriesQueryAsync(message, cancellationToken);

                    case Messages.ResourceType.Instance:
                        return await HandleInstanceQueryAsync(message, cancellationToken);

                    default:
                        throw new NotImplementedException();
                }
            }
            catch (DataStoreException e)
            {
                return new QueryDicomResourcesResponse(e.StatusCode);
            }
        }

        private async Task<QueryDicomResourcesResponse> HandleStudyQueryAsync(QueryDicomResourcesRequest message, CancellationToken cancellationToken)
        {
            DicomMetadata[] resultMetadata;
            QueryResult<DicomStudy> queryResult = await _dicomIndexDataStore.QueryStudiesAsync(
                message.Offset, message.Limit, message.StudyInstanceUID, message.GetQueryAttributes(), cancellationToken);

            if (message.AllOptionalAttributesRequired)
            {
                resultMetadata = await Task.WhenAll(
                    queryResult.Results.Select(
                        x => _dicomMetadataStore.GetStudyDicomMetadataWithAllOptionalAsync(x.StudyInstanceUID, cancellationToken)));
            }
            else
            {
                resultMetadata = await Task.WhenAll(
                   queryResult.Results.Select(
                       x => _dicomMetadataStore.GetStudyDicomMetadataAsync(x.StudyInstanceUID, message.GetOptionalAttributes(), cancellationToken)));
            }

            IList<string> warnings = GetWarningMessages(queryResult.HasMoreResults, resultMetadata.Any(x => x.ResultCoalesced), message.FuzzyMatching);
            return new QueryDicomResourcesResponse(HttpStatusCode.OK, resultMetadata.Select(x => x.DicomDataset), warnings);
        }

        private async Task<QueryDicomResourcesResponse> HandleSeriesQueryAsync(QueryDicomResourcesRequest message, CancellationToken cancellationToken)
        {
            DicomMetadata[] resultMetadata;
            QueryResult<DicomSeries> queryResult = await _dicomIndexDataStore.QuerySeriesAsync(
                message.Offset, message.Limit, message.StudyInstanceUID, message.GetQueryAttributes(), cancellationToken);

            if (message.AllOptionalAttributesRequired)
            {
                resultMetadata = await Task.WhenAll(
                    queryResult.Results.Select(
                        x => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(x.StudyInstanceUID, x.SeriesInstanceUID, cancellationToken)));
            }
            else
            {
                resultMetadata = await Task.WhenAll(
                   queryResult.Results.Select(
                       x => _dicomMetadataStore.GetSeriesDicomMetadataAsync(x.StudyInstanceUID, x.SeriesInstanceUID, message.GetOptionalAttributes(), cancellationToken)));
            }

            IList<string> warnings = GetWarningMessages(queryResult.HasMoreResults, resultMetadata.Any(x => x.ResultCoalesced), message.FuzzyMatching);
            return new QueryDicomResourcesResponse(HttpStatusCode.OK, resultMetadata.Select(x => x.DicomDataset), warnings);
        }

        private async Task<QueryDicomResourcesResponse> HandleInstanceQueryAsync(QueryDicomResourcesRequest message, CancellationToken cancellationToken)
        {
            IEnumerable<DicomDataset> resultDatasets;
            QueryResult<DicomInstance> queryResult = await _dicomIndexDataStore.QueryInstancesAsync(
                message.Offset, message.Limit, message.StudyInstanceUID, message.GetQueryAttributes(), cancellationToken);

            if (message.AllOptionalAttributesRequired)
            {
                resultDatasets = await Task.WhenAll(
                    queryResult.Results.Select(
                        x => _dicomInstanceMetadataStore.GetInstanceMetadataAsync(x, cancellationToken)));
            }
            else
            {
                resultDatasets = await Task.WhenAll(
                   queryResult.Results.Select(
                       x => _dicomInstanceMetadataStore.GetInstanceMetadataAsync(x, message.GetOptionalAttributes(), cancellationToken)));
            }

            // Note: The results of an instance query search can never have coalesced results so always set to false.
            IList<string> warnings = GetWarningMessages(queryResult.HasMoreResults, coalescedResults: false, message.FuzzyMatching);
            return new QueryDicomResourcesResponse(HttpStatusCode.OK, resultDatasets, warnings);
        }

        private static IList<string> GetWarningMessages(bool hasMoreResults, bool coalescedResults, bool fuzzyMatching)
        {
            IList<string> warningMessages = new List<string>();

            // If we have more results, we need to let the caller know.
            if (hasMoreResults)
            {
                warningMessages.Add(MoreResultsWarningMessage);
            }

            if (coalescedResults)
            {
                warningMessages.Add(ResultsCoalescedWarningMessage);
            }

            // Currently we do not support fuzzy matching; must be absolute match.
            if (fuzzyMatching)
            {
                warningMessages.Add(FuzzyMatchingNotSupportedWarningMessage);
            }

            return warningMessages;
        }
    }
}
