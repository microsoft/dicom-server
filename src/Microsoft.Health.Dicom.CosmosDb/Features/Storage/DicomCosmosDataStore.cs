// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
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
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.StoredProcedures.Delete;
using Microsoft.Health.Extensions.DependencyInjection;
using Polly;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage
{
    internal class DicomCosmosDataStore : IDicomIndexDataStore
    {
        private readonly IDocumentClient _documentClient;
        private readonly Uri _collectionUri;
        private readonly DicomIndexingConfiguration _dicomConfiguration;
        private readonly CosmosQueryBuilder _queryBuilder;
        private readonly string _databaseId;
        private readonly string _collectionId;
        private readonly Random _random = new Random();

        public DicomCosmosDataStore(
            IScoped<IDocumentClient> documentClient,
            CosmosDataStoreConfiguration configuration,
            IOptionsMonitor<CosmosCollectionConfiguration> namedCosmosCollectionConfigurationAccessor,
            DicomIndexingConfiguration dicomConfiguration)
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
            _queryBuilder = new CosmosQueryBuilder(dicomConfiguration);
        }

        /// <inheritdoc />
        public async Task IndexSeriesAsync(IReadOnlyCollection<DicomDataset> instances, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(instances, nameof(instances));
            EnsureArg.HasItems(instances, nameof(instances));

            DicomInstance referenceInstance = null;
            foreach (DicomDataset instance in instances)
            {
                var dicomInstance = DicomInstance.Create(instance);
                if (referenceInstance == null)
                {
                    referenceInstance = dicomInstance;
                }
                else if (dicomInstance.StudyInstanceUID != referenceInstance.StudyInstanceUID ||
                            dicomInstance.SeriesInstanceUID != referenceInstance.SeriesInstanceUID)
                {
                    throw new ArgumentException("Not all of the provided instances belong to the same study or series.", nameof(instances));
                }
            }

            var defaultDocument = new QuerySeriesDocument(referenceInstance.StudyInstanceUID, referenceInstance.SeriesInstanceUID);
            RequestOptions requestOptions = CreateRequestOptions(defaultDocument.PartitionKey);

            // Retry when the pre-condition fails on replace (ETag check).
            IAsyncPolicy retryPolicy = CreatePreConditionFailedRetryPolicy();
            await _documentClient.CatchClientExceptionAndThrowDataStoreException(
                async (documentClient) =>
                {
                    QuerySeriesDocument document = await documentClient.GetOrCreateDocumentAsync(_databaseId, _collectionId, defaultDocument.Id, requestOptions, defaultDocument, cancellationToken);

                    foreach (DicomDataset instance in instances)
                    {
                        var queryInstance = QueryInstance.Create(instance, _dicomConfiguration.QueryAttributes);

                        if (!document.AddInstance(queryInstance))
                        {
                            throw new DataStoreException(HttpStatusCode.Conflict);
                        }
                    }

                    // Note, we do a replace (rather than upsert) in case the document is deleted.
                    requestOptions.AccessCondition = new AccessCondition() { Condition = document.ETag, Type = AccessConditionType.IfMatch };

                    Uri documentUri = UriFactory.CreateDocumentUri(_databaseId, _collectionId, document.Id);
                    await documentClient.ReplaceDocumentAsync(documentUri, document, requestOptions, cancellationToken: cancellationToken);
                },
                retryPolicy);
        }

        /// <inheritdoc />
        public async Task<QueryResult<DicomStudy>> QueryStudiesAsync(
            int offset, int limit, string studyInstanceUID = null, IEnumerable<(DicomAttributeId Attribute, string Value)> query = null, CancellationToken cancellationToken = default)
        {
            SqlQuerySpec sqlQuerySpec = _queryBuilder.BuildStudyQuerySpec(offset, limit, query);
            return await ExecuteQueryAsync<DicomStudy>(sqlQuerySpec, studyInstanceUID, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<QueryResult<DicomSeries>> QuerySeriesAsync(
            int offset, int limit, string studyInstanceUID = null, IEnumerable<(DicomAttributeId Attribute, string Value)> query = null, CancellationToken cancellationToken = default)
        {
            SqlQuerySpec sqlQuerySpec = _queryBuilder.BuildSeriesQuerySpec(offset, limit, query);
            return await ExecuteQueryAsync<DicomSeries>(sqlQuerySpec, studyInstanceUID, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<QueryResult<DicomInstance>> QueryInstancesAsync(
            int offset, int limit, string studyInstanceUID = null, IEnumerable<(DicomAttributeId Attribute, string Value)> query = null, CancellationToken cancellationToken = default)
        {
            SqlQuerySpec sqlQuerySpec = _queryBuilder.BuildInstanceQuerySpec(offset, limit, query);
            return await ExecuteQueryAsync<DicomInstance>(sqlQuerySpec, studyInstanceUID, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DicomInstance>> DeleteStudyIndexAsync(string studyInstanceUID, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID, nameof(studyInstanceUID));

            string partitionKey = QuerySeriesDocument.GetPartitionKey(studyInstanceUID);
            IDocumentQuery<QuerySeriesDocument> studyQuery = _documentClient.CreateDocumentQuery<QuerySeriesDocument>(_collectionUri, CreateFeedOptions(partitionKey))
                                                                            .AsDocumentQuery();
            var deletedInstances = new List<DicomInstance>();
            var deleteDocuments = new List<IDocument>();

            // Retry when the pre-condition fails on delete.
            IAsyncPolicy retryPolicy = CreatePreConditionFailedRetryPolicy();

            while (studyQuery.HasMoreResults)
            {
                await _documentClient.CatchClientExceptionAndThrowDataStoreException(
                    async (documentClient) =>
                    {
                        FeedResponse<QuerySeriesDocument> seriesDocuments = await studyQuery.ExecuteNextAsync<QuerySeriesDocument>(cancellationToken);

                        foreach (QuerySeriesDocument seriesDocument in seriesDocuments)
                        {
                            deleteDocuments.Add(seriesDocument);
                            deletedInstances.AddRange(seriesDocument.Instances.Select(x => new DicomInstance(seriesDocument.StudyUID, seriesDocument.SeriesUID, x.InstanceUID)));
                        }
                    },
                    retryPolicy);
            }

            // After searching, no matching items were found.
            if (deleteDocuments.Count == 0)
            {
                throw new DataStoreException(HttpStatusCode.NotFound);
            }

            // Delete all the series using a stored procedure (single transaction).
            var deleteStoredProcedure = new DeleteStoredProcedure();
            await _documentClient.CatchClientExceptionAndThrowDataStoreException(
                x => deleteStoredProcedure.Execute(x, _databaseId, _collectionId, partitionKey, deleteDocuments, cancellationToken),
                retryPolicy);

            return deletedInstances;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DicomInstance>> DeleteSeriesIndexAsync(
            string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID, nameof(studyInstanceUID));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUID, nameof(seriesInstanceUID));

            Uri documentUri = UriFactory.CreateDocumentUri(_databaseId, _collectionId, QuerySeriesDocument.GetDocumentId(studyInstanceUID, seriesInstanceUID));
            RequestOptions requestOptions = CreateRequestOptions(QuerySeriesDocument.GetPartitionKey(studyInstanceUID));

            // Retry when the pre-condition fails on delete.
            IAsyncPolicy retryPolicy = CreatePreConditionFailedRetryPolicy();
            return await _documentClient.CatchClientExceptionAndThrowDataStoreException(
                async (documentClient) =>
                {
                    DocumentResponse<QuerySeriesDocument> response =
                            await documentClient.ReadDocumentAsync<QuerySeriesDocument>(documentUri, requestOptions, cancellationToken: cancellationToken);

                    // Update the ETag check on the request options.
                    requestOptions.AccessCondition = new AccessCondition() { Condition = response.Document.ETag, Type = AccessConditionType.IfMatch };

                    // Delete the entire series document; if this fails the entire read and delete should be retried.
                    await documentClient.DeleteDocumentAsync(documentUri, requestOptions, cancellationToken);

                    return response.Document.Instances
                                            .Select(x => new DicomInstance(response.Document.StudyUID, response.Document.SeriesUID, x.InstanceUID))
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

            Uri documentUri = UriFactory.CreateDocumentUri(_databaseId, _collectionId, QuerySeriesDocument.GetDocumentId(studyInstanceUID, seriesInstanceUID));
            RequestOptions requestOptions = CreateRequestOptions(QuerySeriesDocument.GetPartitionKey(studyInstanceUID));

            IAsyncPolicy retryPolicy = CreatePreConditionFailedRetryPolicy();
            await _documentClient.CatchClientExceptionAndThrowDataStoreException(
                async (documentClient) =>
                {
                    DocumentResponse<QuerySeriesDocument> response =
                            await documentClient.ReadDocumentAsync<QuerySeriesDocument>(documentUri, requestOptions, cancellationToken: cancellationToken);

                    // If the instance does not exist, throw not found exception.
                    if (!response.Document.RemoveInstance(sopInstanceUID))
                    {
                        throw new DataStoreException(HttpStatusCode.NotFound);
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

        private RequestOptions CreateRequestOptions(string partitionKey)
            => new RequestOptions() { PartitionKey = new PartitionKey(partitionKey) };

        private FeedOptions CreateFeedOptions(string partitionKey)
        {
            EnsureArg.IsNotNull(partitionKey, nameof(partitionKey));
            return new FeedOptions() { PartitionKey = new PartitionKey(partitionKey), EnableCrossPartitionQuery = false };
        }

        private FeedOptions CreateCrossPartitionFeedOptions()
        {
            var feedOptions = new FeedOptions() { EnableCrossPartitionQuery = true };

            // TODO: This is a reflection hack to enable cross partition skip and take. This will be in the latest SDK soon.
            PropertyInfo propertyInfo = feedOptions.GetType().GetProperty("EnableCrossPartitionSkipTake", BindingFlags.NonPublic | BindingFlags.Instance);
            propertyInfo.SetValue(feedOptions, Convert.ChangeType(true, propertyInfo.PropertyType));

            return feedOptions;
        }

        private async Task<QueryResult<TResult>> ExecuteQueryAsync<TResult>(SqlQuerySpec sqlQuerySpec, string studyInstanceUID, CancellationToken cancellationToken)
        {
            // If the study instance UID is provided we can run the query against a specific partition.
            FeedOptions feedOptions = string.IsNullOrWhiteSpace(studyInstanceUID) ? CreateCrossPartitionFeedOptions() : CreateFeedOptions(studyInstanceUID);
            IDocumentQuery<QuerySeriesDocument> documentQuery = _documentClient
                                    .CreateDocumentQuery<QuerySeriesDocument>(_collectionUri, sqlQuerySpec, feedOptions)
                                    .AsDocumentQuery();

            var results = new List<TResult>();
            while (documentQuery.HasMoreResults)
            {
                // Each loop has its own retry handler so we don't retry the entire query on failure.
                await _documentClient.CatchClientExceptionAndThrowDataStoreException(
                    async (documentClient) =>
                    {
                        FeedResponse<TResult> nextResults = await documentQuery.ExecuteNextAsync<TResult>(cancellationToken);
                        results.AddRange(nextResults);
                    });
            }

            return new QueryResult<TResult>(documentQuery.HasMoreResults, results);
        }

        private IAsyncPolicy CreatePreConditionFailedRetryPolicy()
            => Policy
                    .Handle<DocumentClientException>(ex => ex.StatusCode == HttpStatusCode.PreconditionFailed || ex.StatusCode == HttpStatusCode.TooManyRequests)
                    .WaitAndRetryForeverAsync(
                            (retryIndex, ex, context) =>
                            {
                                // Use RetryAfter header when TooManyRequests response is returned.
                                if (ex is DocumentClientException doc && doc.StatusCode == HttpStatusCode.TooManyRequests)
                                {
                                    return doc.RetryAfter;
                                }

                                // Otherwise, delay retry with some randomness.
                                return TimeSpan.FromMilliseconds((retryIndex - 1) * _random.Next(200, 500));
                            },
                            (exception, retryIndex, timespan, context) => Task.CompletedTask);
    }
}
