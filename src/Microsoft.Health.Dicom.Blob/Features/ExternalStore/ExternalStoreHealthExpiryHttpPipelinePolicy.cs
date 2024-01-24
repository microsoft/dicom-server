// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Pipeline;
using EnsureThat;
using Microsoft.Health.Dicom.Blob.Utilities;

namespace Microsoft.Health.Dicom.Blob.Features.ExternalStore;

internal class ExternalStoreHealthExpiryHttpPipelinePolicy : HttpPipelinePolicy
{
    private readonly ExternalBlobDataStoreConfiguration _externalStoreOptions;
    private readonly string _healthCheckPathRegex;
    private readonly string _txtRegex;
    private const string GuidRegex = @"[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}";

    public ExternalStoreHealthExpiryHttpPipelinePolicy(ExternalBlobDataStoreConfiguration externalStoreOptions)
    {
        _externalStoreOptions = EnsureArg.IsNotNull(externalStoreOptions, nameof(externalStoreOptions));
        EnsureArg.IsNotNull(externalStoreOptions.BlobContainerUri, nameof(externalStoreOptions.BlobContainerUri));
        EnsureArg.IsNotNull(externalStoreOptions.StorageDirectory, nameof(externalStoreOptions.StorageDirectory));
        EnsureArg.IsNotNull(externalStoreOptions.HealthCheckFilePath, nameof(externalStoreOptions.HealthCheckFilePath));

        UriBuilder uriBuilder = new UriBuilder(_externalStoreOptions.BlobContainerUri);
        uriBuilder.Path = Path.Combine(uriBuilder.Path, _externalStoreOptions.StorageDirectory, _externalStoreOptions.HealthCheckFilePath);

        _healthCheckPathRegex = Regex.Escape(uriBuilder.Uri.ToString());
        _txtRegex = Regex.Escape(".txt");
    }

    public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        if (IsHealthCheckFileUpload(message.Request))
        {
            message.Request.Headers.Add("x-ms-expiry-time", $"{_externalStoreOptions.HealthCheckFileExpiryInMs}");
            message.Request.Headers.Add("x-ms-expiry-option", "RelativeToNow");
        }

        ProcessNext(message, pipeline);
    }

    public override ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        if (IsHealthCheckFileUpload(message.Request))
        {
            message.Request.Headers.Add("x-ms-expiry-time", $"{_externalStoreOptions.HealthCheckFileExpiryInMs}");
            message.Request.Headers.Add("x-ms-expiry-option", "RelativeToNow");
        }

        return ProcessNextAsync(message, pipeline);
    }

    private bool IsHealthCheckFileUpload(Request request)
    {
        return Regex.IsMatch(request.Uri.ToString(), $"^{_healthCheckPathRegex}{GuidRegex}{_txtRegex}$") &&
            (request.Method == RequestMethod.Put || request.Method == RequestMethod.Post || request.Method == RequestMethod.Patch);
    }
}
