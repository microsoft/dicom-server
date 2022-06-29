// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

internal sealed class AzureBlobExportSinkProvider : ExportSinkProvider<AzureBlobExportOptions>
{
    public override ExportDestinationType Type => ExportDestinationType.AzureBlob;

    private readonly ISecretStore _secretStore;
    private readonly IFileStore _fileStore;
    private readonly IServerCredentialProvider _serverCredentialProvider;
    private readonly AzureBlobExportSinkProviderOptions _providerOptions;
    private readonly AzureBlobClientOptions _clientOptions;
    private readonly BlobOperationOptions _operationOptions;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ILogger _logger;

    public AzureBlobExportSinkProvider(
        IFileStore fileStore,
        IServerCredentialProvider serverCredentialProvider,
        IOptionsSnapshot<AzureBlobExportSinkProviderOptions> providerOptions,
        IOptionsSnapshot<AzureBlobClientOptions> clientOptions,
        IOptionsSnapshot<BlobOperationOptions> operationOptions,
        IOptionsSnapshot<JsonSerializerOptions> serializerOptions,
        ILogger<AzureBlobExportSinkProvider> logger)
    {
        _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
        _serverCredentialProvider = EnsureArg.IsNotNull(serverCredentialProvider, nameof(serverCredentialProvider));
        _providerOptions = EnsureArg.IsNotNull(providerOptions?.Value, nameof(providerOptions));
        _clientOptions = EnsureArg.IsNotNull(clientOptions?.Get("Export"), nameof(clientOptions));
        _operationOptions = EnsureArg.IsNotNull(operationOptions?.Value, nameof(operationOptions));
        _serializerOptions = EnsureArg.IsNotNull(serializerOptions?.Value, nameof(serializerOptions));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public AzureBlobExportSinkProvider(
        ISecretStore secretStore,
        IFileStore fileStore,
        IServerCredentialProvider serverCredentialProvider,
        IOptionsSnapshot<AzureBlobExportSinkProviderOptions> providerOptions,
        IOptionsSnapshot<AzureBlobClientOptions> clientOptions,
        IOptionsSnapshot<BlobOperationOptions> operationOptions,
        IOptionsSnapshot<JsonSerializerOptions> serializerOptions,
        ILogger<AzureBlobExportSinkProvider> logger)
        : this(fileStore, serverCredentialProvider, providerOptions, clientOptions, operationOptions, serializerOptions, logger)
    {
        // The real Azure Functions runtime/container will use DryIoc for dependency injection
        // and will still select this ctor for use, even if no ISecretStore service is configured.
        // So instead, we simply allow null in either ctor.
        _secretStore = secretStore;
    }

    protected override async Task CompleteCopyAsync(AzureBlobExportOptions options, CancellationToken cancellationToken = default)
    {
        if (_secretStore == null)
        {
            if (options.Secret != null)
                _logger.LogWarning("No secret store has been registered, but a secret was previously configured. Unable to clean up sensitive information.");
        }
        else if (options.Secret != null)
        {
            if (await _secretStore.DeleteSecretAsync(options.Secret.Name, cancellationToken))
                _logger.LogInformation("Successfully cleaned up sensitive information from secret store.");
            else
                _logger.LogWarning("Sensitive information has already been deleted for this operation.");
        }
    }

    protected override async Task<IExportSink> CreateAsync(AzureBlobExportOptions options, Guid operationId, CancellationToken cancellationToken = default)
    {
        options = await RetrieveSensitiveOptionsAsync(options, cancellationToken);

        return new AzureBlobExportSink(
            _fileStore,
            await options.GetBlobContainerClientAsync(_serverCredentialProvider, _clientOptions, cancellationToken),
            new AzureBlobExportFormatOptions(
                operationId,
                AzureBlobExportOptions.DicomFilePattern,
                AzureBlobExportOptions.ErrorLogPattern),
            _operationOptions,
            _serializerOptions);
    }

    protected override async Task<AzureBlobExportOptions> SecureSensitiveInfoAsync(AzureBlobExportOptions options, Guid operationId, CancellationToken cancellationToken = default)
    {
        // Clear secrets if it's already set
        if (options.Secret != null)
            options.Secret = null;

        // Determine whether we need to store any settings in the secret store
        BlobSecrets secrets = null;
        if (options.BlobContainerUri != null)
        {
            if (IsSensitiveBlobContainerUri(options.BlobContainerUri))
                secrets = new BlobSecrets { BlobContainerUri = options.BlobContainerUri };
        }
        else if (IsSensitiveConnectionString(options))
        {
            secrets = new BlobSecrets { ConnectionString = options.ConnectionString };
        }

        // If there is sensitive info, store the secret(s)
        if (secrets != null)
        {
            if (_secretStore == null)
                throw new InvalidOperationException(DicomBlobResource.MissingSecretStore);

            string name = operationId.ToString(OperationId.FormatSpecifier);
            string version = await _secretStore.SetSecretAsync(
                name,
                JsonSerializer.Serialize(secrets, _serializerOptions),
                MediaTypeNames.Application.Json,
                cancellationToken);

            options.BlobContainerUri = null;
            options.ConnectionString = null;
            options.Secret = new SecretKey { Name = name, Version = version };
        }

        return options;
    }

    protected override Task ValidateAsync(AzureBlobExportOptions options, CancellationToken cancellationToken = default)
    {
        List<ValidationResult> results = options.Validate(new ValidationContext(this)).ToList();

        // Run additional validation based on the sink provider options if the options themselves are configured correctly.
        // We need to ensure that they do not contain SAS Tokens or use public access if it is disallowed
        if (results.Count == 0)
        {
            if (options.BlobContainerUri != null)
            {
                if (options.BlobContainerUri.Query.Length > 1)
                {
                    if (!_providerOptions.AllowSasTokens)
                        results.Add(new ValidationResult(DicomBlobResource.SasTokenAuthenticationUnsupported));
                }
                else if (!options.UseManagedIdentity && !_providerOptions.AllowPublicAccess)
                {
                    results.Add(new ValidationResult(DicomBlobResource.PublicBlobStorageConnectionUnsupported));
                }
            }
            else if (!_providerOptions.AllowSasTokens && !options.IsEmulatorConnectionString() && options.ConnectionString.Contains("SharedAccessSignature=", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new ValidationResult(DicomBlobResource.SasTokenAuthenticationUnsupported));
            }
            else if (!_providerOptions.AllowPublicAccess && !options.ConnectionString.Contains("SharedAccessSignature=", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new ValidationResult(DicomBlobResource.PublicBlobStorageConnectionUnsupported));
            }
        }

        if (results.Count > 0)
            throw new ValidationException(results.First().ErrorMessage);

        return Task.CompletedTask;
    }

    private async Task<AzureBlobExportOptions> RetrieveSensitiveOptionsAsync(AzureBlobExportOptions options, CancellationToken cancellationToken = default)
    {
        if (options.Secret != null)
        {
            if (_secretStore == null)
                throw new InvalidOperationException(DicomBlobResource.MissingSecretStore);

            string json = await _secretStore.GetSecretAsync(options.Secret.Name, options.Secret.Version, cancellationToken);
            BlobSecrets secrets = JsonSerializer.Deserialize<BlobSecrets>(json, _serializerOptions);

            options.ConnectionString = secrets.ConnectionString;
            options.BlobContainerUri = secrets.BlobContainerUri;
            options.Secret = null;
        }

        return options;
    }

    private static bool IsSensitiveBlobContainerUri(Uri blobContainerUri)
       => blobContainerUri.Query.Length > 1;

    private static bool IsSensitiveConnectionString(AzureBlobExportOptions options)
        => !options.IsEmulatorConnectionString() && options.ConnectionString.Contains("SharedAccessSignature=", StringComparison.OrdinalIgnoreCase);

    private sealed class BlobSecrets
    {
        public string ConnectionString { get; set; }

        public Uri BlobContainerUri { get; set; }
    }
}
