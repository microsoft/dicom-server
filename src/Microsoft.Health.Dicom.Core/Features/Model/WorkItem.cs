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
    public readonly struct WorkItem : IEquatable<WorkItem>
    {


        public bool Equals(WorkItem other)
            => GetHashCode() == other.GetHashCode();

        public override bool Equals(object obj)
            => obj is WorkItem && Equals((WorkItem)obj);

        // TODO: to be identified
        public override int GetHashCode() =>
            base.GetHashCode();

        public static bool operator ==(WorkItem left, WorkItem right)
            => left.Equals(right);

        public static bool operator !=(WorkItem left, WorkItem right)
            => !(left == right);
    }
}
