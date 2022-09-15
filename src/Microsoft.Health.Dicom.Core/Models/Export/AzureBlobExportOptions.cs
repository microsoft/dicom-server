// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Models.Export;

internal sealed class AzureBlobExportOptions
{
    public Uri BlobContainerUri { get; set; }

    public string ConnectionString { get; set; }

    public string BlobContainerName { get; set; }

    public bool UseManagedIdentity { get; set; }

    [JsonProperty] // Newtonsoft is only used internally while this property would be ignored by System.Text.Json
    internal SecretKey Secret { get; set; }

    // TODO: Make public upon request. Perhaps a boolean flag instead?
    internal const string DicomFilePattern = "%Operation%/results/%Study%/%Series%/%SopInstance%.dcm";

    internal const string ErrorLogPattern = "%Operation%/errors.log";
}
