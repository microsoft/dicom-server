// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Tests.Common.Comparers
{
    public class DicomFileEqualityComparer : IEqualityComparer<DicomFile>
    {
        public DicomFileEqualityComparer()
           : this(new DicomTag[0])
        {
        }

        public DicomFileEqualityComparer(IEnumerable<DicomTag> ignoredTags)
        {
            EnsureArg.IsNotNull(ignoredTags, nameof(ignoredTags));
            IgnoredTags = ignoredTags;
        }

        bool IEqualityComparer<DicomFile>.Equals(DicomFile x, DicomFile y)
        {
            if (x == null || y == null)
            {
                return object.ReferenceEquals(x, y);
            }

            IEqualityComparer<IEnumerable<DicomItem>> metadataComparer = new DicomItemCollectionEqualityComparer(IgnoredTags);
            if (!metadataComparer.Equals(x.FileMetaInfo, y.FileMetaInfo))
            {
                return false;
            }

            IEqualityComparer<DicomDataset> dataSetComparer = new DicomDatasetEqualityComparer(IgnoredTags);
            return dataSetComparer.Equals(x.Dataset, y.Dataset);
        }

        int IEqualityComparer<DicomFile>.GetHashCode(DicomFile obj)
        {
            return obj.GetHashCode();
        }

        public IEnumerable<DicomTag> IgnoredTags { get; }
    }
}
