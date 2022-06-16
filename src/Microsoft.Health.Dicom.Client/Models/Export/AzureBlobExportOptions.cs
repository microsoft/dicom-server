// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Client.Models.Export;

internal sealed class AzureBlobExportOptions
{
    public string BlobContainerName { get; init; }

    public Uri BlobContainerUri { get; init; }

    public string ConnectionString { get; init; }

    public bool UseManagedIdentity { get; init; }
}
