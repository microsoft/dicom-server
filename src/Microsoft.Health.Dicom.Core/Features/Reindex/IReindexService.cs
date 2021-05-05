// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public interface IReindexService
    {
        Task<string> StartNewReindexJob(IEnumerable<ExtendedQueryTagStoreEntry> extendedQueryTagStoreEntries, CancellationToken cancellationToken = default);

        Task RemoveTagFromReindexing(ExtendedQueryTagStoreEntry extendedQueryTagEntry, CancellationToken cancellationToken = default);

    }
}
