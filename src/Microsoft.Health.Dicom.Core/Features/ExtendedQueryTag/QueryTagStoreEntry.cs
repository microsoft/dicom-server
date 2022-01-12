// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Representation of a workitem query tag entry.
    /// </summary>
    public class WorkitemQueryTagStoreEntry : QueryTagEntry
    {
        /// <summary>
        /// Key of this extended query tag entry.
        /// </summary>
        public int Key { get; }

        /// <summary>
        /// Query status of this tag.
        /// </summary>
        public QueryStatus QueryStatus { get; }
    }
}
