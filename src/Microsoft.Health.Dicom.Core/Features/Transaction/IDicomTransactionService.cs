// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Core.Features.Transaction
{
    public interface IDicomTransactionService
    {
        Task<ITransaction> CreateTransactionAsync(DicomInstance dicomInstance, CancellationToken cancellationToken = default);
    }
}
