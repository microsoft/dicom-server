// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Tests.Common.Comparers
{
    public class DicomItemCollectionEqualityComparer : IEqualityComparer<IEnumerable<DicomItem>>
    {
        public static DicomItemCollectionEqualityComparer Default { get; } = new DicomItemCollectionEqualityComparer();

        private readonly IEnumerable<DicomTag> _ignoredTags;

        public DicomItemCollectionEqualityComparer()
            : this(new DicomTag[] { DicomTag.ImplementationVersionName })
        {
        }

        public DicomItemCollectionEqualityComparer(IEnumerable<DicomTag> ignoredTags)
        {
            EnsureArg.IsNotNull(ignoredTags, nameof(ignoredTags));
            _ignoredTags = ignoredTags;
        }


        public bool Equals(IEnumerable<DicomItem> x, IEnumerable<DicomItem> y)
        {
            if (x == null || y == null)
            {
                return object.ReferenceEquals(x, y);
            }

            IEqualityComparer<DicomItem> dicomItemComparer = new DicomItemEqualityComparer();

            ISet<DicomTag> ignoredSet = new HashSet<DicomTag>(_ignoredTags);
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

        public int GetHashCode(IEnumerable<DicomItem> obj)
        {
            return obj.GetHashCode();
        }
    }
}
