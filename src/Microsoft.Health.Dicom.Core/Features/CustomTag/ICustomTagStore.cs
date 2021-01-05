// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public interface ICustomTagStore
    {
        Task<long> AddCustomTagAsync(string key, string vr, CustomTagLevel leve, CustomTagStatus status, CancellationToken cancellationToken = default);

        Task UpdateCustomTagStatusAsync(long key, CustomTagStatus status, CancellationToken cancellationToken = default);

        Task DeleteCustomTagAsync(long key, CancellationToken cancellationToken = default);

        Task<IEnumerable<VersionedInstanceIdentifier>> GetVersionedInstancesAsync(long endWatermark, int top = 10, IndexStatus indexStatus = IndexStatus.Created, CancellationToken cancellationToken = default);

        Task<long?> GetLatestInstanceAsync(CancellationToken cancellationToken = default);
    }
}
