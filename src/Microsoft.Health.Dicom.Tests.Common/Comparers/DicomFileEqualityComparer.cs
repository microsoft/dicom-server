// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Tests.Common.Comparers
{
    public class DicomFileEqualityComparer : IEqualityComparer<DicomFile>
    {
        public static DicomFileEqualityComparer Default { get; } = new DicomFileEqualityComparer();

        private readonly DicomItemCollectionEqualityComparer _metadataComparer;
        private readonly DicomDatasetEqualityComparer _datasetComparer;
        private readonly IEnumerable<DicomTag> _ignoredTags;

        public DicomFileEqualityComparer()
           : this(Array.Empty<DicomTag>())
        {
        }

        public DicomFileEqualityComparer(IEnumerable<DicomTag> ignoredTags)
        {
            EnsureArg.IsNotNull(ignoredTags, nameof(ignoredTags));
            _ignoredTags = ignoredTags;
            _metadataComparer = new DicomItemCollectionEqualityComparer(_ignoredTags);
            _datasetComparer = new DicomDatasetEqualityComparer(_ignoredTags);
        }

        public bool Equals(DicomFile x, DicomFile y)
        {
            if (x == null || y == null)
            {
                return object.ReferenceEquals(x, y);
            }

            if (!_metadataComparer.Equals(x.FileMetaInfo, y.FileMetaInfo))
            {
                return false;
            }

            return _datasetComparer.Equals(x.Dataset, y.Dataset);
        }

        public int GetHashCode(DicomFile obj)
        {
            return obj.GetHashCode();
        }
    }
}
