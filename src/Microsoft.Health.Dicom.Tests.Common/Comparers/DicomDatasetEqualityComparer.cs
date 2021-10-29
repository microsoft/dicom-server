// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using FellowOakDicom;
using Dicom.Imaging;
using EnsureThat;

namespace Microsoft.Health.Dicom.Tests.Common.Comparers
{
    public class DicomDatasetEqualityComparer : IEqualityComparer<DicomDataset>
    {
        public static DicomDatasetEqualityComparer Default { get; } = new DicomDatasetEqualityComparer();

        private readonly IEnumerable<DicomTag> _ignoredTags;

        public DicomDatasetEqualityComparer()
            : this(Array.Empty<DicomTag>())
        {
        }

        public DicomDatasetEqualityComparer(IEnumerable<DicomTag> ignoredTags)
        {
            EnsureArg.IsNotNull(ignoredTags, nameof(ignoredTags));
            _ignoredTags = ignoredTags;
        }


        public bool Equals(DicomDataset x, DicomDataset y)
        {
            if (x == null || y == null)
            {
                return object.ReferenceEquals(x, y);
            }

            // Compare DicomItems except PixelData, since DicomItemCollectionEqualityComparer cannot handle it
            IEqualityComparer<IEnumerable<DicomItem>> dicomItemsComparaer = new DicomItemCollectionEqualityComparer(_ignoredTags.Concat(new[] { DicomTag.PixelData }));

            if (!dicomItemsComparaer.Equals(x, y))
            {
                return false;
            }

            IEqualityComparer<DicomPixelData> dicomPixelDataComparer = new DicomPixelDataEqualityComparer();

            // Compare PixelData
            return dicomPixelDataComparer.Equals(DicomPixelData.Create(x), DicomPixelData.Create(y));
        }

        public int GetHashCode(DicomDataset obj)
        {
            return obj.GetHashCode();
        }
    }
}
