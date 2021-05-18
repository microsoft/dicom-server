// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom.Imaging;

namespace Microsoft.Health.Dicom.Tests.Common.Comparers
{
    public class DicomPixelDataEqualityComparer : IEqualityComparer<DicomPixelData>
    {
        public static DicomPixelDataEqualityComparer Default => new DicomPixelDataEqualityComparer();

        public bool Equals(DicomPixelData x, DicomPixelData y)
        {
            if (x == null || y == null)
            {
                return object.ReferenceEquals(x, y);
            }

            if (x.NumberOfFrames != y.NumberOfFrames)
            {
                return false;
            }

            if (!x.PhotometricInterpretation.Equals(y.PhotometricInterpretation))
            {
                return false;
            }

            if (!x.BitsAllocated.Equals(y.BitsAllocated))
            {
                return false;
            }

            if (!x.BitsStored.Equals(y.BitsStored))
            {
                return false;
            }

            if (!x.Height.Equals(y.Height))
            {
                return false;
            }

            if (!x.Width.Equals(y.Width))
            {
                return false;
            }

            for (int i = 0; i < x.NumberOfFrames; i++)
            {
                if (!x.GetFrame(i).Data.SequenceEqual(y.GetFrame(i).Data))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(DicomPixelData obj)
        {
            return obj.GetHashCode();
        }
    }
}
