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

        // TODO: Add export
        return status.Name != null &&
            status.Name.StartsWith(FunctionNames.ReindexInstances, StringComparison.OrdinalIgnoreCase)
            ? DicomOperation.Reindex
            : DicomOperation.Unknown;
    }
}
