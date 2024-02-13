// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Health.Dicom.Blob.Utilities;

namespace Microsoft.Health.Dicom.Blob.Features.ExternalStore;

internal class ExternalStoreHealthExpiryHttpPipelinePolicy : HttpPipelinePolicy
{
    private readonly ExternalBlobDataStoreConfiguration _externalStoreOptions;
    private readonly Regex _healthCheckRegex;
    private const string GuidRegex = @"[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}";

    public ExternalStoreHealthExpiryHttpPipelinePolicy(ExternalBlobDataStoreConfiguration externalStoreOptions)
    {
        _externalStoreOptions = EnsureArg.IsNotNull(externalStoreOptions, nameof(externalStoreOptions));
        EnsureArg.IsNotNull(externalStoreOptions.StorageDirectory, nameof(externalStoreOptions.StorageDirectory));
        EnsureArg.IsNotNull(externalStoreOptions.HealthCheckFilePath, nameof(externalStoreOptions.HealthCheckFilePath));

        Uri blobUri;

        if (_externalStoreOptions.BlobContainerUri != null)
        {
            blobUri = _externalStoreOptions.BlobContainerUri;
        }
        else
        {
            // For local testing with Azurite
            BlobContainerClient blobContainerClient = new BlobContainerClient(_externalStoreOptions.ConnectionString, _externalStoreOptions.ContainerName);
            blobUri = blobContainerClient.Uri;
        }

        UriBuilder uriBuilder = new UriBuilder(blobUri);
        uriBuilder.Path = Path.Combine(uriBuilder.Path, _externalStoreOptions.StorageDirectory, _externalStoreOptions.HealthCheckFilePath);

        string healthCheckPathRegex = Regex.Escape(uriBuilder.Uri.AbsoluteUri);
        _healthCheckRegex = new Regex($"^{healthCheckPathRegex}{GuidRegex}\\.txt$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }

    public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        AddExpiryHeaderToHealthCheckFileUploadRequest(message);
        ProcessNext(message, pipeline);
    }

    public override ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        AddExpiryHeaderToHealthCheckFileUploadRequest(message);
        return ProcessNextAsync(message, pipeline);
    }

    private void AddExpiryHeaderToHealthCheckFileUploadRequest(HttpMessage message)
    {
        if (_healthCheckRegex.IsMatch(message.Request.Uri.ToUri().AbsoluteUri) &&
        (message.Request.Method == RequestMethod.Put || message.Request.Method == RequestMethod.Post || message.Request.Method == RequestMethod.Patch))
        {
            message.Request.Headers.Add("x-ms-expiry-time", _externalStoreOptions.HealthCheckFileExpiry.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
            message.Request.Headers.Add("x-ms-expiry-option", "RelativeToNow");
        }
    }
}
