// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="CustomTagStoreEntry"/>.
    /// </summary>
    internal static class CustomTagStoreEntryExtensions
    {
        /// <summary>
        /// Convert to Index Tag.
        /// </summary>
        /// <param name="customTagStoreEntry">The custom tag store entry.</param>
        /// <returns>The index Tag.</returns>
        public static IndexTag Convert(this CustomTagStoreEntry customTagStoreEntry)
        {
            EnsureArg.IsNotNull(customTagStoreEntry, nameof(customTagStoreEntry));
            DicomTag tag = DicomTag.Parse(customTagStoreEntry.Path);
            DicomVR vr = DicomVR.Parse(customTagStoreEntry.VR);
            return new IndexTag(tag, vr, customTagStoreEntry.Level, isCustomTag: true);
        }
    }
}
