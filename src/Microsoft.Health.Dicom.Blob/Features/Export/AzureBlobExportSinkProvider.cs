// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Core.Features.Identity;
using Microsoft.Health.Dicom.Blob.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

internal sealed class AzureBlobExportSinkProvider : ExportSinkProvider<AzureBlobExportOptions>
{
    internal const string ClientOptionsName = "Export";

    public override ExportDestinationType Type => ExportDestinationType.AzureBlob;

    private readonly ISecretStore _secretStore;
    private readonly IFileStore _fileStore;
    private readonly IExternalCredentialProvider _credentialProvider;
    private readonly AzureBlobExportSinkProviderOptions _providerOptions;
    private readonly AzureBlobClientOptions _clientOptions;
    private readonly BlobOperationOptions _operationOptions;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ILogger _logger;

    public AzureBlobExportSinkProvider(
        IFileStore fileStore,
        IExternalCredentialProvider credentialProvider,
        IOptionsSnapshot<AzureBlobExportSinkProviderOptions> providerOptions,
        IOptionsSnapshot<AzureBlobClientOptions> clientOptions,
        IOptionsSnapshot<BlobOperationOptions> operationOptions,
        IOptionsSnapshot<JsonSerializerOptions> serializerOptions,
        ILogger<AzureBlobExportSinkProvider> logger)
    {
        _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
        _credentialProvider = EnsureArg.IsNotNull(credentialProvider, nameof(credentialProvider));
        _providerOptions = EnsureArg.IsNotNull(providerOptions?.Value, nameof(providerOptions));
        _clientOptions = EnsureArg.IsNotNull(clientOptions?.Get(ClientOptionsName), nameof(clientOptions));
        _operationOptions = EnsureArg.IsNotNull(operationOptions?.Value, nameof(operationOptions));
        _serializerOptions = EnsureArg.IsNotNull(serializerOptions?.Value, nameof(serializerOptions));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public AzureBlobExportSinkProvider(
        ISecretStore secretStore,
        IFileStore fileStore,
        IExternalCredentialProvider credentialProvider,
        IOptionsSnapshot<AzureBlobExportSinkProviderOptions> providerOptions,
        IOptionsSnapshot<AzureBlobClientOptions> clientOptions,
        IOptionsSnapshot<BlobOperationOptions> operationOptions,
        IOptionsSnapshot<JsonSerializerOptions> serializerOptions,
        ILogger<AzureBlobExportSinkProvider> logger)
        : this(fileStore, credentialProvider, providerOptions, clientOptions, operationOptions, serializerOptions, logger)
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
            options.GetBlobContainerClient(_credentialProvider, _clientOptions),
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
            if (ContainsQueryStringParameter(options.BlobContainerUri, AzureStorageConnection.Uri.Sig))
                secrets = new BlobSecrets { BlobContainerUri = options.BlobContainerUri };
        }
        else if (ContainsKey(options.ConnectionString, AzureStorageConnection.SharedAccessSignature))
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
        ValidationResult error = null;

        // Using connection strings?
        if (options.BlobContainerUri == null)
        {
            // No connection info?
            if (string.IsNullOrWhiteSpace(options.ConnectionString) || string.IsNullOrWhiteSpace(options.BlobContainerName))
            {
                error = new ValidationResult(DicomBlobResource.MissingExportBlobConnection);
            }
            else // Otherwise, validate the connection string
            {
                if (!IsEmulator(options.ConnectionString) &&
                    ContainsKey(options.ConnectionString, AzureStorageConnection.AccountKey))
                {
                    // Account keys are not allowed
                    error = new ValidationResult(DicomBlobResource.AzureStorageAccountKeyUnsupported);
                }
                else if (!_providerOptions.AllowSasTokens &&
                    ContainsKey(options.ConnectionString, AzureStorageConnection.SharedAccessSignature))
                {
                    // SAS tokens are not allowed
                    error = new ValidationResult(DicomBlobResource.SasTokenAuthenticationUnsupported);
                }
                else if (!_providerOptions.AllowPublicAccess &&
                    !ContainsKey(options.ConnectionString, AzureStorageConnection.SharedAccessSignature))
                {
                    // Public access not allowed
                    error = new ValidationResult(DicomBlobResource.PublicBlobStorageConnectionUnsupported);
                }
                else if (options.UseManagedIdentity)
                {
                    // Managed identity must be used with URIs
                    error = new ValidationResult(DicomBlobResource.InvalidExportBlobAuthentication);
                }
            }
        }
        else // Otherwise, using a blob container URI
        {
            if (!string.IsNullOrWhiteSpace(options.ConnectionString) || !string.IsNullOrWhiteSpace(options.BlobContainerName))
            {
                // Conflicting connection info
                error = new ValidationResult(DicomBlobResource.ConflictingExportBlobConnections);
            }
            else if (options.UseManagedIdentity &&
                ContainsQueryStringParameter(options.BlobContainerUri, AzureStorageConnection.Uri.Sig))
            {
                // Managed identity and SAS both specified
                error = new ValidationResult(DicomBlobResource.ConflictingBlobExportAuthentication);
            }
            else if (!_providerOptions.AllowSasTokens &&
                ContainsQueryStringParameter(options.BlobContainerUri, AzureStorageConnection.Uri.Sig))
            {
                // SAS tokens are not allowed
                error = new ValidationResult(DicomBlobResource.SasTokenAuthenticationUnsupported);
            }
            else if (!_providerOptions.AllowPublicAccess && !options.UseManagedIdentity &&
                !ContainsQueryStringParameter(options.BlobContainerUri, AzureStorageConnection.Uri.Sig))
            {
                // No auth specified, but public access is forbidden
                error = new ValidationResult(DicomBlobResource.PublicBlobStorageConnectionUnsupported);
            }
        }

        return error == null ? Task.CompletedTask : throw new ValidationException(error.ErrorMessage);
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

    private static bool IsEmulator(string connectionString)
    {
        EnsureArg.IsNotNullOrWhiteSpace(connectionString, nameof(connectionString));
        return connectionString.Equals("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase) ||
            ContainsPair(connectionString, AzureStorageConnection.AccountName, "devstoreaccount1");
    }

    // Note: These Contain methods may produce false positives as they do not check for the exact name

    private static bool ContainsKey(string connectionString, string key)
    {
        EnsureArg.IsNotNullOrWhiteSpace(connectionString, nameof(connectionString));
        EnsureArg.IsNotNullOrWhiteSpace(key, nameof(key));

        return connectionString.Contains(key + '=', StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsPair(string connectionString, string key, string value)
    {
        EnsureArg.IsNotNullOrWhiteSpace(connectionString, nameof(connectionString));
        EnsureArg.IsNotNullOrWhiteSpace(key, nameof(key));

        return connectionString.Contains(key + '=' + value, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsQueryStringParameter(Uri storageUri, string name)
    {
        EnsureArg.IsNotNull(storageUri, nameof(storageUri));
        EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

        return storageUri.Query.Contains(name + '=', StringComparison.OrdinalIgnoreCase);
    }

    private sealed class BlobSecrets
    {
        public string ConnectionString { get; set; }

        public Uri BlobContainerUri { get; set; }
    }
}
