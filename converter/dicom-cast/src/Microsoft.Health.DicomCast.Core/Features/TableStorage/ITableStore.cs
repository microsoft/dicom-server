// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.DicomCast.Core.Features.ExceptionStorage
{
    public interface ITableStore
    {
        Task StoreExceptionToTable(string studyId, string seriesId, string instanceId, Exception exceptionToStore, TableErrorType errorType, CancellationToken cancellationToken = default);
    }
}
