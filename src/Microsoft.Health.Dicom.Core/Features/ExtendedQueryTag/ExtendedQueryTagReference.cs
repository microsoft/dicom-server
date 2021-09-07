// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    // TODO: Use immutable type once we remove .NET Core 3.1 dependency

    /// <summary>
    /// Represents a reference to an extended query tag in the store.
    /// </summary>
    public class ExtendedQueryTagReference : IEquatable<ExtendedQueryTagReference>
    {
        /// <summary>
        /// Gets or sets the extended extended query tag key.
        /// </summary>
        public int Key { get; set; }

        /// <summary>
        /// Gets or sets the path for the extended query tag.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Returns a value indicating whether the <paramref name="obj"/> is equal to this <see cref="ExtendedQueryTagReference"/>.
        /// </summary>
        /// <param name="obj">An object to compare.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="obj"/> is equal to this instance; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Equals(object obj)
            => obj is ExtendedQueryTagReference other && Equals(other);

        /// <summary>
        /// Returns a value indicating whether the <paramref name="other"/> structure is equal
        /// to this <see cref="ExtendedQueryTagReference"/>.
        /// </summary>
        /// <param name="other">The other instance to compare.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="other"/> is equal to this instance; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(ExtendedQueryTagReference other)
            => other is not null && Key == other.Key && Path == other.Path;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
            => HashCode.Combine(Key, Path);
    }
}
