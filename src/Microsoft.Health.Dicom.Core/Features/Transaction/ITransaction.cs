// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Core.Features.Transaction
{
    public interface ITransaction : IDisposable
    {
        Task AppendInstanceAsync(DicomInstance dicomInstance, CancellationToken cancellationToken);

        Task CommitAsync(CancellationToken cancellationToken = default);

        Task AbortAsync(CancellationToken cancellationToken = default);
    }
}
