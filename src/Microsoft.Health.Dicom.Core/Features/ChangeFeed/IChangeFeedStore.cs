// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.ChangeFeed;

namespace Microsoft.Health.Dicom.Core.Features.ChangeFeed;

public interface IChangeFeedStore
{
    Task<ChangeFeedEntry> GetChangeFeedLatestAsync(ChangeFeedOrder order, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChangeFeedEntry>> GetChangeFeedAsync(DateTimeOffsetRange range, long offset, int limit, ChangeFeedOrder order, CancellationToken cancellationToken = default);
}
