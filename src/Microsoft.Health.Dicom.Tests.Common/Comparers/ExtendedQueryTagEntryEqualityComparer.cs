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
    public class ExtendedQueryTagEntryEqualityComparer : IEqualityComparer<GetExtendedQueryTagEntry>
    {
        public static ExtendedQueryTagEntryEqualityComparer Default => new ExtendedQueryTagEntryEqualityComparer();

        public bool Equals(GetExtendedQueryTagEntry x, GetExtendedQueryTagEntry y)
        {
            if (x == null || y == null)
            {
                return x == y;
            }

            return x.Path.Equals(y.Path, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.VR, y.VR, StringComparison.OrdinalIgnoreCase)
                && x.PrivateCreator == y.PrivateCreator
                && x.Level == y.Level
                && x.Status == y.Status;
        }

        public int GetHashCode(GetExtendedQueryTagEntry extendedQueryTagEntry)
        {
            EnsureArg.IsNotNull(extendedQueryTagEntry, nameof(extendedQueryTagEntry));
            return HashCode.Combine(
                extendedQueryTagEntry.Path,
                extendedQueryTagEntry.VR,
                extendedQueryTagEntry.PrivateCreator,
                extendedQueryTagEntry.Level,
                extendedQueryTagEntry.Status);
        }
    }
}
