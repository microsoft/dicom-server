// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public class QueryResult<TItem>
    {
        public QueryResult(bool hasMoreResults, IEnumerable<TItem> results)
        {
            HasMoreResults = hasMoreResults;
            Results = results;
        }

        public bool HasMoreResults { get; }

        public IEnumerable<TItem> Results { get; }
    }
}
