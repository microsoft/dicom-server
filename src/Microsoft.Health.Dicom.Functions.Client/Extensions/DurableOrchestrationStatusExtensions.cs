// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Core.Models.Operations;

namespace Microsoft.Health.Dicom.Functions.Client.Extensions;

internal static class DurableOrchestrationStatusExtensions
{
    private static readonly IReadOnlyDictionary<string, DicomOperation> NameOperationMapping = new Dictionary<string, DicomOperation>(StringComparer.OrdinalIgnoreCase)
    {
        { FunctionNames.ReindexInstances, DicomOperation.Reindex },
        { FunctionNames.ExportDicomFiles, DicomOperation.Export },
        { FunctionNames.CopyInstances, DicomOperation.Copy },
    };

    public static DicomOperation GetDicomOperation(this DurableOrchestrationStatus status)
    {
        EnsureArg.IsNotNull(status, nameof(status));

        return NameOperationMapping.GetValueOrDefault(status.Name, DicomOperation.Unknown);
    }
}
