// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Query.Model;

namespace Microsoft.Health.Dicom.Core.Features.Query;

public interface IQueryStore
{
    Task<QueryResult> QueryAsync(
        int partitionKey,
        QueryExpression query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StudyResult>> GetStudyResultAsync(
        int partitionKey,
        IReadOnlyCollection<long> versions,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SeriesResult>> GetSeriesResultAsync(
        int partitionKey,
        IReadOnlyCollection<long> versions,
        CancellationToken cancellationToken = default);
}
