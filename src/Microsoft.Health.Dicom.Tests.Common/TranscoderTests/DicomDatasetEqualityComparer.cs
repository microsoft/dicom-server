// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;
using Dicom.Imaging;

namespace Microsoft.Health.Dicom.Tests.Common.TranscoderTests
{
    /// <summary>
    /// Compare if 2 DicomItem equals or not.
    /// Most DicomItem doesn't have method Equals implented, so has to make it ourselves.
    /// </summary>
    public class DicomDatasetEqualityComparer : IEqualityComparer<DicomDataset>
    {
        public DicomDatasetEqualityComparer()
            : this(new DicomTag[0])
        {
        }

        public DicomDatasetEqualityComparer(IEnumerable<DicomTag> ignoredTags) => IgnoredTags = ignoredTags;

        public IEnumerable<DicomTag> IgnoredTags { get; }

        bool IEqualityComparer<DicomDataset>.Equals(DicomDataset x, DicomDataset y)
        {
            if (x == null || y == null)
            {
                return object.ReferenceEquals(x, y);
            }

            if (!x.InternalTransferSyntax.Equals(y.InternalTransferSyntax))
            {
                return false;
            }

            IEqualityComparer<IEnumerable<DicomItem>> dicomItemsComparaer = new DicomItemCollectionEqualityComparer(IgnoredTags.Concat(new[] { DicomTag.PixelData }));

            if (!dicomItemsComparaer.Equals(x, y))
            {
                return false;
            }

            IEqualityComparer<DicomPixelData> dicomPixelDataComparer = new DicomPixelDataEqualityComparer();

            // Verify PixelData
            return dicomPixelDataComparer.Equals(DicomPixelData.Create(x), DicomPixelData.Create(y));
        }

        int IEqualityComparer<DicomDataset>.GetHashCode(DicomDataset obj)
        {
            return obj.GetHashCode();
        }
    }
}
