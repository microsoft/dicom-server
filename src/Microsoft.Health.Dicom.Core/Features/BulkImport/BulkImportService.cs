// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;

namespace Microsoft.Health.Dicom.Core.Features.BulkImport
{
    public class BulkImportService : IBulkImportService
    {
        private readonly IBulkImportDataStore _bulkImportDataStore;

        public BulkImportService(IBulkImportDataStore bulkImportDataStore)
        {
            _bulkImportDataStore = bulkImportDataStore;
        }

        public async Task RetrieveInitialBlobsAsync(string accountName, CancellationToken cancellationToken)
        {
            var storageAccountUri = new Uri($"https://{accountName}.blob.core.windows.net");

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync(storageAccountUri.ToString());

            CloudBlobClient client = new CloudBlobClient(storageAccountUri, new StorageCredentials(new TokenCredential(accessToken)));

            var containers = new List<CloudBlobContainer>();
            BlobContinuationToken continuationToken = null;

            do
            {
                ContainerResultSegment containerResultSegment = await client.ListContainersSegmentedAsync(continuationToken, cancellationToken);

                if (containerResultSegment == null)
                {
                    break;
                }

                containers.AddRange(containerResultSegment.Results);

                continuationToken = containerResultSegment.ContinuationToken;
            }
            while (continuationToken != null);

            foreach (CloudBlobContainer container in containers)
            {
                continuationToken = null;

                do
                {
                    BlobResultSegment blobResultSegment = await container.ListBlobsSegmentedAsync(
                        prefix: string.Empty,
                        useFlatBlobListing: true,
                        blobListingDetails: BlobListingDetails.None,
                        maxResults: null,
                        currentToken: continuationToken,
                        options: null,
                        operationContext: null,
                        cancellationToken);

                    if (blobResultSegment == null)
                    {
                        continue;
                    }

                    IReadOnlyList<BlobReference> references = blobResultSegment.Results.Cast<CloudBlockBlob>().Select(
                        result => new BlobReference(result.Container.Name, result.Name)).ToArray();

                    await _bulkImportDataStore.QueueBulkImportEntriesAsync(accountName, references, cancellationToken);

                    continuationToken = blobResultSegment.ContinuationToken;
                }
                while (continuationToken != null);
            }
        }

        public async Task QueueEntriesAsync(string accountName, IReadOnlyList<string> blobNames, CancellationToken cancellationToken)
        {
            ////await _bulkImportDataStore.QueueEntriesAsync(accountName, blobNames, cancellationToken);
            await Task.Delay(0);
        }
    }
}
