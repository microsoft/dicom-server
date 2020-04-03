// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public interface IDicomStoreService
    {
        Task<StoreDicomResponse> StoreMultiPartDicomResourceAsync(
            Stream contentStream,
            string requestContentType,
            string studyInstanceUid,
            CancellationToken cancellationToken = default);
    }
}
