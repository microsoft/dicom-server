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
        public CustomTagFilterDetails(int tagKey, CustomTagLevel tagLevel, DicomVR vr, DicomTag tag)
        {
            Key = tagKey;
            VR = vr;
            Level = tagLevel;
            Tag = tag;
        }

        public CustomTagFilterDetails(DicomTag tag)
        {
            Tag = tag;
        }

        public int Key { get; }

        public DicomVR VR { get; }

        public CustomTagLevel Level { get; }

        public DicomTag Tag { get; }

        public override string ToString()
        {
            return $"Key: {Key}, VR {VR.Code} Level:{Level} Tag:{Tag.ToString()}";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Tag.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CustomTagFilterDetails);
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
