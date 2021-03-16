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
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexTag"/> class.
        /// </summary>
        /// <remarks>Used for constuctoring from core dicom tag.PatientName e.g. </remarks>
        /// <param name="tag">The core dicom Tag.</param>
        /// <param name="level">The tag level.</param>
        public IndexTag(DicomTag tag, CustomTagLevel level)
        {
            EnsureArg.IsNotNull(tag, nameof(tag));

            Tag = tag;
            VR = tag.GetDefaultVR();
            Level = level;
            CustomTagStoreEntry = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexTag"/> class.
        /// </summary>
        /// <remarks>Used for constuctoring from custom tags.</remarks>
        /// <param name="entry">The custom tag store entry.</param>
        public IndexTag(CustomTagStoreEntry entry)
        {
            EnsureArg.IsNotNull(entry, nameof(entry));

            Tag = DicomTag.Parse(entry.Path);
            VR = DicomVR.Parse(entry.VR);
            Level = entry.Level;
            CustomTagStoreEntry = entry;
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
    }
}
