// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Functions.Durable
{
    /// <summary>
    /// Represents a factory that leverages <see cref="Guid.NewGuid"/> for generating <see cref="Guid"/> values.
    /// </summary>
    public sealed class GuidFactory : IGuidFactory
    {
        private GuidFactory()
        {
        }

        /// <summary>
        /// Gets the default <see cref="IGuidFactory"/> that uses <see cref="Guid.NewGuid"/>.
        /// </summary>
        /// <value>The singleton <see cref="GuidFactory"/> instance.</value>
        public static IGuidFactory Default { get; }

        /// <inheritdoc />
        public Guid Create()
            => Guid.NewGuid();
    }
}
