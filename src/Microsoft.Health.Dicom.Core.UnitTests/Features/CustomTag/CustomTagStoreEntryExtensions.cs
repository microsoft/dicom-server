// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag
{
    public static class CustomTagStoreEntryExtensions
    {
        /// <summary>
        /// Build TagPath Dictionary from CustomTagStoreEntry list.
        /// </summary>
        /// <param name="customTagStoreEntries">The CustomTagStoreEntry list.</param>
        /// <returns>The dictionary.</returns>
        public static IDictionary<string, CustomTagStoreEntry> ToTagPathDictionary(this IEnumerable<CustomTagStoreEntry> customTagStoreEntries)
        {
            return customTagStoreEntries.ToDictionary(
                   keySelector: entry => entry.Path,
                   comparer: StringComparer.OrdinalIgnoreCase);
        }
    }
}
