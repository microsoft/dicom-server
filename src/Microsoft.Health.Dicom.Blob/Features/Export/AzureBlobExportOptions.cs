// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
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

    public string DicomFilePattern { get; set; } = "%Operation%/Results/%Study%/%Series%/%SopInstance%.dcm";

    public string ErrorLogPattern { get; set; } = "%Operation%/Errors.log";

    internal SecretKey Secrets { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();
        if (ContainerUri == null)
        {
            if (string.IsNullOrWhiteSpace(ConnectionString) || string.IsNullOrWhiteSpace(ContainerName))
                results.Add(new ValidationResult(DicomBlobResource.MissingExportBlobConnection));
        }
        else if (!string.IsNullOrWhiteSpace(ConnectionString) || !string.IsNullOrWhiteSpace(ContainerName))
        {
            results.Add(new ValidationResult(DicomBlobResource.ConflictingExportBlobConnections));
        }

        if (string.IsNullOrWhiteSpace(DicomFilePattern))
            results.Add(new ValidationResult(
                string.Format(CultureInfo.CurrentCulture, DicomBlobResource.MissingProperty, nameof(DicomFilePattern)),
                new string[] { nameof(DicomFilePattern) }));

        if (string.IsNullOrWhiteSpace(ErrorLogPattern))
            results.Add(new ValidationResult(
                string.Format(CultureInfo.CurrentCulture, DicomBlobResource.MissingProperty, nameof(ErrorLogPattern)),
                new string[] { nameof(ErrorLogPattern) }));

        return results;
    }

    public BlobContainerClient GetBlobContainerClient(BlobClientOptions options)
    {
        EnsureArg.IsNotNull(options, nameof(options));

        return ContainerUri != null
            ? new BlobContainerClient(ContainerUri, options)
            : new BlobContainerClient(ConnectionString, ContainerName, options);
    }

    public async Task ClassifyAsync(ISecretStore secretStore, string secretName, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(secretStore, nameof(secretStore));

        // TODO: Should we detect if the ContainerUri actually has a SAS token before storing the secret?
        var values = new BlobSecrets
        {
            ConnectionString = ConnectionString,
            ContainerUri = ContainerUri,
        };

        string version = await secretStore.SetSecretAsync(
            secretName,
            JsonSerializer.Serialize(values, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }),
            cancellationToken);

        Secrets = new SecretKey { Name = secretName, Version = version };
        ConnectionString = null;
        ContainerUri = null;
    }

    public async Task DeclassifyAsync(ISecretStore secretStore, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(secretStore, nameof(secretStore));

        if (Secrets != null)
        {
            string json = await secretStore.GetSecretAsync(Secrets.Name, Secrets.Version, cancellationToken);
            BlobSecrets values = JsonSerializer.Deserialize<BlobSecrets>(json);

            ConnectionString = values.ConnectionString;
            ContainerUri = values.ContainerUri;
            Secrets = null;
        }
    }

    private sealed class BlobSecrets
    {
        public string ConnectionString { get; set; }

        public Uri ContainerUri { get; set; }
    }
}
