// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ILogger _logger;

    public AzureBlobExportSinkProvider(IOptions<JsonSerializerOptions> serializerOptions, ILogger<AzureBlobExportSinkProvider> logger)
    {
        _serializerOptions = EnsureArg.IsNotNull(serializerOptions?.Value, nameof(serializerOptions));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public AzureBlobExportSinkProvider(ISecretStore secretStore, IOptions<JsonSerializerOptions> serializerOptions, ILogger<AzureBlobExportSinkProvider> logger)
        : this(serializerOptions, logger)
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

    protected override async Task<IExportSink> CreateAsync(IServiceProvider provider, AzureBlobExportOptions options, Guid operationId, CancellationToken cancellationToken = default)
    {
        options = await RetrieveSensitiveOptionsAsync(options, cancellationToken);

        return new AzureBlobExportSink(
            provider.GetRequiredService<IFileStore>(),
            await options.GetBlobContainerClientAsync(
                provider.GetRequiredService<IExportIdentityProvider>(),
                provider.GetRequiredService<IOptionsMonitor<AzureBlobClientOptions>>().Get("Export"),
                cancellationToken),
            Options.Create(
                new AzureBlobExportFormatOptions(
                    operationId,
                    AzureBlobExportOptions.DicomFilePattern,
                    AzureBlobExportOptions.ErrorLogPattern,
                    Encoding.UTF8)),
            provider.GetRequiredService<IOptions<BlobOperationOptions>>(),
            provider.GetRequiredService<IOptions<JsonSerializerOptions>>());
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
            var builder = new UriBuilder(options.BlobContainerUri);
            if (builder.Query != null && builder.Query.Length > 1) // More than "?"
                secrets = new BlobSecrets { BlobContainerUri = options.BlobContainerUri };
        }
        else if (options.ConnectionString.Contains("SharedAccessSignature=", StringComparison.InvariantCultureIgnoreCase))
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

    private sealed class BlobSecrets
    {
        public string ConnectionString { get; set; }

        public Uri BlobContainerUri { get; set; }
    }
}
