// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Health.Dicom.Core.Serialization;

// Note: This class ignore the JsonNamingPolicy as it is not currently in-use for DICOM.
internal sealed class StrictStringEnumConverter<T> : JsonConverter<T>
    where T : struct, Enum
{
    private readonly JsonNamingPolicy _namingPolicy;

    private static readonly ImmutableDictionary<string, T> Values = ImmutableDictionary.CreateRange(
        StringComparer.OrdinalIgnoreCase,
        Enum.GetValues<T>().Select(x => KeyValuePair.Create(Enum.GetName(x), x)));

    /// <summary>
    /// Creates a new instance of the <see cref="StrictStringEnumConverter{T}"/>
    /// with the given naming policy.
    /// </summary>
    /// <param name="namingPolicy">An optional JSON naming policy.</param>
    public StrictStringEnumConverter(JsonNamingPolicy namingPolicy = null)
        => _namingPolicy = namingPolicy;

    public override bool CanConvert(Type typeToConvert)
        => typeToConvert == typeof(T);

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                string.Format(CultureInfo.CurrentCulture, DicomCoreResource.UnexpectedJsonToken, JsonTokenType.String, reader.TokenType));
        }

        string name = reader.GetString();
        if (!Values.TryGetValue(name, out T value))
        {
            throw new JsonException(
                string.Format(CultureInfo.CurrentCulture, DicomCoreResource.UnexpectedValue, name, GetOrderedNames()));
        }

        return value;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (!Enum.IsDefined(value))
        {
            throw new JsonException(
                string.Format(CultureInfo.CurrentCulture, DicomCoreResource.UnexpectedValue, value, GetOrderedNames()));
        }

        writer.WriteStringValue(ConvertName(Enum.GetName(value)));
    }

    private string ConvertName(string name)
        => _namingPolicy != null ? _namingPolicy.ConvertName(name) : name;

    private static string GetOrderedNames()
        => string.Join(", ", Values.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Select(x => $"'{x}'"));
}
