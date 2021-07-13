// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using EnsureThat;

namespace Microsoft.Health.Dicom.SqlServer.Features.Schema
{
    /// <summary>
    /// Represents a range of SQL schema versions.
    /// </summary>
    public readonly struct VersionRange : IEquatable<VersionRange>, IEnumerable<SchemaVersion>
    {
        /// <summary>
        /// Gets the inclusive lowest version in the range.
        /// </summary>
        /// <value>The minimum <see cref="SchemaVersion"/>.</value>
        public SchemaVersion Min { get; }

        /// <summary>
        /// Gets the inclusive highest version in the range.
        /// </summary>
        /// <value>The maximum <see cref="SchemaVersion"/>.</value>
        public SchemaVersion Max { get; }

        /// <summary>
        /// Gets the number of versions contained in the range.
        /// </summary>
        /// <value>The size of the range.</value>
        public int Count => Max - Min + 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionRange"/> structure for a single version.
        /// </summary>
        /// <param name="version">The <see cref="SchemaVersion"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="version"/> is not a defined <see cref="SchemaVersion"/> value
        /// </exception>
        public VersionRange(SchemaVersion version)
            : this(version, version)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionRange"/> structure.
        /// </summary>
        /// <param name="min">The minimum <see cref="SchemaVersion"/>.</param>
        /// <param name="max">The maximum <see cref="SchemaVersion"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="min"/> is not a defined <see cref="SchemaVersion"/> value</para>
        /// <para>-or-</para>
        /// <para><paramref name="max"/> is not a defined <see cref="SchemaVersion"/> value</para>
        /// <para>-or-</para>
        /// <para><paramref name="min"/> is larger than <paramref name="max"/></para>
        /// </exception>
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

        /// <summary>
        /// Indicates whether the version is within the range.
        /// </summary>
        /// <param name="version">A <see cref="SchemaVersion"/> value.</param>
        /// <returns>
        /// <see langword="true"/> if the <paramref name="version"/> is between the <see cref="Min"/>
        /// and <see cref="Max"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="version"/> is not a defined <see cref="SchemaVersion"/> value
        /// </exception>
        public bool Contains(SchemaVersion version)
        {
            EnsureArg.EnumIsDefined(version, nameof(version));
            return Min <= version && version <= Max;
        }

        /// <summary>
        /// Deconstructs the <see cref="VersionRange"/> value into the <see cref="Min"/> and <see cref="Max"/>.
        /// </summary>
        /// <param name="min">When this method returns, contains <see cref="Min"/>. This parameter is passed uninitialized.</param>
        /// <param name="max">When this method returns, contains <see cref="Max"/>. This parameter is passed uninitialized.</param>
        public void Deconstruct(out SchemaVersion min, out SchemaVersion max)
        {
            min = Min;
            max = Max;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="obj"/> is an instance of <see cref="VersionRange"/> and
        /// equals the value of this instance; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Equals(object obj)
            => obj is VersionRange other && Equals(other);

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the value of the specified
        /// <see cref="VersionRange"/> instance.
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns>
        /// <see langword="true"/> if the <paramref name="other"/> parameter equals the value of this instance;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(VersionRange other)
            => Min == other.Min && Max == other.Max;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
            => Min.GetHashCode() ^ Max.GetHashCode();

        /// <summary>
        /// Returns an enumerator that iterates through the included versions.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> that can be used to iterate through the versions.</returns>
        public IEnumerator<SchemaVersion> GetEnumerator()
        {
            for (SchemaVersion current = Min; current <= Max; current++)
            {
                yield return current;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        /// <summary>
        /// Determines whether two specified instances of <see cref="VersionRange"/> are equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> represent range of versions;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator ==(VersionRange left, VersionRange right)
            => left.Equals(right);

        /// <summary>
        /// Determines whether two specified instances of <see cref="VersionRange"/> are not equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> do not represent range of
        /// versions; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator !=(VersionRange left, VersionRange right)
            => !(left == right);

        /// <summary>
        /// Converts <see cref="SchemaVersion"/> value into a <see cref="VersionRange"/> consisting of a single version.
        /// </summary>
        /// <param name="version">A <see cref="SchemaVersion"/> value.</param>
        [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Use ctor overload.")]
        public static implicit operator VersionRange(SchemaVersion version)
            => new VersionRange(version);
    }
}
