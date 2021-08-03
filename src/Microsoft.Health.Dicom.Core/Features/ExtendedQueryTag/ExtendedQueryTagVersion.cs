// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// ExtendedQueryTag version.
    /// </summary>
    public struct ExtendedQueryTagVersion : IEquatable<ExtendedQueryTagVersion>, IComparable<ExtendedQueryTagVersion>
    {
        private readonly byte[] _version;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedQueryTagVersion"/> class.
        /// </summary>
        /// <param name="version">The version.</param>        
        public ExtendedQueryTagVersion(byte[] version)
        {
            _version = EnsureArg.IsNotNull(version, nameof(version));
            Debug.Assert(version.Length == 8, "Version length should be 8");
        }

        public int CompareTo(ExtendedQueryTagVersion other)
        {
            Debug.Assert(_version.Length == other._version.Length, "Version should have same length");
            for (int i = 0; i < _version.Length; i++)
            {
                if (_version[i] != other._version[i])
                {
                    return _version[i] - other._version[i];
                }
            }
            return 0;
        }

        public bool Equals(ExtendedQueryTagVersion other)
        {
            return CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return obj is ExtendedQueryTagVersion && Equals((ExtendedQueryTagVersion)obj);
        }

        public override int GetHashCode() => HashCode.Combine(_version);

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

        public byte[] ToByteArray()
        {
            return _version;
        }
    }
}
