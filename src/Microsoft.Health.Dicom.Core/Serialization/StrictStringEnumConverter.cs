// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Serialization;

/// <summary>
/// Represents a <see cref="JsonConverterFactory"/> for enumeration types where values are
/// strictly represented by their names as JSON string tokens.
/// </summary>
public sealed class StrictStringEnumConverter : JsonConverterFactory
{
    private readonly JsonNamingPolicy _namingPolicy;

    /// <summary>
    /// Creates a new instance of the <see cref="StrictStringEnumConverter"/>
    /// with the given naming policy.
    /// </summary>
    /// <param name="namingPolicy">An optional JSON naming policy.</param>
    public StrictStringEnumConverter(JsonNamingPolicy namingPolicy = null)
        => _namingPolicy = namingPolicy;

    /// <summary>
    /// Determines whether the JSON converter can operate on the given type.
    /// </summary>
    /// <param name="typeToConvert">The type to serialize and/or deserialize.</param>
    /// <returns>
    /// <see langword="true"/> if the type is compatible with the converter; otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="typeToConvert"/> is <see langword="null"/>.</exception>
    public override bool CanConvert(Type typeToConvert)
        => EnsureArg.IsNotNull(typeToConvert).IsEnum;

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => Activator.CreateInstance(
            typeof(StrictStringEnumConverter<>).MakeGenericType(EnsureArg.IsNotNull(typeToConvert)),
            new object[] { _namingPolicy }) as JsonConverter;
}
