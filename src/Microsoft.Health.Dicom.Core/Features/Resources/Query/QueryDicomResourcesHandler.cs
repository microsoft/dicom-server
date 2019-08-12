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
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Query
{
    public class QueryDicomResourcesHandler : IRequestHandler<QueryDicomResourcesRequest, QueryDicomResourcesResponse>
    {
        private const string MoreResultsWarningMessage = "299 {+service}: There are additional results that can be requested.";
        private const string FuzzyMatchingNotSupportedWarningMessage = "299 {+service}: The fuzzymatching parameter is not supported. Only literal matching has been performed.";
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

        private async Task<QueryDicomResourcesResponse> HandleStudyQueryAsync(QueryDicomResourcesRequest message, CancellationToken cancellationToken)
        {
            IEnumerable<DicomDataset> resultDatasets;
            QueryResult<DicomStudy> queryResult = await _dicomIndexDataStore.QueryStudiesAsync(
                message.Offset, message.Limit, message.StudyInstanceUID, message.Query, cancellationToken);

            if (message.AllOptionalAttributesRequired)
            {
                resultDatasets = await Task.WhenAll(
                    queryResult.Results.Select(
                        x => _dicomMetadataStore.GetStudyDicomMetadataWithAllOptionalAsync(x.StudyInstanceUID, cancellationToken)));
            }
            else
            {
                resultDatasets = await Task.WhenAll(
                   queryResult.Results.Select(
                       x => _dicomMetadataStore.GetStudyDicomMetadataAsync(x.StudyInstanceUID, message.OptionalAttributes, cancellationToken)));
            }

            string[] warnings = GetWarningMessages(queryResult.HasMoreResults, message.FuzzyMatching);
            return new QueryDicomResourcesResponse(HttpStatusCode.OK, resultDatasets, warnings);
        }

        private async Task<QueryDicomResourcesResponse> HandleSeriesQueryAsync(QueryDicomResourcesRequest message, CancellationToken cancellationToken)
        {
            IEnumerable<DicomDataset> resultDatasets;
            QueryResult<DicomSeries> queryResult = await _dicomIndexDataStore.QuerySeriesAsync(
                message.Offset, message.Limit, message.StudyInstanceUID, message.Query, cancellationToken);

            if (message.AllOptionalAttributesRequired)
            {
                resultDatasets = await Task.WhenAll(
                    queryResult.Results.Select(
                        x => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(x.StudyInstanceUID, x.SeriesInstanceUID, cancellationToken)));
            }
            else
            {
                resultDatasets = await Task.WhenAll(
                   queryResult.Results.Select(
                       x => _dicomMetadataStore.GetSeriesDicomMetadataAsync(x.StudyInstanceUID, x.SeriesInstanceUID, message.OptionalAttributes, cancellationToken)));
            }

            string[] warnings = GetWarningMessages(queryResult.HasMoreResults, message.FuzzyMatching);
            return new QueryDicomResourcesResponse(HttpStatusCode.OK, resultDatasets, warnings);
        }

        private async Task<QueryDicomResourcesResponse> HandleInstanceQueryAsync(QueryDicomResourcesRequest message, CancellationToken cancellationToken)
        {
            IEnumerable<DicomDataset> resultDatasets;
            QueryResult<DicomInstance> queryResult = await _dicomIndexDataStore.QueryInstancesAsync(
                message.Offset, message.Limit, message.StudyInstanceUID, message.Query, cancellationToken);

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
                       x => _dicomInstanceMetadataStore.GetInstanceMetadataAsync(x, message.OptionalAttributes, cancellationToken)));
            }

            string[] warnings = GetWarningMessages(queryResult.HasMoreResults, message.FuzzyMatching);
            return new QueryDicomResourcesResponse(HttpStatusCode.OK, resultDatasets, warnings);
        }

        private static string[] GetWarningMessages(bool hasMoreResults, bool fuzzyMatching)
        {
            // If we have more results, we need to let the caller know.
            if (hasMoreResults)
            {
                if (fuzzyMatching)
                {
                    return new string[] { MoreResultsWarningMessage, FuzzyMatchingNotSupportedWarningMessage };
                }

                return new string[] { MoreResultsWarningMessage };
            }

            // Currently we do not support fuzzy matching; must be absolute match.
            if (fuzzyMatching)
            {
                return new string[] { FuzzyMatchingNotSupportedWarningMessage };
            }

            return Array.Empty<string>();
        }
    }
}
