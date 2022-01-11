// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace TestTagPath
{
    /// <summary>
    /// Represents a path composed of multiple tags to model a specific tag in a sequence.
    /// </summary>
    public class TagOnlyDicomItem : DicomItem
    {
        /// <summary>
        /// Make a tag path from Dicom tags.
        /// </summary>
        /// <param name="tag"></param>
        public TagOnlyDicomItem(DicomTag tag)
            : base(tag)
        {
        }

        public override DicomVR? ValueRepresentation => Tag.DictionaryEntry.ValueRepresentations.FirstOrDefault();
    }
}
