// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

internal sealed class AzureBlobExportOptions
{
    /// <summary>
    /// like https://mystorageacct.blob.core.windows.net/mycontainer?sp
    /// </summary>
    public Uri ContainerSasUri { get; set; }

    /// <summary>
    /// Like https://mystorageacct.blob.core.windows.net/mycontainer
    /// </summary>
    public Uri ContainerUri { get; set; }

    /// <summary>
    /// Destination folder path
    /// </summary>
    public string FolderPath { get; set; }
}
