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
    /// ExtendedQueryTag ETag.
    /// </summary>
    public class ExtendedQueryTagETag
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedQueryTagETag"/> class.
        /// </summary>
        /// <param name="tagVersions">The tag versions.</param>        
        public ExtendedQueryTagETag(IReadOnlyCollection<ExtendedQueryTagVersion> tagVersions)
        {
            TagVerions = EnsureArg.IsNotNull(tagVersions, nameof(tagVersions));
            ETag = tagVersions.Count == 0 ? null : tagVersions.Max();
        }

        /// <summary>
        /// Create   ExtendedQueryTagETag from QueryTag collection.
        /// </summary>
        /// <param name="queryTags">The QueryTag collection.</param>
        /// <returns>The ExtendedQueryTagETag.</returns>
        public static ExtendedQueryTagETag FromQueryTags(IReadOnlyCollection<QueryTag> queryTags)
        {
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));
            return new ExtendedQueryTagETag(queryTags
                .Where(x => x.IsExtendedQueryTag && x.ExtendedQueryTagStoreEntry.Version.HasValue)
                .Select(x => x.ExtendedQueryTagStoreEntry.Version.Value)
                .ToList());
        }

        /// <summary>
        /// Gets collection of TagVersions.
        /// </summary>
        public IReadOnlyCollection<ExtendedQueryTagVersion> TagVerions { get; }

        /// <summary>
        /// Gets ETag.
        /// </summary>
        public ExtendedQueryTagVersion? ETag { get; }
    }
}
