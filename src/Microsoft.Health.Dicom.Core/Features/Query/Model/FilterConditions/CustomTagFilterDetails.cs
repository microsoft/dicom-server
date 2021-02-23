// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    /// <summary>
    /// Details required to create a custom tag filter condition
    /// </summary>
    public class CustomTagFilterDetails : IEquatable<CustomTagFilterDetails>
    {
        public CustomTagFilterDetails(long tagKey, CustomTagLevel tagLevel, DicomTag tag)
        {
            Key = tagKey;
            Level = tagLevel;
            Tag = tag;
        }

        public CustomTagFilterDetails(DicomTag tag)
        {
            Tag = tag;
        }

        public long Key { get; }

        public CustomTagLevel Level { get; }

        public DicomTag Tag { get; }

        public override string ToString()
        {
            return $"Key: {Key}, Level:{Level} Tag:{Tag}";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Tag.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            CustomTagFilterDetails other = obj as CustomTagFilterDetails;
            if (other == null)
            {
                return false;
            }

            return Tag == other.Tag;
        }

        public bool Equals(CustomTagFilterDetails other)
        {
            if (other == null)
            {
                return false;
            }

            return Tag == other.Tag;
        }
    }
}
