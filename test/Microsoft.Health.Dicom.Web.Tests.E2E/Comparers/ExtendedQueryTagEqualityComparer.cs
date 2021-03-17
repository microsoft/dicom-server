// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Comparers
{
    public class ExtendedQueryTagEqualityComparer : IEqualityComparer<ExtendedQueryTag>
    {
        public ExtendedQueryTagEqualityComparer(bool ignoreStatus)
        {
            IgnoreStatus = ignoreStatus;
        }

        public bool IgnoreStatus { get; }

        public bool Equals(ExtendedQueryTag x, ExtendedQueryTag y)
        {
            if (x == null || y == null)
            {
                return x == y;
            }

            return x.Path.Equals(y.Path, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.VR, y.VR, StringComparison.OrdinalIgnoreCase)
                && x.Level == y.Level
                && (IgnoreStatus ? true : x.Status == y.Status);
        }

        public int GetHashCode([DisallowNull] ExtendedQueryTag obj)
        {
            return HashCode.Combine(obj.Path, obj.VR, obj.Level.GetHashCode(), obj.Status.GetHashCode());
        }
    }
}
