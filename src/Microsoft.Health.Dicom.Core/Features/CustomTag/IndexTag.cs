// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Indexable Dicom Tag.
    /// </summary>
    public class IndexTag
    {
        public IndexTag(DicomTag tag, DicomVR vr, CustomTagLevel level, CustomTagStoreEntry customTagStoreEntry)
        {
            EnsureArg.IsNotNull(tag, nameof(tag));
            EnsureArg.IsNotNull(vr, nameof(vr));

            Tag = tag;
            VR = vr;
            Level = level;
            CustomTagStoreEntry = customTagStoreEntry;
        }

        /// <summary>
        /// Gets Dicom Tag.
        /// </summary>
        public DicomTag Tag { get; }

        /// <summary>
        /// Gets Dicom VR.
        /// </summary>
        public DicomVR VR { get; }

        /// <summary>
        /// Gets Dicom Tag Level.
        /// </summary>
        public CustomTagLevel Level { get; }

        /// <summary>
        /// Gets whether this is custom tag or not.
        /// </summary>
        public bool IsCustomTag => CustomTagStoreEntry != null;

        /// <summary>
        /// Gets the underlying customTagStoreEntry for custom tag.
        /// </summary>
        public CustomTagStoreEntry CustomTagStoreEntry { get; }

        /// <summary>
        /// Build IndexTag for core dicom tag.
        /// </summary>
        /// <param name="tag">the core dicom tag.</param>
        /// <param name="level">The level</param>
        /// <returns>The index tag</returns>
        public static IndexTag FromCoreDicomTag(DicomTag tag, CustomTagLevel level)
        {
            EnsureArg.IsNotNull(tag, nameof(tag));
            return new IndexTag(tag, tag.GetDefaultVR(), level, null);
        }

        /// <summary>
        /// Build IndexTag from customtag store entry..
        /// </summary>
        /// <param name="entry">the custom tag store entry.</param>
        /// <returns>The index tag</returns>
        public static IndexTag FromCustomTagStoreEntry(CustomTagStoreEntry entry)
        {
            EnsureArg.IsNotNull(entry, nameof(entry));
            return new IndexTag(DicomTag.Parse(entry.Path), DicomVR.Parse(entry.VR), entry.Level, entry);
        }
    }
}
