// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Microsoft.Health.Dicom.Core.Models;

/// <summary>
/// Represents a configuration associated with a <see cref="Type"/> that indicates what settings should be present.
/// </summary>
/// <typeparam name="T">The type of the configuration.</typeparam>
public sealed class TypedConfiguration<T>
{
    /// <summary>
    /// Gets or sets the kind of configuration this object contains.
    /// </summary>
    /// <value>A value indicating the type of configuration.</value>
    public T Type { get; set; }

    /// <summary>
    /// Gets or sets the type-specific configuration.
    /// </summary>
    /// <value>The type-specific configuration.</value>
    public IConfiguration Configuration { get; set; }
}
