// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Indexable Dicom Tag.
    /// </summary>
    public class QueryTag
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryTag"/> class.
        /// </summary>
        /// <remarks>Used for constuctoring from core dicom tag.PatientName e.g. </remarks>
        /// <param name="tag">The core dicom Tag.</param>
        /// <param name="level">The tag level.</param>
        public QueryTag(DicomTag tag, QueryTagLevel level)
        {
            EnsureArg.IsNotNull(tag, nameof(tag));

            Tag = tag;
            VR = tag.GetDefaultVR();
            Level = level;
            ExtendedQueryTagStoreEntry = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryTag"/> class.
        /// </summary>
        /// <remarks>Used for constuctoring from extended query tags.</remarks>
        /// <param name="entry">The extended query tag store entry.</param>
        public QueryTag(ExtendedQueryTagStoreEntry entry)
        {
            EnsureArg.IsNotNull(entry, nameof(entry));
            string fullPath = string.IsNullOrEmpty(entry.PrivateCreator) ? entry.Path : $"{entry.Path}:{entry.PrivateCreator}";
            Tag = DicomTag.Parse(fullPath);
            VR = DicomVR.Parse(entry.VR);
            Level = entry.Level;
            ExtendedQueryTagStoreEntry = entry;
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
        public QueryTagLevel Level { get; }

        /// <summary>
        /// Gets whether this is extended query tag or not.
        /// </summary>
        public bool IsExtendedQueryTag => ExtendedQueryTagStoreEntry != null;

        /// <summary>
        /// Gets the underlying extendedQueryTagStoreEntry for extended query tag.
        /// </summary>
        public ExtendedQueryTagStoreEntry ExtendedQueryTagStoreEntry { get; }
    }
}
