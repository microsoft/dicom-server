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
    /// ExtendedQueryTags version.
    /// </summary>
    public class ExtendedQueryTagsVersion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedQueryTagsVersion"/> class.
        /// </summary>
        /// <param name="tagVersions">The tag versions.</param>        
        public ExtendedQueryTagsVersion(IReadOnlyCollection<ExtendedQueryTagVersion> tagVersions)
        {
            TagVerions = EnsureArg.IsNotNull(tagVersions, nameof(tagVersions));
            Version = tagVersions.Count == 0 ? null : tagVersions.Max();
        }

        /// <summary>
        /// Create <see cref="ExtendedQueryTagsVersion"/> from QueryTag collection.
        /// </summary>
        /// <param name="queryTags">The QueryTag collection.</param>
        /// <returns>The ExtendedQueryTagETag.</returns>
        public static ExtendedQueryTagsVersion FromQueryTags(IReadOnlyCollection<QueryTag> queryTags)
        {
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));
            return new ExtendedQueryTagsVersion(queryTags
                .Where(x => x.IsExtendedQueryTag && x.ExtendedQueryTagStoreEntry.Version.HasValue)
                .Select(x => x.ExtendedQueryTagStoreEntry.Version.Value)
                .ToList());
        }

        /// <summary>
        /// Gets collection of TagVersions.
        /// </summary>
        public IReadOnlyCollection<ExtendedQueryTagVersion> TagVerions { get; }

        /// <summary>
        /// Gets version of the set of tag versions.
        /// </summary>
        public ExtendedQueryTagVersion? Version { get; }
    }
}
