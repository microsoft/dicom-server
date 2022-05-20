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
        _secretStore = EnsureArg.IsNotNull(secretStore, nameof(secretStore));
    }

    protected override async Task<IExportSink> CreateAsync(IServiceProvider provider, AzureBlobExportOptions options, Guid operationId, CancellationToken cancellationToken = default)
    {
        options = await RetrieveSensitiveOptionsAsync(options, cancellationToken);

        return new AzureBlobExportSink(
            provider.GetRequiredService<IFileStore>(),
            options.GetBlobContainerClient(provider.GetRequiredService<IOptionsMonitor<AzureBlobClientOptions>>().Get("Export")),
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

        if (_secretStore == null)
        {
            _logger.LogWarning("No secret store has been registered. Sensitive export settings will be preserved in plaintext.");
            return options;
        }

        // TODO: Should we detect if the ContainerUri actually has a SAS token before storing the secret?
        var secrets = new BlobSecrets
        {
            ConnectionString = options.ConnectionString,
            ContainerUri = options.ContainerUri,
        };

        string name = operationId.ToString(OperationId.FormatSpecifier);
        string version = await _secretStore.SetSecretAsync(
            name,
            JsonSerializer.Serialize(secrets, _serializerOptions),
            cancellationToken);

        options.ConnectionString = null;
        options.ContainerUri = null;
        options.Secret = new SecretKey { Name = name, Version = version };

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
            options.ContainerUri = secrets.ContainerUri;
            options.Secret = null;
        }

        return options;
    }

    private sealed class BlobSecrets
    {
        public string ConnectionString { get; set; }

        public Uri ContainerUri { get; set; }
    }
}
