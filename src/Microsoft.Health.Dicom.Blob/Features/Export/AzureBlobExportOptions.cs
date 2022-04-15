// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

internal sealed class AzureBlobExportOptions
{
    public Uri ContainerUri { get; set; }

    public string Path { get; set; }

    public string SasToken { get; set; }
}
