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
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Options;
using Microsoft.Health.CosmosDb.Configs;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.CosmosDb.Config;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents;
using Microsoft.Health.Extensions.DependencyInjection;
using Polly;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage
{
    internal class DicomCosmosDataStore : IDicomIndexDataStore
    {
        private readonly IDocumentClient _documentClient;
        private readonly Uri _collectionUri;
        private readonly DicomCosmosConfiguration _dicomConfiguration;
        private readonly string _databaseId;
        private readonly string _collectionId;

        public DicomCosmosDataStore(
            IScoped<IDocumentClient> documentClient,
            CosmosDataStoreConfiguration configuration,
            IOptionsMonitor<CosmosCollectionConfiguration> namedCosmosCollectionConfigurationAccessor,
            DicomCosmosConfiguration dicomConfiguration)
        {
            EnsureArg.IsNotNull(documentClient?.Value, nameof(documentClient));
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(namedCosmosCollectionConfigurationAccessor, nameof(namedCosmosCollectionConfigurationAccessor));
            EnsureArg.IsNotNull(dicomConfiguration, nameof(dicomConfiguration));

            CosmosCollectionConfiguration cosmosCollectionConfiguration = namedCosmosCollectionConfigurationAccessor.Get(Constants.CollectionConfigurationName);

            _documentClient = documentClient.Value;
            _collectionUri = configuration.GetRelativeCollectionUri(cosmosCollectionConfiguration.CollectionId);
            _databaseId = configuration.DatabaseId;
            _collectionId = cosmosCollectionConfiguration.CollectionId;
            _dicomConfiguration = dicomConfiguration;
        }

        /// <inheritdoc />
        public async Task IndexInstanceAsync(DicomDataset dicomDataset, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            var dicomIdentity = DicomIdentity.Create(dicomDataset);
            var defaultDocument = new QuerySeriesDocument(dicomIdentity.StudyInstanceUID, dicomIdentity.SeriesInstanceUID);

            RequestOptions requestOptions = CreateRequestOptions(defaultDocument.PartitionKey);

            // Retry when the pre-condition fails on replace (ETag check).
            IAsyncPolicy retryPolicy = CreatePreConditionFailedRetryPolicy();
            await _documentClient.ThrowIndexDataStoreException(
                async (documentClient) =>
                {
                    QuerySeriesDocument document = await documentClient.GetorCreateDocumentAsync(_databaseId, _collectionId, defaultDocument.Id, requestOptions, defaultDocument, cancellationToken);

                    var instance = QueryInstance.Create(dicomDataset, _dicomConfiguration.QueryAttributes);

                    if (!document.AddInstance(instance))
                    {
                        throw new IndexDataStoreException(HttpStatusCode.Conflict);
                    }

                    // Note, we do a replace (rather than upsert) in case the document is deleted.
                    requestOptions.AccessCondition = new AccessCondition() { Condition = document.ETag, Type = AccessConditionType.IfMatch };

                    Uri documentUri = UriFactory.CreateDocumentUri(_databaseId, _collectionId, document.Id);
                    await documentClient.ReplaceDocumentAsync(documentUri, document, requestOptions, cancellationToken: cancellationToken);
                },
                retryPolicy);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DicomIdentity>> DeleteSeriesIndexAsync(
            string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID, nameof(studyInstanceUID));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUID, nameof(seriesInstanceUID));

            Uri documentUri = UriFactory.CreateDocumentUri(
                _databaseId, _collectionId, QuerySeriesDocument.GetDocumentId(studyInstanceUID, seriesInstanceUID));
            RequestOptions requestOptions = CreateRequestOptions(QuerySeriesDocument.GetPartitionKey(studyInstanceUID));

            // Retry when the pre-condition fails on delete.
            IAsyncPolicy retryPolicy = CreatePreConditionFailedRetryPolicy();
            return await _documentClient.ThrowIndexDataStoreException(
                async (documentClient) =>
                {
                    DocumentResponse<QuerySeriesDocument> response =
                            await documentClient.ReadDocumentAsync<QuerySeriesDocument>(documentUri, requestOptions, cancellationToken: cancellationToken);

                    // Update the ETag check on the request options.
                    requestOptions.AccessCondition = new AccessCondition() { Condition = response.Document.ETag, Type = AccessConditionType.IfMatch };

                    // Delete the entire series document; if this fails the entire read and delete should be retried.
                    await documentClient.DeleteDocumentAsync(documentUri, requestOptions, cancellationToken);

                    return response.Document.Instances
                                            .Select(x => new DicomIdentity(response.Document.StudyInstanceUID, response.Document.SeriesInstanceUID, x.SopInstanceUID))
                                            .ToArray();
                },
                retryPolicy);
        }

        /// <inheritdoc />
        public async Task DeleteInstanceIndexAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID, nameof(studyInstanceUID));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUID, nameof(seriesInstanceUID));
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUID, nameof(sopInstanceUID));

            Uri documentUri = UriFactory.CreateDocumentUri(
                _databaseId, _collectionId, QuerySeriesDocument.GetDocumentId(studyInstanceUID, seriesInstanceUID));
            RequestOptions requestOptions = CreateRequestOptions(QuerySeriesDocument.GetPartitionKey(studyInstanceUID));

            IAsyncPolicy retryPolicy = CreatePreConditionFailedRetryPolicy();
            await _documentClient.ThrowIndexDataStoreException(
                async (documentClient) =>
                {
                    DocumentResponse<QuerySeriesDocument> response =
                            await documentClient.ReadDocumentAsync<QuerySeriesDocument>(documentUri, requestOptions, cancellationToken: cancellationToken);

                    // If the instance does not exist, throw not found exception.
                    if (!response.Document.RemoveInstance(sopInstanceUID))
                    {
                        throw new IndexDataStoreException(HttpStatusCode.NotFound);
                    }

                    requestOptions.AccessCondition = new AccessCondition() { Condition = response.Document.ETag, Type = AccessConditionType.IfMatch };

                    // Delete the entire series if no more instances, otherwise replace.
                    if (response.Document.Instances.Count == 0)
                    {
                        await documentClient.DeleteDocumentAsync(documentUri, requestOptions, cancellationToken);
                    }
                    else
                    {
                        await documentClient.ReplaceDocumentAsync(documentUri, response.Document, requestOptions, cancellationToken: cancellationToken);
                    }
                },
                retryPolicy);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DicomIdentity>> GetInstancesInStudyAsync(string studyInstanceUID, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID, nameof(studyInstanceUID));

            FeedOptions feedOptions = CreateFeedOptions(studyInstanceUID);
            var identityQuery = _documentClient.CreateDocumentQuery<QuerySeriesDocument>(_collectionUri, feedOptions)
                    .SelectMany(x => x.Instances.Select(y => new { x.StudyInstanceUID, x.SeriesInstanceUID, y.SopInstanceUID }))
                    .AsDocumentQuery();

            return await _documentClient.ThrowIndexDataStoreException(
                async (documentClient) =>
                {
                    var results = new List<DicomIdentity>();

                    while (identityQuery.HasMoreResults)
                    {
                        FeedResponse<DicomIdentity> nextResults = await identityQuery.ExecuteNextAsync<DicomIdentity>(cancellationToken);
                        results.AddRange(nextResults);
                    }

                    if (results.Any())
                    {
                        return results;
                    }

                    // If no results, this study does not exist.
                    throw new IndexDataStoreException(HttpStatusCode.NotFound);
                });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DicomIdentity>> GetInstancesInSeriesAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID, nameof(studyInstanceUID));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUID, nameof(seriesInstanceUID));

            string documentId = QuerySeriesDocument.GetDocumentId(studyInstanceUID, seriesInstanceUID);
            RequestOptions requestOptions = CreateRequestOptions(QuerySeriesDocument.GetPartitionKey(studyInstanceUID));

            return await _documentClient.ThrowIndexDataStoreException(
                async (documentClient) =>
                {
                    DocumentResponse<QuerySeriesDocument> response = await documentClient.ReadDocumentAsync<QuerySeriesDocument>(
                                                        UriFactory.CreateDocumentUri(_databaseId, _collectionId, documentId),
                                                        requestOptions,
                                                        cancellationToken: cancellationToken);

                    return response.Document.Instances.Select(
                        x => new DicomIdentity(response.Document.StudyInstanceUID, response.Document.SeriesInstanceUID, x.SopInstanceUID));
                });
        }

        private RequestOptions CreateRequestOptions(string partitionKey)
            => new RequestOptions() { PartitionKey = new PartitionKey(partitionKey) };

        private FeedOptions CreateFeedOptions(string partitionKey)
            => new FeedOptions() { PartitionKey = new PartitionKey(partitionKey) };

        private static IAsyncPolicy CreatePreConditionFailedRetryPolicy()
            => Policy
                    .Handle<DocumentClientException>(ex => ex.StatusCode == HttpStatusCode.PreconditionFailed)
                    .RetryForeverAsync();
    }
}
