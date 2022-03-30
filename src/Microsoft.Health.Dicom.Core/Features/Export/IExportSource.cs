// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

public interface IExportSource
{
    PaginatedResults<IReadOnlyCollection<long>> GetBatchOffsets(int size, ContinuationToken continuationToken = default);

    Task<IExportBatch> GetBatchAsync(long offset);
}
