// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;

namespace Microsoft.Health.DicomCast.Core.Features.TableStorage
{
    /// <summary>
    /// Implementation of ITableStoreService to use when the feature flag is not enabled as the regular
    /// implementation requires a CloudTableClient which is not initialized when the feature flag is disabled
    /// </summary>
    public class TableStoreServiceUnImplementedForFeatureFlag : ITableStoreService
    {
        /// <inheritdoc/>
        public Task StoreException(string studyId, string seriesId, string instanceId, Exception exceptionToStore, TableErrorType errorType, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
