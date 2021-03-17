// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="CustomTagStoreEntry"/>.
    /// </summary>
    public static class CustomTagStoreEntryExtensions
    {
        /// <summary>
        /// Create  <see cref="CustomTagEntry"/> from  <see cref="CustomTagStoreEntry"/>.
        /// </summary>
        /// <param name="storeEntry">The custom tag store entry.</param>
        /// <returns>The custom tag entry.</returns>
        public static CustomTagEntry ToCustomTagEntry(this CustomTagStoreEntry storeEntry)
        {
            EnsureArg.IsNotNull(storeEntry, nameof(storeEntry));
            return new CustomTagEntry() { Path = storeEntry.Path, VR = storeEntry.VR, PrivateCreator = storeEntry.PrivateCreator, Level = storeEntry.Level, Status = storeEntry.Status };
        }
    }
}
