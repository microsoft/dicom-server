// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Client.Models.Export;

internal sealed class AzureBlobExportOptions
{
    public Uri ContainerUri { get; init; }

    public string ConnectionString { get; init; }

    public string ContainerName { get; init; }
}
