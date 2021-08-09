// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// ExtendedQueryTag version.
    /// </summary>
    /// <remarks>
    /// We use SQL ROWVERSION to track changes on extended query tags. Every time the tag is changed, a new version is assigned to.
    /// We use max version amoung all extended query tags as version of ExtendedQueryTagTable, since ROWVERSION keep increasing and never falls back.
    /// The version is used to indicate if extended query tags we are using are same as ExtendedQueryTagTable.
    /// </remarks>
    public readonly struct ExtendedQueryTagVersion : IEquatable<ExtendedQueryTagVersion>, IComparable<ExtendedQueryTagVersion>
    {
        /// <summary>
        /// Get extended query tag version for a list of tag versions.
        /// </summary>
        /// <param name="tagVersions">The tag version collection.</param>
        /// <returns>The tag version.</returns>
        public static ExtendedQueryTagVersion? GetExtendedQueryTagVersion(IReadOnlyCollection<ExtendedQueryTagVersion> tagVersions)
        {
            EnsureArg.IsNotNull(tagVersions, nameof(tagVersions));
            return tagVersions.Count == 0 ? null : tagVersions.Max();
        }

        /// <summary>
        /// Get extended query tag version for a list of query tags.
        /// </summary>
        /// <param name="queryTags">The query tags.</param>
        /// <returns>The tag version.</returns>>
        public static ExtendedQueryTagVersion? GetExtendedQueryTagVersion(IReadOnlyCollection<QueryTag> queryTags)
        {
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));
            return GetExtendedQueryTagVersion(queryTags
                .Where(x => x.IsExtendedQueryTag && x.ExtendedQueryTagStoreEntry.Version.HasValue)
                .Select(x => x.ExtendedQueryTagStoreEntry.Version.Value)
                .ToList());
        }

        private readonly ulong _version;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedQueryTagVersion"/> class.
        /// </summary>
        /// <param name="version">The version.</param>        
        public ExtendedQueryTagVersion(byte[] version)
        {
            EnsureArg.IsNotNull(version, nameof(version));
            Debug.Assert(version.Length == 8, "Version length should be 8");
            _version = BinaryPrimitives.ReadUInt64BigEndian(version);
        }

        public int CompareTo(ExtendedQueryTagVersion other)
        {
            return _version.CompareTo(other._version);
        }

        public bool Equals(ExtendedQueryTagVersion other)
        {
            return CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return obj is ExtendedQueryTagVersion version && Equals(version);
        }

        public override int GetHashCode() => HashCode.Combine(_version);
        public byte[] ToByteArray()
        {
            byte[] result = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(result, _version);
            return result;
        }

        public static bool operator ==(ExtendedQueryTagVersion left, ExtendedQueryTagVersion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ExtendedQueryTagVersion left, ExtendedQueryTagVersion right)
        {
            return !(left == right);
        }

        public static bool operator <(ExtendedQueryTagVersion left, ExtendedQueryTagVersion right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(ExtendedQueryTagVersion left, ExtendedQueryTagVersion right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(ExtendedQueryTagVersion left, ExtendedQueryTagVersion right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(ExtendedQueryTagVersion left, ExtendedQueryTagVersion right)
        {
            return left.CompareTo(right) >= 0;
        }

    }
}
