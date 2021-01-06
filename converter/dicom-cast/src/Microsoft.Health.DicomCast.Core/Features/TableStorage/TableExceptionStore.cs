// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using EnsureThat;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;

namespace Microsoft.Health.DicomCast.Core.Features.TableStorage
{
    /// <inheritdoc/>
    public class TableExceptionStore : IExceptionStore
    {
        private readonly ITableStore _store;

        public TableExceptionStore(ITableStore store)
        {
            EnsureArg.IsNotNull(store);

            _store = store;
        }

        /// <inheritdoc/>
        public async void StoreException(string studyUid, string seriesUid, string instanceUid, long changeFeedSequence, Exception exceptionToStore, TableErrorType errorType, CancellationToken cancellationToken = default)
        {
            await _store.StoreExceptionToTable(studyUid, seriesUid, instanceUid, changeFeedSequence, exceptionToStore, errorType, cancellationToken);
        }
    }
}
