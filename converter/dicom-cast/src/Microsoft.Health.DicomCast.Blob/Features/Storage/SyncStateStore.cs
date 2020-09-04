// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.DicomCast.Core.Exceptions;
using Microsoft.Health.DicomCast.Core.Features.State;
using Newtonsoft.Json;

namespace Microsoft.Health.DicomCast.Blob.Features.Storage
{
    public class SyncStateStore : ISyncStateStore
    {
        private const string JsonContentType = "application/json";
        private const string SyncStateBlobName = "SyncState.json";
        private static readonly Encoding _jsonTextEncoding = Encoding.UTF8;
        private readonly BlobContainerClient _container;

        public SyncStateStore(
            BlobServiceClient client,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));

            BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationOptionsName);
            EnsureArg.IsNotNull(containerConfiguration);

            _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
        }

        public async Task<SyncState> ReadAsync(CancellationToken cancellationToken)
        {
            BlockBlobClient blob = GetBlockBlobClient();
            if (await blob.ExistsAsync(cancellationToken))
            {
                string syncStateString;
                try
                {
                    await using (var stream = new MemoryStream())
                    {
                        await blob.DownloadToAsync(stream, cancellationToken);
                        syncStateString = _jsonTextEncoding.GetString(stream.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    throw new DataStoreException(ex);
                }

                return JsonConvert.DeserializeObject<SyncState>(syncStateString);
            }

            return SyncState.CreateInitialSyncState();
        }

        public async Task UpdateAsync(SyncState state, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(state);

            var syncStateString = JsonConvert.SerializeObject(state);
            BlockBlobClient blob = GetBlockBlobClient();

            try
            {
                await using (var stream = new MemoryStream(_jsonTextEncoding.GetBytes(syncStateString)))
                {
                    await blob.UploadAsync(
                        stream,
                        new BlobHttpHeaders()
                        {
                            ContentType = JsonContentType,
                        },
                        metadata: null,
                        conditions: null,
                        accessTier: null,
                        progressHandler: null,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new DataStoreException(ex);
            }
        }

        private BlockBlobClient GetBlockBlobClient()
        {
            return _container.GetBlockBlobClient(SyncStateBlobName);
        }
    }
}
