// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.SqlServer.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="ExtendedQueryTagStoreEntryExtensions"/>.
    /// </summary>
    public static class ExtendedQueryTagStoreEntryExtensions
    {
        /// <summary>
        /// Get max tag version from a collection of <see cref="ExtendedQueryTagStoreEntry"/>
        /// </summary>
        /// <param name="entries">The collection of <see cref="ExtendedQueryTagStoreEntry"/></param>
        /// <returns>The max tag version. Return null if collection if empty</returns>
        public static ulong? GetMaxTagVersion(this IEnumerable<ExtendedQueryTagStoreEntry> entries)
        {
            EnsureArg.IsNotNull(entries, nameof(entries));
            return entries.Select(x => x.Version).DefaultIfEmpty(null).Max();
        }
    }
}
