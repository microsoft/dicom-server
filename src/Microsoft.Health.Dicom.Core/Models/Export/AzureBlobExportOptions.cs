// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Models.Export;

internal sealed class AzureBlobExportOptions : IValidatableObject
{
    public Uri ContainerUri { get; set; }

    public string ConnectionString { get; set; }

    public string ContainerName { get; set; }

    [JsonProperty] // Newtonsoft is only used internally while this property would be ignored by System.Text.Json
    internal SecretKey Secret { get; set; }

    // TODO: Make public upon request. Perhaps a boolean flag instead?
    internal const string DicomFilePattern = "%Operation%/Results/%Study%/%Series%/%SopInstance%.dcm";

    internal const string ErrorLogPattern = "%Operation%/Errors.log";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();
        if (ContainerUri == null)
        {
            if (string.IsNullOrWhiteSpace(ConnectionString) || string.IsNullOrWhiteSpace(ContainerName))
                results.Add(new ValidationResult(DicomCoreResource.MissingExportBlobConnection));
        }
        else if (!string.IsNullOrWhiteSpace(ConnectionString) || !string.IsNullOrWhiteSpace(ContainerName))
        {
            results.Add(new ValidationResult(DicomCoreResource.ConflictingExportBlobConnections));
        }

        return results;
    }
}
