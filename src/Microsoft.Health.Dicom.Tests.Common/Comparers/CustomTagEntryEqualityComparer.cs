// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Dicom.Tests.Common.Comparers
{
    public class CustomTagEntryEqualityComparer : IEqualityComparer<CustomTagEntry>
    {
        public bool Equals(CustomTagEntry x, CustomTagEntry y)
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

        public int GetHashCode([DisallowNull] CustomTagEntry obj)
        {
            return HashCode.Combine(obj.Path, obj.VR, obj.PrivateCreator, obj.Level.GetHashCode(), obj.Status.GetHashCode());
        }
    }
}
