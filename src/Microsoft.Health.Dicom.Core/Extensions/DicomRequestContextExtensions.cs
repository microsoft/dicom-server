// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Partition;

namespace Microsoft.Health.Dicom.Core.Extensions;

public static class DicomRequestContextExtensions
{
    public static int GetPartitionKey(this IDicomRequestContext dicomRequestContext)
    {
        EnsureArg.IsNotNull(dicomRequestContext, nameof(dicomRequestContext));

        var partitionKey = dicomRequestContext.DataPartitionEntry?.PartitionKey;
        EnsureArg.IsTrue(partitionKey.HasValue, nameof(partitionKey));
        return partitionKey.Value;
    }
    public static string GetPartitionName(this IDicomRequestContext dicomRequestContext)
    {
        EnsureArg.IsNotNull(dicomRequestContext, nameof(dicomRequestContext));

        var partitionName = dicomRequestContext.DataPartitionEntry?.PartitionName;
        Debug.Assert(partitionName.Length > 0);
        return partitionName;
    }
    public static PartitionEntry GetPartitionEntry(this IDicomRequestContext dicomRequestContext)
    {
        EnsureArg.IsNotNull(dicomRequestContext, nameof(dicomRequestContext));

        var partition = dicomRequestContext.DataPartitionEntry;
        Debug.Assert(partition is not null);
        return partition;
    }
}
