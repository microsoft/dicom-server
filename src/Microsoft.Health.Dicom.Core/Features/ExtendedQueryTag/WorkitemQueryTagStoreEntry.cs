// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Representation of a workitem query tag entry.
    /// </summary>
    public class WorkitemQueryTagStoreEntry : QueryTagEntry
    {
        public WorkitemQueryTagStoreEntry(int key, string path, string vr, QueryStatus queryStatus)
        {
            Key = key;
            Path = EnsureArg.IsNotNullOrWhiteSpace(path);
            VR = EnsureArg.IsNotNullOrWhiteSpace(vr);
            QueryStatus = EnsureArg.EnumIsDefined(queryStatus);
        }

        /// <summary>
        /// Key of this extended query tag entry.
        /// </summary>
        public int Key { get; }

        /// <summary>
        /// Query status of this tag.
        /// </summary>
        public QueryStatus QueryStatus { get; }

        /// <summary>
        /// Get the DicomItem for this tag.
        /// </summary>
        public DicomItem Item { get; }
    }
}
