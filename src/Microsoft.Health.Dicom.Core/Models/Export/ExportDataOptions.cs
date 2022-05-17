// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;
using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Models.Export;

/// <summary>
/// Represents options for either what data should be exported from the DICOM service,
/// or where that data should be copied.
/// </summary>
public sealed class ExportDataOptions<T>
{
    internal ExportDataOptions(T type, object options)
    {
        Type = type;
        Options = EnsureArg.IsNotNull(options, nameof(options));
    }

    /// <summary>
    /// Gets the type of options this instance represents.
    /// </summary>
    /// <value>A type denoting the kind of options.</value>
    public T Type { get; }

    [JsonProperty]
    [JsonInclude]
    internal object Options { get; }
}
