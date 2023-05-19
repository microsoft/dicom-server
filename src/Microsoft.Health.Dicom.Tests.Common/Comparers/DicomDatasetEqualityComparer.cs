// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.Imaging;

namespace Microsoft.Health.Dicom.Tests.Common.Comparers;

public class DicomDatasetEqualityComparer : IEqualityComparer<DicomDataset>
{
    public static DicomDatasetEqualityComparer Default { get; } = new DicomDatasetEqualityComparer();

    private readonly ISet<DicomTag> _ignoredTags;

    public DicomDatasetEqualityComparer()
        : this(new HashSet<DicomTag>())
    {
    }

    public DicomDatasetEqualityComparer(ISet<DicomTag> ignoredTags)
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

        _ignoredTags.Add(DicomTag.PixelData);
        // Compare DicomItems except PixelData, since DicomItemCollectionEqualityComparer cannot handle it
        IEqualityComparer<IEnumerable<DicomItem>> dicomItemsComparaer = new DicomItemCollectionEqualityComparer(_ignoredTags);

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
