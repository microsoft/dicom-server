// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Reindex
{
    public class ReindexTagStoreEntry
    {
        public ExtendedQueryTagStoreEntry QueryTagStoreEntry { get; set; }

        public long OperationKey { get; set; }

        public long EndWatarmark { get; set; }

        public ReindexTagStatus Status { get; set; }
    }
}
