// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------


using Azure.Core;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob.Utilities;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core.Exceptions;
using System;
using System.Linq;
using Microsoft.Health.Dicom.Core;

namespace Microsoft.Health.Dicom.Blob.Features.ExternalStore;

/// Represents the blob container created by the user and initialized JIT
internal class ExternalBlobClient : IBlobClient
{
    private const int MaxBlobNameLength = 1024;
    private const int MaxBlobSegmentsAllowed = 254;
    private readonly object _lockObj = new object();
    private readonly BlobServiceClientOptions _blobClientOptions;
    private readonly ExternalBlobDataStoreConfiguration _externalStoreOptions;
    private readonly IExternalOperationCredentialProvider _credentialProvider;
    private BlobContainerClient _blobContainerClient;
    private readonly char[] _allowedChars = new[] { '.', '/', '-' };

    /// <summary>
    /// Configures a blob client for an external store.
    /// </summary>
    /// <param name="credentialProvider"></param>
    /// <param name="externalStoreOptions">Options to use with configuring the external store.</param>
    /// <param name="blobClientOptions">Options to use when configuring the blob client.</param>
    public ExternalBlobClient(
        IExternalOperationCredentialProvider credentialProvider,
        IOptions<ExternalBlobDataStoreConfiguration> externalStoreOptions,
        IOptions<BlobServiceClientOptions> blobClientOptions)
    {
        _credentialProvider = EnsureArg.IsNotNull(credentialProvider, nameof(credentialProvider));
        _blobClientOptions = EnsureArg.IsNotNull(blobClientOptions?.Value, nameof(blobClientOptions));
        _externalStoreOptions = EnsureArg.IsNotNull(externalStoreOptions?.Value, nameof(externalStoreOptions));
        _externalStoreOptions.StorageDirectory = SanitizeServiceStorePath(_externalStoreOptions.StorageDirectory);
    }

    public bool IsExternal => true;

    public string ServiceStorePath => _externalStoreOptions.StorageDirectory;

    public BlobContainerClient BlobContainerClient
    {
        get
        {
            EnsureValidConfiguration();
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
    }

    private static string SanitizeServiceStorePath(string path)
    {
        return !path.EndsWith("/", StringComparison.OrdinalIgnoreCase) ? path + "/" : path;
    }

    /// <summary>
    /// See https://learn.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata#blob-names
    /// </summary>
    private void ServiceStorePathContainsInvalidCharactersCheck()
    {
        // Reserved URL characters must be properly escaped.
        // https://www.rfc-editor.org/rfc/rfc3986#section-2.2
        // reserved    = gen-delims / sub-delims
        // gen-delims  = ":" / "/" / "?" / "#" / "[" / "]" / "@"
        // sub-delims  = "!" / "$" / "&" / "'" / "(" / ")"
        //               / "*" / "+" / "," / ";" / "="
        if (_externalStoreOptions.StorageDirectory.
            Select(x => !char.IsLetterOrDigit(x) && !_allowedChars.Contains(x)).
            Any(x => x is true))
        {
            throw new DataStoreException(DicomCoreResource.ExternalDataStoreInvalidCharactersInServiceStorePath, isExternal: true);
        }
    }

    /// <summary>
    /// See https://learn.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata#blob-names
    /// </summary>
    private void ServiceStorePathSegmentInvalidCheck()
    {
        // If your account does not have a hierarchical namespace, then the number of path segments comprising the blob
        // name cannot exceed 254. A path segment is the string between consecutive delimiter characters
        // (e.g., the forward slash '/') that corresponds to the name of a virtual directory.
        if (_externalStoreOptions.StorageDirectory.Count(c => c == '/') > MaxBlobSegmentsAllowed)
        {
            throw new DataStoreException(DicomCoreResource.ExternalDataStoreInvalidServiceStorePathSegments, isExternal: true);
        }
    }

    /// <summary>
    /// This validation necessary only for OSS as the path will be specified for managed services.
    /// </summary>
    /// <exception cref="DataStoreException"></exception>
    private void EnsureValidConfiguration()
    {
        // A blob name must be at least one character long and cannot be more than 1,024 characters long
        if (_externalStoreOptions.StorageDirectory.Length is 0 or > MaxBlobNameLength)
        {
            throw new DataStoreException(DicomCoreResource.ExternalDataStoreBlobNameTooLong, isExternal: true);
        }

        ServiceStorePathContainsInvalidCharactersCheck();

        ServiceStorePathSegmentInvalidCheck();
    }
}
