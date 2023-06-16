// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Partition;

namespace Microsoft.Health.Dicom.Core.Extensions;

public static class DicomRequestContextExtensions
{
    public static int GetPartitionKey(this IDicomRequestContext dicomRequestContext)
    {
        EnsureArg.IsNotNull(dicomRequestContext, nameof(dicomRequestContext));

        return dicomRequestContext.DataPartitionEntry.PartitionKey;
    }

    public static string GetPartitionName(this IDicomRequestContext dicomRequestContext)
    {
        EnsureArg.IsNotNull(dicomRequestContext, nameof(dicomRequestContext));

        return dicomRequestContext.DataPartitionEntry.PartitionName;
    }

    public static PartitionEntry GetPartitionEntry(this IDicomRequestContext dicomRequestContext)
    {
        EnsureArg.IsNotNull(dicomRequestContext, nameof(dicomRequestContext));

        return dicomRequestContext.DataPartitionEntry;
    }
}