// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Identity;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Health.Dicom.Blob.Utilities;
internal class ExternalBlobDataStoreConfiguration
{
    public const string SectionName = "ExternalBlobStore";

    public string ConnectionString { get; set; }

    public string ContainerName { get; set; }

    public BlobDataStoreAuthenticationType AuthenticationType { get; set; } = BlobDataStoreAuthenticationType.ConnectionString;

    public BlobDataStoreRequestOptions RequestOptions { get; } = new BlobDataStoreRequestOptions();

    /// <summary>
    /// If set, the client id of the managed identity to use when connecting to azure storage, if AuthenticationType == ManagedIdentity.
    /// </summary>
    public string ManagedIdentityClientId
    {
        get => Credentials.ManagedIdentityClientId;
        set => Credentials.ManagedIdentityClientId = value;
    }

    /// <summary>
    /// Gets or sets the options for configuring DefaultAzureCredential
    /// </summary>
    /// <value>The settings for configuring the default azure credential</value>
    public DefaultAzureCredentialOptions Credentials { get; set; } = new DefaultAzureCredentialOptions();
}
