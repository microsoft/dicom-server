// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Models.Export;

/// <summary>
/// Specifies the kind of destination where data may be exported.
/// </summary>
public enum ExportDestinationType
{
    /// <summary>
    /// Specifies an unknown destination.
    /// </summary>
    Unknown,

    /// <summary>
    /// Specifies Azure Blob Storage.
    /// </summary>
    AzureBlob,
}
