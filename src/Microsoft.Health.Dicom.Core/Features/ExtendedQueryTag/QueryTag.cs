// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Query;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Queryable Dicom Tag.
    /// </summary>
    public class QueryTag
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryTag"/> class.
        /// </summary>
        /// <remarks>Used for constuctoring from core dicom tag.PatientName e.g. </remarks>
        /// <param name="tag">The core dicom Tag.</param>
        public QueryTag(DicomTag tag)
        {
            EnsureArg.IsNotNull(tag, nameof(tag));

            Item = new DicomValuelessItem(tag);
            Tag = tag;
            VR = tag.GetDefaultVR();
            Level = QueryLimit.GetQueryTagLevel(tag);
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
            Item = new DicomValuelessItem(Tag);
            VR = DicomVR.Parse(entry.VR);
            Level = entry.Level;
            ExtendedQueryTagStoreEntry = entry;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryTag"/> class.
        /// </summary>
        /// <remarks>Used for constructing from <see cref="DicomItem"/> (to model sequences).</remarks>
        /// <param name="item">The Dicom item.</param>
        public QueryTag(DicomItem item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            Item = item;
            Tag = Item.Tag;
            VR = Item.ValueRepresentation;
            Level = QueryLimit.GetQueryTagLevel(Tag);
            ExtendedQueryTagStoreEntry = null;
        }

        /// <summary>
        /// Get the DicomItem for this tag.
        /// </summary>
        public DicomItem Item { get; }

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
        public QueryTagEntry ExtendedQueryTagStoreEntry { get; }

        /// <summary>
        /// Gets name of this query tag.
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return Tag.DictionaryEntry == DicomDictionary.UnknownTag ? Tag.GetPath() : Tag.DictionaryEntry.Keyword;
        }
    }
}
