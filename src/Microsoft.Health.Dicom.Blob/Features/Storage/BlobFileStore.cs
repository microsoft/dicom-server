// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Blob.Features.Storage
{
    /// <summary>
    /// Provides functionality for managing the DICOM files using the Azure Blob storage.
    /// </summary>
    public class BlobFileStore : IFileStore
    {
        private const string GetFileStreamTagName = nameof(BlobFileStore) + "." + nameof(GetFileAsync);
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private readonly ILogger<BlobFileStore> _logger;
        private readonly BlobContainerClient _container;
        private readonly BlobOperationOptions _options;

        public BlobFileStore(
            BlobServiceClient client,
            RecyclableMemoryStreamManager recyclableMemoryStreamManager,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            IOptions<BlobOperationOptions> options,
            ILogger<BlobFileStore> logger)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
            EnsureArg.IsNotNull(options?.Value, nameof(options));
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

            BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationName);

            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
            _logger = logger;
            _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
            _options = options.Value;
        }

        /// <inheritdoc />
        public async Task<Uri> StoreFileAsync(
            VersionedInstanceIdentifier versionedInstanceIdentifier,
            Stream stream,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
            EnsureArg.IsNotNull(stream, nameof(stream));

            BlockBlobClient blob = GetInstanceBlockBlob(versionedInstanceIdentifier);
            stream.Seek(0, SeekOrigin.Begin);

            var blobUploadOptions = new BlobUploadOptions { TransferOptions = _options.Upload };

            try
            {
                await blob.UploadAsync(
                    stream,
                    blobUploadOptions,
                    cancellationToken);

                return blob.Uri;
            }
            catch (Exception ex)
            {
                throw new DataStoreException(ex);
            }
        }

        /// <inheritdoc />
        public async Task DeleteFileIfExistsAsync(
            VersionedInstanceIdentifier versionedInstanceIdentifier,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

            BlockBlobClient blob = GetInstanceBlockBlob(versionedInstanceIdentifier);

            await ExecuteAsync(() => blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, cancellationToken));
        }

        /// <inheritdoc />
        public async Task<Stream> GetFileAsync(
            VersionedInstanceIdentifier versionedInstanceIdentifier,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

            BlockBlobClient blob = GetInstanceBlockBlob(versionedInstanceIdentifier);

            Stream stream = null;
            var blobOpenReadOptions = new BlobOpenReadOptions(allowModifications: false);

            await ExecuteAsync(async () =>
            {
                stream = await blob.OpenReadAsync(blobOpenReadOptions, cancellationToken);
            });

            return stream;
        }

        /// <inheritdoc />
        public async Task<Stream> GetFileFrameAsync(
            VersionedInstanceIdentifier versionedInstanceIdentifier,
            FrameRange range,
            CancellationToken cancellationToken)
        {
            var stopWatch = Stopwatch.StartNew();
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

            BlockBlobClient blob = GetInstanceBlockBlob(versionedInstanceIdentifier);

            Stream stream = null;
            var blobOpenReadOptions = new BlobOpenReadOptions(allowModifications: false);

            await ExecuteAsync(async () =>
            {
                stream = _recyclableMemoryStreamManager.GetStream(GetFileStreamTagName);
                var httpRange = new HttpRange(range.Offset, range.Length);
                Response<BlobDownloadStreamingResult> result = await blob.DownloadStreamingAsync(httpRange, conditions: null, rangeGetContentHash: false, cancellationToken);
                result.Value.Content.CopyTo(stream);
                stream.Seek(0, SeekOrigin.Begin);
                result.Value.Content.Dispose();
            });
            stopWatch.Stop();
            _logger.LogInformation("BlobFileStore:GetFileFrameAsync: {0}", stopWatch.ElapsedMilliseconds);
            return stream;
        }

        private BlockBlobClient GetInstanceBlockBlob(VersionedInstanceIdentifier versionedInstanceIdentifier)
        {
            string blobName = $"{versionedInstanceIdentifier.StudyInstanceUid}/{versionedInstanceIdentifier.SeriesInstanceUid}/{versionedInstanceIdentifier.SopInstanceUid}_{versionedInstanceIdentifier.Version}.dcm";

            return _container.GetBlockBlobClient(blobName);
        }

        private static async Task ExecuteAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobNotFound)
            {
                throw new ItemNotFoundException(ex);
            }
            catch (Exception ex)
            {
                throw new DataStoreException(ex);
            }
        }
    }
}
