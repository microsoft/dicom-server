// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;

namespace Microsoft.Health.DicomCast.Core.Features.TableStorage
{
    public class TableStoreService : ITableStoreService
    {
        private readonly ITableStore _store;

        public TableStoreService(ITableStore store)
        {
            EnsureArg.IsNotNull(store);

            _store = store;
        }

        public async Task StoreException(string studyId, string seriesId, string instanceId, Exception exceptionToStore, TableErrorType errorType, CancellationToken cancellationToken = default)
        {
            await _store.StoreExceptionToTable(studyId, seriesId, instanceId, exceptionToStore, errorType, cancellationToken);
        }
    }
}
