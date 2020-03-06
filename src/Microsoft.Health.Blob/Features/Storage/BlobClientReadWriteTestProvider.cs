// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Health.Blob.Configs;
using Microsoft.IO;

namespace Microsoft.Health.Blob.Features.Storage
{
    public class BlobClientReadWriteTestProvider : IBlobClientTestProvider
    {
        private const string TestBlobName = "_testblob_";
        private static readonly byte[] TestBlobContent = new byte[] { 1 };
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public BlobClientReadWriteTestProvider(RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        }

        public async Task PerformTestAsync(CloudBlobClient blobClient, BlobDataStoreConfiguration configuration, BlobContainerConfiguration blobContainerConfiguration, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(blobClient, nameof(blobClient));
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(blobContainerConfiguration, nameof(blobContainerConfiguration));

            CloudBlobContainer blobContainer = blobClient.GetContainerReference(blobContainerConfiguration.ContainerName);
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(TestBlobName);

            await blob.UploadFromByteArrayAsync(TestBlobContent, 0, TestBlobContent.Length, AccessCondition.GenerateEmptyCondition(), new BlobRequestOptions(), new OperationContext(), cancellationToken);
            await DownloadBlobContentAsync(blob, cancellationToken);
        }

        private async Task<byte[]> DownloadBlobContentAsync(CloudBlockBlob blob, CancellationToken cancellationToken)
        {
            await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
            {
                await blob.DownloadToStreamAsync(stream, cancellationToken);
                return stream.ToArray();
            }
        }
    }
}
