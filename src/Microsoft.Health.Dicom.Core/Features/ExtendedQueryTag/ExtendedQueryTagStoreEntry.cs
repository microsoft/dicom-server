// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Represent each extended query tag entry has retrieved from the store.
    /// </summary>
    public class ExtendedQueryTagStoreEntry : ExtendedQueryTagEntry
    {
        public ExtendedQueryTagStoreEntry(int key, string path, string vr, string privateCreator, QueryTagLevel level, ExtendedQueryTagStatus status, QueryTagQueryStatus queryStatus, int errorCount)
        {
            Key = key;
            Path = EnsureArg.IsNotNullOrWhiteSpace(path);
            VR = EnsureArg.IsNotNullOrWhiteSpace(vr);
            PrivateCreator = privateCreator;
            Level = EnsureArg.EnumIsDefined(level);
            Status = EnsureArg.EnumIsDefined(status);
            QueryStatus = EnsureArg.EnumIsDefined(queryStatus);
            ErrorCount = EnsureArg.IsGte(errorCount, 0, nameof(errorCount));
        }

        /// <summary>
        /// Key of this extended query tag entry.
        /// </summary>
        public int Key { get; }

        /// <summary>
        /// Status of this tag.
        /// </summary>
        public ExtendedQueryTagStatus Status { get; }

        /// <summary>
        /// Level of this tag. Could be Study, Series or Instance.
        /// </summary>
        public QueryTagLevel Level { get; }

        /// <summary>
        /// Query status of this tag.
        /// </summary>
        public QueryStatus QueryStatus { get; }

        /// <summary>
        /// Error count on this tag.
        /// </summary>
        public int ErrorCount { get; }

        /// <summary>
        /// Convert to  <see cref="GetExtendedQueryTagEntry"/>.
        /// </summary>
        /// <returns>The extended query tag entry.</returns>
        public GetExtendedQueryTagEntry ToExtendedQueryTagEntry()
        {
            return new GetExtendedQueryTagEntry { Path = Path, VR = VR, PrivateCreator = PrivateCreator, Level = Level, Status = Status };
        }
    }
}
