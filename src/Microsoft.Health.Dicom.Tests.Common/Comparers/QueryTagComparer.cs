// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Tests.Common.Comparers
{
    public class QueryTagComparer : IEqualityComparer<QueryTag>
    {
        public static QueryTagComparer Default => new QueryTagComparer();

        public bool Equals(QueryTag x, QueryTag y)
        {
            if (x == null || y == null)
            {
                return x == y;
            }

            return x.Tag == y.Tag &&
                x.VR == y.VR
                && x.Level == y.Level
                && ExtendedQueryTagStoreEntryEqualityComparer.Default.Equals(x.ExtendedQueryTagStoreEntry, y.ExtendedQueryTagStoreEntry);
        }

        public int GetHashCode(QueryTag queryTag)
        {
            EnsureArg.IsNotNull(queryTag, nameof(queryTag));
            return HashCode.Combine(
                queryTag.Tag,
                queryTag.VR,
                queryTag.Level,
                queryTag.ExtendedQueryTagStoreEntry);
        }
    }
}
