// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

internal sealed class AzureBlobExportOptions : IValidatableObject
{
    public Uri ContainerUri { get; set; }

    public string ConnectionString { get; set; }

    public string ContainerName { get; set; }

    // TODO: Make public upon request. Perhaps a boolean flag instead?
    internal string DicomFilePattern { get; set; } = "%Operation%/Results/%Study%/%Series%/%SopInstance%.dcm";

    internal string ErrorLogPattern { get; set; } = "%Operation%/Errors.log";

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
}
