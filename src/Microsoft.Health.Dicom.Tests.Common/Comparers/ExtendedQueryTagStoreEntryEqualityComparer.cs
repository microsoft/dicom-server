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
    public class ExtendedQueryTagStoreEntryEqualityComparer : IEqualityComparer<ExtendedQueryTagStoreEntry>
    {
        public static ExtendedQueryTagStoreEntryEqualityComparer Default => new ExtendedQueryTagStoreEntryEqualityComparer();

        public bool Equals(ExtendedQueryTagStoreEntry x, ExtendedQueryTagStoreEntry y)
        {
            if (x == null || y == null)
            {
                return x == y;
            }

            return x.Path.Equals(y.Path, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.VR, y.VR, StringComparison.OrdinalIgnoreCase)
                && x.PrivateCreator == y.PrivateCreator
                && x.Level == y.Level
                && x.Status == y.Status
                && x.Key == y.Key;
        }

        public int GetHashCode(ExtendedQueryTagStoreEntry extendedQueryTagStoreEntry)
        {
            EnsureArg.IsNotNull(extendedQueryTagStoreEntry, nameof(extendedQueryTagStoreEntry));
            return HashCode.Combine(
                extendedQueryTagStoreEntry.Path,
                extendedQueryTagStoreEntry.VR,
                extendedQueryTagStoreEntry.PrivateCreator,
                extendedQueryTagStoreEntry.Level,
                extendedQueryTagStoreEntry.Status,
                extendedQueryTagStoreEntry.Key);
        }
    }
}
