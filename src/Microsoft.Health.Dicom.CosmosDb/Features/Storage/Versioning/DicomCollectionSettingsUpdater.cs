// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.CosmosDb.Configs;
using Microsoft.Health.CosmosDb.Features.Storage.Versioning;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage.Versioning
{
    public class DicomCollectionSettingsUpdater : IDicomCollectionUpdater
    {
        private readonly ILogger<DicomCollectionSettingsUpdater> _logger;
        private readonly CosmosDataStoreConfiguration _configuration;
        private readonly CosmosCollectionConfiguration _collectionConfiguration;
        private const int CollectionSettingsVersion = 1;

        public DicomCollectionSettingsUpdater(
            CosmosDataStoreConfiguration configuration,
            IOptionsMonitor<CosmosCollectionConfiguration> namedCosmosCollectionConfigurationAccessor,
            ILogger<DicomCollectionSettingsUpdater> logger)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(namedCosmosCollectionConfigurationAccessor, nameof(namedCosmosCollectionConfigurationAccessor));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _configuration = configuration;
            _collectionConfiguration = namedCosmosCollectionConfigurationAccessor.Get(Constants.CollectionConfigurationName);
            _logger = logger;
        }

        public async Task ExecuteAsync(IDocumentClient client, DocumentCollection collection, Uri relativeCollectionUri)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(collection, nameof(collection));

            CollectionVersion thisVersion = await GetLatestCollectionVersion(client, collection);

            if (thisVersion.Version < CollectionSettingsVersion)
            {
                _logger.LogDebug("Ensuring indexes are up-to-date {CollectionUri}", _configuration.GetAbsoluteCollectionUri(_collectionConfiguration.CollectionId));

                collection.IndexingPolicy = new IndexingPolicy
                {
                    IndexingMode = IndexingMode.Consistent,
                    Automatic = true,
                    IncludedPaths = new Collection<IncludedPath>
                    {
                        new IncludedPath
                        {
                            Path = "/*",
                            Indexes = new Collection<Index>(),
                        },
                    },
                    ExcludedPaths = new Collection<ExcludedPath>()
                    {
                        new ExcludedPath
                        {
                            Path = "/\"_etag\"/?",
                        },
                    },
                };

                // Setting the DefaultTTL to -1 means that by default all documents in the collection will live forever
                // but the Cosmos DB service should monitor this collection for documents that have overridden this default.
                // See: https://docs.microsoft.com/en-us/azure/cosmos-db/time-to-live
                collection.DefaultTimeToLive = -1;

                await client.ReplaceDocumentCollectionAsync(collection);

                thisVersion.Version = CollectionSettingsVersion;
                await client.UpsertDocumentAsync(relativeCollectionUri, thisVersion);
            }
        }

        private static async Task<CollectionVersion> GetLatestCollectionVersion(IDocumentClient documentClient, DocumentCollection collection)
        {
            IDocumentQuery<CollectionVersion> query = documentClient.CreateDocumentQuery<CollectionVersion>(
                    collection.SelfLink,
                    new SqlQuerySpec("SELECT * FROM root r"),
                    new FeedOptions { PartitionKey = new PartitionKey(CollectionVersion.CollectionVersionPartition) })
                .AsDocumentQuery();

            FeedResponse<CollectionVersion> result = await query.ExecuteNextAsync<CollectionVersion>();

            return result.FirstOrDefault() ?? new CollectionVersion();
        }
    }
}
