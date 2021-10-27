// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Context;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    public static class DicomRequestContextExtensions
    {
        public static int GetPartitionKey(this IDicomRequestContext dicomRequestContext)
        {
            EnsureArg.IsNotNull(dicomRequestContext, nameof(dicomRequestContext));

            var partitionKey = dicomRequestContext.DataPartitionEntry?.PartitionKey;
            EnsureArg.IsTrue(partitionKey.HasValue, nameof(partitionKey));
            return partitionKey.Value;
        }
    }
}
