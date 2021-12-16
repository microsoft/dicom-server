// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;

namespace Microsoft.Health.Dicom.Core.Features.Model
{
    /// <summary>
    /// Represents a Work Item.
    /// </summary>
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly struct Workitem : IEquatable<Workitem>
    {


        public bool Equals(Workitem other)
            => GetHashCode() == other.GetHashCode();

        public override bool Equals(object obj)
            => obj is Workitem && Equals((Workitem)obj);

        // TODO: to be identified
        public override int GetHashCode() =>
            base.GetHashCode();

        public static bool operator ==(Workitem left, Workitem right)
            => left.Equals(right);

        public static bool operator !=(Workitem left, Workitem right)
            => !(left == right);
    }
}
