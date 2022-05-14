// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Models;

/// <summary>
/// Represents the key for a secret.
/// </summary>
public sealed class SecretKey
{
    /// <summary>
    /// Gets the name of a secret.
    /// </summary>
    /// <value>The name of a secret.</value>
    public string Name { get; set; }

    /// <summary>
    /// Gets the version of a secret.
    /// </summary>
    /// <remarks>
    /// The version only makes sense within the context of a name.
    /// </remarks>
    /// <value>The version of a secret associated with a name.</value>
    public string Version { get; set; }
}
