// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// ExtendedQueryTag row version.
    /// </summary>
    public class ExtendedQueryTagETag
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryTag"/> class.
        /// </summary>
        /// <remarks>Used for constuctoring from core dicom tag.PatientName e.g. </remarks>
        /// <param name="rowVersions">The core dicom Tag.</param>        
        public ExtendedQueryTagETag(IReadOnlyCollection<ExtendedQueryTagVersion> rowVersions)
        {
            EnsureArg.IsNotNull(rowVersions, nameof(rowVersions));
            ETag = rowVersions.Count == 0 ? null : rowVersions.Max();
        }

        public static ExtendedQueryTagETag FromQueryTags(IReadOnlyCollection<QueryTag> queryTags)
        {
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));
            return new ExtendedQueryTagETag(queryTags
                .Where(x => x.IsExtendedQueryTag && x.ExtendedQueryTagStoreEntry.Version.HasValue)
                .Select(x => x.ExtendedQueryTagStoreEntry.Version.Value)
                .ToList());
        }

        public ExtendedQueryTagVersion? ETag { get; }
    }
}
