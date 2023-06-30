// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text.Json;
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
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;

/// <summary>
/// Provides functionality for managing DICOM Azure durable function input.
/// </summary>
public class BlobSystemStore : ISystemStore
{
    private const string AddInput = nameof(BlobSystemStore) + "." + nameof(StoreInputAsync);

    private readonly BlobContainerClient _container;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly ILogger<BlobSystemStore> _logger;

    public BlobSystemStore(
        BlobServiceClient client,
        IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        IOptions<JsonSerializerOptions> jsonSerializerOptions,
        ILogger<BlobSystemStore> logger)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        _jsonSerializerOptions = EnsureArg.IsNotNull(jsonSerializerOptions?.Value, nameof(jsonSerializerOptions));
        _recyclableMemoryStreamManager = EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));

        var containerConfiguration = namedBlobContainerConfigurationAccessor
            .Get(Constants.SystemContainerConfigurationName);

        _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
    }

    public async Task<string> StoreInputAsync<T>(T input, CancellationToken cancellationToken = default) where T : class
    {
        EnsureArg.IsNotNull(input, nameof(input));

        var blob = GetBlockBlobClient();

        try
        {
            await using Stream stream = _recyclableMemoryStreamManager.GetStream(AddInput);
            await JsonSerializer.SerializeAsync(stream, input, _jsonSerializerOptions, cancellationToken);

            // Uploads the blob. Overwrites the blob if it exists, otherwise creates a new one.
            stream.Seek(0, SeekOrigin.Begin);
            await blob.UploadAsync(
                stream,
                new BlobHttpHeaders { ContentType = KnownContentTypes.ApplicationJsonUtf8 },
                metadata: null,
                conditions: null,
                accessTier: null,
                progressHandler: null,
                cancellationToken);

            return blob.Name;
        }
        catch (Exception ex)
        {
            throw new DataStoreException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<TResult> GetInputAsync<TResult>(string name, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(name, nameof(name));

        BlockBlobClient blobClient = GetBlockBlobClient(name);

        try
        {
            BlobDownloadResult result = await blobClient.DownloadContentAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<TResult>(result.Content.ToStream(), _jsonSerializerOptions, cancellationToken);
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

    private BlockBlobClient GetBlockBlobClient(string name = null)
    {
        string blobName = string.IsNullOrEmpty(name) ? $"{Guid.NewGuid():N}.json" : name;
        return _container.GetBlockBlobClient(blobName);
    }
}

