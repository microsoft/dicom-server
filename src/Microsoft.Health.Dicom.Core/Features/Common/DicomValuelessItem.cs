// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    /// <summary>
    /// A DicomItem with a tag but no value - used to model either concrete DicomElements or DicomSequences.
    /// </summary>
    public class DicomValuelessItem : DicomItem
    {
        /// <summary>
        /// Creates a valueless DicomItem from a tag.
        /// </summary>
        /// <param name="tag"></param>
        public DicomValuelessItem(DicomTag tag)
            : base(tag)
        {
        }

        public override DicomVR ValueRepresentation => Tag.DictionaryEntry.ValueRepresentations.FirstOrDefault();
    }
}
