// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.ChangeFeed;

public interface IChangeFeedStore
{
    Task<ChangeFeedEntry> GetChangeFeedLatestAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChangeFeedEntry>> GetChangeFeedAsync(long offset, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChangeFeedEntry>> GetDeletedChangeFeedByWatermarkOrTimeStampAsync(int batchCount, DateTime? timeStamp, long startWatermark = default, long endWatermark = default, CancellationToken cancellationToken = default);

    Task<long> GetMaxDeletedChangeFeedWatermarkAsync(DateTime timeStamp, CancellationToken cancellationToken = default);
}
