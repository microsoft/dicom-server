// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

internal sealed class AzureBlobExportOptions : ISensitive, IValidatableObject
{
    public Uri ContainerUri { get; set; }

    public string ConnectionString { get; set; }

    public string ContainerName { get; set; }

    public string Folder { get; set; } = "%Operation%";

    public string FilePattern { get; set; } = "Results/%Study%/%Series%/%SopInstance%.dcm";

    public string SasToken { get; set; }

    internal SecretKey Secrets { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();
        if (ContainerUri == null)
        {
            if (string.IsNullOrEmpty(ConnectionString) || string.IsNullOrEmpty(ContainerName))
                results.Add(new ValidationResult(DicomBlobResource.MissingExportBlobConnection));
        }
        else if (!string.IsNullOrEmpty(ConnectionString) || !string.IsNullOrEmpty(ContainerName))
        {
            results.Add(new ValidationResult(DicomBlobResource.ConflictingExportBlobConnections));
        }

        if (string.IsNullOrEmpty(FilePattern))
            results.Add(new ValidationResult(
                string.Format(CultureInfo.CurrentCulture, DicomBlobResource.MissingProperty, nameof(FilePattern)),
                new string[] { nameof(FilePattern) }));

        return results;
    }

    public BlobContainerClient GetBlobContainerClient(BlobClientOptions options)
    {
        EnsureArg.IsNotNull(options, nameof(options));

        if (!string.IsNullOrWhiteSpace(SasToken))
        {
            return new BlobContainerClient(ContainerUri, new AzureSasCredential(SasToken), options);
        }
        else if (ContainerUri != null)
        {
            return new BlobContainerClient(ContainerUri, options);
        }
        else
        {
            return new BlobContainerClient(ConnectionString, ContainerName);
        }
    }

    public async Task ClassifyAsync(ISecretStore secretStore, string secretName, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(secretStore, nameof(secretStore));

        if (!string.IsNullOrEmpty(ConnectionString) || !string.IsNullOrEmpty(SasToken))
        {
            var values = new BlobSecrets
            {
                ConnectionString = ConnectionString,
                SasToken = SasToken,
            };

            string version = await secretStore.SetSecretAsync(
                secretName,
                JsonSerializer.Serialize(values),
                cancellationToken);

            Secrets = new SecretKey { Name = secretName, Version = version };

            ConnectionString = null;
            SasToken = null;
        }
    }

    public async Task DeclassifyAsync(ISecretStore secretStore, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(secretStore, nameof(secretStore));

        if (Secrets != null)
        {
            string json = await secretStore.GetSecretAsync(Secrets.Name, Secrets.Version, cancellationToken);
            var values = JsonSerializer.Deserialize<BlobSecrets>(json);

            ConnectionString = values.ConnectionString;
            SasToken = values.SasToken;
        }
    }

    private sealed class BlobSecrets
    {
        public string ConnectionString { get; set; }

        public string SasToken { get; set; }
    }
}
