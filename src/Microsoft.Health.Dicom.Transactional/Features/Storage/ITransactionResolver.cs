// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;

namespace Microsoft.Health.Dicom.Transactional.Features.Storage
{
    public interface ITransactionResolver
    {
        Task ResolveTransactionAsync(ICloudBlob cloudBlob, CancellationToken cancellationToken = default);
    }
}
