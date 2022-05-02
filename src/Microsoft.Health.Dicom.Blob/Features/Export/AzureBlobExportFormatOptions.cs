// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

internal sealed class AzureBlobExportFormatOptions
{
    public Encoding ErrorEncoding { get; }

    public string ErrorFile { get; }

    public string FilePattern { get; }

    public Guid OperationId { get; }

    public AzureBlobExportFormatOptions(Guid operationId, string dicomFilePattern, string errorFilePattern, Encoding errorEncoding)
    {
        OperationId = operationId;
        FilePattern = ExportFilePattern.Parse(
            EnsureArg.IsNotNullOrWhiteSpace(dicomFilePattern, nameof(dicomFilePattern)),
            ExportPatternPlaceholders.All);
        ErrorFile = ExportFilePattern.Format(
            ExportFilePattern.Parse(
                EnsureArg.IsNotNullOrWhiteSpace(errorFilePattern, nameof(errorFilePattern)),
                ExportPatternPlaceholders.Operation),
            operationId);
        ErrorEncoding = EnsureArg.IsNotNull(errorEncoding, nameof(errorEncoding));
    }

    public string GetFilePath(VersionedInstanceIdentifier identifier)
        => ExportFilePattern.Format(FilePattern, OperationId, identifier);
}
