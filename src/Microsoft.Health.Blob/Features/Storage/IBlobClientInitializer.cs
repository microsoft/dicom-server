// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Health.Blob.Features.Storage
{
    /// <summary>
    /// Provides methods for creating a CloudBlobClient instance and initializing containers.
    /// </summary>
    public interface IBlobClientInitializer
    {
        /// <summary>
        /// Creates an unopened <see cref="IDocumentClient"/> based on the given <see cref="BlobDataStoreConfiguration"/>.
        /// </summary>
        /// <param name="configuration">The connection string and requestion options.</param>
        /// <returns>A <see cref="CloudBlobClient"/> instance</returns>
        CloudBlobClient CreateBlobClient(BlobDataStoreConfiguration configuration);

        /// <summary>
        /// Perform a trivial query to establish a connection.
        /// DocumentClient.OpenAsync() is not supported when a token is used as the access key.
        /// </summary>
        /// <param name="client">The document client</param>
        /// <param name="configuration">The data store config</param>
        /// <param name="blobContainerConfiguration">The container configuration to use for validating the blob client is open</param>
        Task OpenBlobClientAsync(CloudBlobClient client, BlobDataStoreConfiguration configuration, BlobContainerConfiguration blobContainerConfiguration);

        /// <summary>
        /// Ensures that the necessary database and collection exist with the proper indexing policy and stored procedures
        /// </summary>
        /// <param name="client">The <see cref="CloudBlobClient"/> instance to use for initialization.</param>
        /// <param name="configuration">The data store configuration.</param>
        /// <param name="containerInitializers">The blob container initializers.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        Task InitializeDataStoreAsync(CloudBlobClient client, BlobDataStoreConfiguration configuration, IEnumerable<IBlobContainerInitializer> containerInitializers);
    }
}
