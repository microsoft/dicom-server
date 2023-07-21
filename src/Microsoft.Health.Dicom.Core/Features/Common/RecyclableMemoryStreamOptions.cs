// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Features.Common;

/// <summary>
/// Represents the settings that may be configured for the shared instance of <see cref="RecyclableMemoryStreamManager"/>.
/// </summary>
public class RecyclableMemoryStreamOptions
{
    /// <summary>
    /// The default <see cref="IConfigurationSection"/> name.
    /// </summary>
    public const string DefaultSectionName = "RecyclableMemoryStream";

    /// <inheritdoc cref="RecyclableMemoryStreamManager.AggressiveBufferReturn"/>
    public bool AggressiveBufferReturn { get; set; }

    /// <inheritdoc cref="RecyclableMemoryStreamManager.GenerateCallStacks"/>
    public bool GenerateCallStacks { get; set; }

    /// <inheritdoc cref="RecyclableMemoryStreamManager.MaximumFreeLargePoolBytes"/>
    public long MaximumFreeLargePoolBytes { get; set; } // Unbounded = 0L

    /// <inheritdoc cref="RecyclableMemoryStreamManager.MaximumFreeSmallPoolBytes"/>
    public long MaximumFreeSmallPoolBytes { get; set; } // Unbounded = 0L

    /// <inheritdoc cref="RecyclableMemoryStreamManager.MaximumStreamCapacity"/>
    public long MaximumStreamCapacity { get; set; }

    /// <inheritdoc cref="RecyclableMemoryStreamManager.ThrowExceptionOnToArray"/>
    public bool ThrowExceptionOnToArray { get; set; }
}
