// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;

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
        public bool IsCustomTag { get => CustomTagStoreEntry != null; }

        public CustomTagStoreEntry CustomTagStoreEntry { get; }
    }
}
