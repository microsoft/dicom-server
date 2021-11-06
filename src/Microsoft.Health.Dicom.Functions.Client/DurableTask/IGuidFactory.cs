// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Functions.Client.DurableTask
{
    /// <summary>
    /// Represents a factory for generating unique <see cref="Guid"/> values.
    /// </summary>
    public interface IGuidFactory
    {
        /// <summary>
        /// Creates a unique <see cref="Guid"/> value.
        /// </summary>
        /// <returns>A unique <see cref="Guid"/> value.</returns>
        Guid Create();
    }
}
