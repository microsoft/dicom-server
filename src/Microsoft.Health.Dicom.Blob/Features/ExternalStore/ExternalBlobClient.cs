// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using System;
using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Blob.Utilities;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Core.Features.Identity;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Blob.Features.ExternalStore;

/// Represents the blob container created by the user and initialized JIT
internal class ExternalBlobClient : IBlobClient
{
    private readonly object _lockObj = new object();
    private readonly BlobServiceClientOptions _blobClientOptions;
    private readonly ExternalBlobDataStoreConfiguration _externalStoreOptions;
    private readonly IExternalCredentialProvider _credentialProvider;
    private BlobContainerClient _blobContainerClient;
    private readonly bool _isPartitionEnabled;
    private readonly ILogger _logger;

    /// <summary>
    /// Configures a blob client for an external store.
    /// </summary>
    /// <param name="credentialProvider"></param>
    /// <param name="externalStoreOptions">Options to use with configuring the external store.</param>
    /// <param name="blobClientOptions">Options to use when configuring the blob client.</param>
    /// <param name="featureConfiguration">Feature configuration.</param>
    /// <param name="logger">A logger for diagnostic information.</param>
    public ExternalBlobClient(
        IExternalCredentialProvider credentialProvider,
        IOptions<ExternalBlobDataStoreConfiguration> externalStoreOptions,
        IOptions<BlobServiceClientOptions> blobClientOptions,
        IOptions<FeatureConfiguration> featureConfiguration,
        ILogger<ExternalBlobClient> logger)
    {
        _credentialProvider = EnsureArg.IsNotNull(credentialProvider, nameof(credentialProvider));
        _blobClientOptions = EnsureArg.IsNotNull(blobClientOptions?.Value, nameof(blobClientOptions));
        _externalStoreOptions = EnsureArg.IsNotNull(externalStoreOptions?.Value, nameof(externalStoreOptions));
        _externalStoreOptions.StorageDirectory = SanitizeServiceStorePath(_externalStoreOptions.StorageDirectory);
        _isPartitionEnabled = EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration)).Value.EnableDataPartitions;
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _logger.LogInformation("External blob client registered. Partition feature flag is set to {IsPartitionEnabled}", _isPartitionEnabled);

        ExternalStoreHealthExpiryHttpPipelinePolicy httpPolicy = new ExternalStoreHealthExpiryHttpPipelinePolicy(_externalStoreOptions);
        _blobClientOptions.AddPolicy(httpPolicy, HttpPipelinePosition.PerCall);
    }

    public bool IsExternal => true;

    public BlobContainerClient BlobContainerClient
    {
        get
        {
            if (_blobContainerClient == null)
            {
                lock (_lockObj)
                {
                    if (_blobContainerClient == null)
                    {
                        try
                        {
                            if (_externalStoreOptions.BlobContainerUri != null)
                            {
                                TokenCredential credential = _credentialProvider.GetTokenCredential();
                                _blobContainerClient = new BlobContainerClient(_externalStoreOptions.BlobContainerUri, credential, _blobClientOptions);
                            }
                            else
                            {
                                _blobContainerClient = new BlobContainerClient(_externalStoreOptions.ConnectionString, _externalStoreOptions.ContainerName, _blobClientOptions);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new DataStoreException(ex, isExternal: true);
                        }
                    }
                }
            }
            return _blobContainerClient;
        }
        set => _blobContainerClient = value;
    }

    private static string SanitizeServiceStorePath(string path)
    {
        return !path.EndsWith('/') ? path + "/" : path;
    }

    /// <summary>
    /// Gets path to store blobs in. When partitioning is enabled, the path appends partition as a subdirectory.
    /// </summary>
    /// <param name="partitionName">Partition name to use to append as subdirectory to prefix.</param>
    /// <returns></returns>
    public string GetServiceStorePath(string partitionName)
    {
        return _isPartitionEnabled ?
            _externalStoreOptions.StorageDirectory + partitionName + "/" :
            _externalStoreOptions.StorageDirectory;
    }

    public BlobRequestConditions GetConditions(FileProperties fileProperties)
    {
        if (fileProperties != null)
        {
            return new BlobRequestConditions // ensure file has not been changed since we last worked with it
            {
                IfMatch = new ETag(fileProperties.ETag),
            };
        }

        return null;
    }
}
