// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

internal sealed class AzureBlobExportFormatOptions
{
    public Encoding ErrorEncoding { get; init; }

    public string ErrorFile { get; init; }

    public string FilePattern { get; init; }

    public Guid OperationId { get; init; }

    public string GetFilePath(VersionedInstanceIdentifier identifier)
        => ExportFilePattern.Format(FilePattern, OperationId, identifier);
}
