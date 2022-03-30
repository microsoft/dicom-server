// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Health.Dicom.Core.Models.Export;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Functions.Serialization;

internal sealed class DataSourceJsonConverter : JsonConverter<DataSource>
{
    public override DataSource ReadJson(JsonReader reader, Type objectType, DataSource existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject obj = serializer.Deserialize<JObject>(reader);

        if (!TryGetProperty(obj, nameof(DataSource.Type), JTokenType.String, out JValue typeToken))
            throw new JsonException();

        if (typeToken.Value<string>().Equals(nameof(ExportSourceType.UID), StringComparison.OrdinalIgnoreCase))
        {
            if (!TryGetProperty(obj, nameof(DataSource.Metadata), JTokenType.Array, out JArray idsToken))
                throw new JsonException();

            return new DataSource
            {
                Metadata = idsToken.Values().Select(x => x.Value<string>()).ToArray(),
                Type = ExportSourceType.UID,
            };
        }
        else
        {
            throw new JsonException();
        }
    }

    public override void WriteJson(JsonWriter writer, DataSource value, JsonSerializer serializer)
    {
        if (value.Type != ExportSourceType.UID)
            throw new JsonException();

        writer.WriteStartObject();

        writer.WritePropertyName(nameof(DataSource.Type));
        serializer.Serialize(writer, value.Type);

        writer.WritePropertyName(nameof(DataSource.Metadata));
        serializer.Serialize(writer, value.Metadata);

        writer.WriteEndObject();
    }

    private static bool TryGetProperty<T>(JObject obj, string name, JTokenType type, out T token)
        where T : JToken
    {
        if (obj.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out JToken rawToken) && rawToken is T t && t.Type == type)
        {
            token = t;
            return true;
        }

        token = default;
        return false;
    }
}
