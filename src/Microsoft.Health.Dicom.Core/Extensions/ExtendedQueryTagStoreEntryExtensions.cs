// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    /// <summary>
    /// A collection of <see langword="static"/> methods for interacting with <see cref="ExtendedQueryTagStoreEntry"/>.
    /// </summary>
    public static class ExtendedQueryTagStoreEntryExtensions
    {
        /// <summary>
        /// Gets the corresponding <see cref="ExtendedQueryTagReference"/> for an <paramref name="entry"/>.
        /// </summary>
        /// <param name="entry">An <see cref="ExtendedQueryTagStoreEntry"/>.</param>
        /// <returns>The reference for the added entry.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="entry"/> is <see langword="null"/>.</exception>
        public static ExtendedQueryTagReference ToReference(this ExtendedQueryTagStoreEntry entry)
        {
            EnsureArg.IsNotNull(entry, nameof(entry));
            return new ExtendedQueryTagReference { Key = entry.Key, Path = entry.Path };
        }
    }
}
