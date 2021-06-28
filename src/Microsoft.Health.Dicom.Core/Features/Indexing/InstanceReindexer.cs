// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    /// <summary>
    /// Represents an Reindexer which reindexes DICOM instance.
    /// </summary>
    public class InstanceReindexer : IInstanceReindexer
    {
        public Task ReindexInstanceAsync(IReadOnlyList<ExtendedQueryTagStoreEntry> entries, long watermark, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}
