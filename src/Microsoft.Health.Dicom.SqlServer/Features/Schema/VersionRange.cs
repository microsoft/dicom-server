// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using EnsureThat;

namespace Microsoft.Health.Dicom.SqlServer.Features.Schema
{
    internal readonly struct VersionRange : IEquatable<VersionRange>, IEnumerable<SchemaVersion>
    {
        public SchemaVersion Min { get; }

        public SchemaVersion Max { get; }

        public int Count => Max - Min + 1;

        public VersionRange(SchemaVersion version)
            : this(version, version)
        { }

        public VersionRange(SchemaVersion min, SchemaVersion max)
        {
            EnsureArg.EnumIsDefined(min, nameof(min));
            EnsureArg.EnumIsDefined(max, nameof(max));

            if (min > max)
            {
                throw new ArgumentOutOfRangeException(nameof(min));
            }

            Min = min;
            Max = max;
        }

        public bool Contains(SchemaVersion version)
        {
            EnsureArg.EnumIsDefined(version, nameof(version));
            return Min <= version && version <= Max;
        }

        public void Deconstruct(out SchemaVersion min, out SchemaVersion max)
        {
            min = Min;
            max = Max;
        }

        public override bool Equals(object obj)
            => obj is VersionRange other && Equals(other);

        public bool Equals(VersionRange other)
            => Min == other.Min && Max == other.Max;

        public override int GetHashCode()
            => HashCode.Combine(Min, Max);

        public IEnumerator<SchemaVersion> GetEnumerator()
        {
            for (SchemaVersion current = Min; current <= Max; current++)
            {
                yield return current;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public static bool operator ==(VersionRange left, VersionRange right)
            => left.Equals(right);

        public static bool operator !=(VersionRange left, VersionRange right)
            => !(left == right);

        public static implicit operator VersionRange(SchemaVersion version)
            => new VersionRange(version);
    }
}
