// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Indexing;

namespace Microsoft.Health.Dicom.SqlServer.Features.Indexing
{
    internal class SqlReindexStore : IReindexStore
    {
        public Task CompleteReindexAsync(string operationId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ReindexEntry>> GetReindexEntriesAsync(string operationId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ReindexOperation> PrepareReindexingAsync(IReadOnlyList<int> tagKeys, string operationId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateReindexProgressAsync(string operationId, long endWatermark, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
