// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Models.Delete;

namespace Microsoft.Health.Dicom.Core.Features.Delete;

public interface IDeleteService
{
    Task DeleteStudyAsync(string studyInstanceUid, CancellationToken cancellationToken = default);

    Task DeleteSeriesAsync(string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken = default);

    Task DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken = default);

    Task<(bool Success, int RetrievedInstanceCount)> CleanupDeletedInstancesAsync(CancellationToken cancellationToken = default);

    Task DeleteInstanceNowAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken);

    Task<DeleteMetrics> GetMetricsAsync(CancellationToken cancellationToken = default);
}
