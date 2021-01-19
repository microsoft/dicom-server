// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Features.TableStorage;

namespace Microsoft.Health.DicomCast.Core.Features.ExceptionStorage
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
        public async Task WriteExceptionAsync(ChangeFeedEntry changeFeedEntry, Exception exceptionToStore, ErrorType errorType, CancellationToken cancellationToken = default)
        {
            await _store.StoreExceptionToTable(changeFeedEntry, exceptionToStore, errorType, cancellationToken);
        }

        public async Task WriteRetryableExceptionAsync(ChangeFeedEntry changeFeedEntry, int retryNum, Exception exceptionToStore, CancellationToken cancellationToken = default)
        {
            await _store.StoreRetryableExceptionToTable(changeFeedEntry, retryNum, exceptionToStore, cancellationToken);
        }
    }
}
