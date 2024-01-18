// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Core.Models.Operations;

namespace Microsoft.Health.Dicom.Functions.Client.Extensions;

internal static class DurableOrchestrationStatusExtensions
{
    public static DicomOperation GetDicomOperation(this DurableOrchestrationStatus status)
    {
        EnsureArg.IsNotNull(status, nameof(status));

        if (status?.Name != null)
        {
            if (status.Name.StartsWith(FunctionNames.ContentLengthBackFill, StringComparison.OrdinalIgnoreCase))
                return DicomOperation.ContentLengthBackFill;

            if (status.Name.StartsWith(FunctionNames.DataCleanup, StringComparison.OrdinalIgnoreCase))
                return DicomOperation.DataCleanup;

            if (status.Name.StartsWith(FunctionNames.ExportDicomFiles, StringComparison.OrdinalIgnoreCase))
                return DicomOperation.Export;

            if (status.Name.StartsWith(FunctionNames.ReindexInstances, StringComparison.OrdinalIgnoreCase))
                return DicomOperation.Reindex;

            if (status.Name.StartsWith(FunctionNames.UpdateInstances, StringComparison.OrdinalIgnoreCase))
                return DicomOperation.Update;
        }

        return DicomOperation.Unknown;
    }
}
