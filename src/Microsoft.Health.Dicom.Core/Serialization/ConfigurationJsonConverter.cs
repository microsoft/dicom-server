// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Health.Dicom.Core.Serialization;

internal sealed class ConfigurationJsonConverter : JsonConverter<IConfiguration>
{
    public override IConfiguration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(EnumeratePairs(JsonSerializer.Deserialize<JsonElement>(ref reader, options)))
            .Build();
    }

    public override void Write(Utf8JsonWriter writer, IConfiguration value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (IConfigurationSection section in value.GetChildren())
        {
            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(section.Key) ?? section.Key);

            if (section.Value == null)
                Write(writer, section, options);
            else
                writer.WriteStringValue(section.Value);
        }

        writer.WriteEndObject();
    }

    private static IEnumerable<KeyValuePair<string, string>> EnumeratePairs(JsonElement element)
        => EnumeratePairs(string.Empty, element);

    private static IEnumerable<KeyValuePair<string, string>> EnumeratePairs(string path, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (KeyValuePair<string, string> p in element.EnumerateObject().SelectMany(x => EnumeratePairs(GetPath(path, x.Name), x.Value)))
                {
                    yield return p;
                }
                break;
            case JsonValueKind.Array:
                foreach (KeyValuePair<string, string> p in element.EnumerateArray().SelectMany((x, i) => EnumeratePairs(GetPath(path, i.ToString(CultureInfo.InvariantCulture)), x)))
                {
                    yield return p;
                }
                break;
            case JsonValueKind.String:
                yield return KeyValuePair.Create(path, element.GetString());
                break;
            case JsonValueKind.False:
            case JsonValueKind.True:
                yield return KeyValuePair.Create(path, element.GetBoolean().ToString());
                break;
            case JsonValueKind.Number:
                yield return KeyValuePair.Create(path, element.GetRawText());
                break;
            case JsonValueKind.Null:
                break;
            default:
                throw new JsonException(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.InvalidJsonToken, element.ValueKind));
        }
    }

    private static string GetPath(string current, string key)
        => string.IsNullOrEmpty(current) ? key : current + ':' + key;
}
