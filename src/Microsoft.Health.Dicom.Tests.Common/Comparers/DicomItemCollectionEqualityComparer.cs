// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;

namespace Microsoft.Health.Dicom.Tests.Common.Comparers
{
    public class DicomItemCollectionEqualityComparer : IEqualityComparer<IEnumerable<DicomItem>>
    {
        public DicomItemCollectionEqualityComparer()
            : this(new DicomTag[0])
        {
        }

        public DicomItemCollectionEqualityComparer(IEnumerable<DicomTag> ignoredTags) => IgnoredTags = ignoredTags;

        public IEnumerable<DicomTag> IgnoredTags { get; }

        bool IEqualityComparer<IEnumerable<DicomItem>>.Equals(IEnumerable<DicomItem> x, IEnumerable<DicomItem> y)
        {
            if (x == null || y == null)
            {
                return object.ReferenceEquals(x, y);
            }

            IEqualityComparer<DicomItem> dicomItemComparer = new DicomItemEqualityComparer();

            ISet<DicomTag> ignoredSet = new HashSet<DicomTag>(IgnoredTags);
            Dictionary<DicomTag, DicomItem> xDict = x.ToDictionary(item => item.Tag);
            Dictionary<DicomTag, DicomItem> yDict = y.ToDictionary(item => item.Tag);
            if (xDict.Count != yDict.Count)
            {
                return false;
            }

            foreach (DicomTag tag in xDict.Keys)
            {
                if (ignoredSet.Contains(tag))
                {
                    continue;
                }

                if (!yDict.ContainsKey(tag))
                {
                    return false;
                }

                if (!dicomItemComparer.Equals(xDict[tag], yDict[tag]))
                {
                    return false;
                }
            }

            return true;
        }

        int IEqualityComparer<IEnumerable<DicomItem>>.GetHashCode(IEnumerable<DicomItem> obj)
        {
            return obj.GetHashCode();
        }
    }
}
