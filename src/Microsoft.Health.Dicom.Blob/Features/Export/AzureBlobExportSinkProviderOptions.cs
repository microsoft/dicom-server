// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Blob.Features.Export;

/// <summary>
/// Represents the options for the Azure Blob Storage export sink provider.
/// </summary>
public class AzureBlobExportSinkProviderOptions
{
    /// <summary>
    /// The default section name within a configuration.
    /// </summary>
    public const string DefaultSection = "Export:Sinks:AzureBlob";

    /// <summary>
    /// Gets or sets whether SAS tokens should be an allowed form of authentication.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if SAS tokens can be used in connection strings or URIs;
    /// otherwise, <see langword="false"/>
    /// </value>
    public bool AllowSasTokens { get; set; }

    /// <summary>
    /// Gets or sets whether the sink allows connections to storage accounts with public access (unauthenticated).
    /// </summary>
    /// <value><see langword="true"/> if unauthenticated connections are allowed; otherwise, <see langword="false"/></value>
    public bool AllowPublicAccess { get; set; }
}
