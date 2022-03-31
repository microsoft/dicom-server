// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models.Export;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Core.Serialization.Newtonsoft;

internal sealed class SourceManifestJsonConverter : JsonConverter<SourceManifest>
{
    public override SourceManifest ReadJson(JsonReader reader, Type objectType, SourceManifest existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JObject obj = serializer.Deserialize<JObject>(reader);

        if (!TryGetProperty(obj, nameof(SourceManifest.Type), JTokenType.String, out JValue typeToken))
            throw new JsonException();

        if (typeToken.Value<string>().Equals(nameof(ExportSourceType.Identifiers), StringComparison.OrdinalIgnoreCase))
        {
            if (!TryGetProperty(obj, nameof(SourceManifest.Input), JTokenType.Array, out JArray idsToken))
                throw new JsonException();


            return new SourceManifest
            {
                Input = idsToken.Values().Select(x => x.ToObject<DicomIdentifier>(serializer)).ToArray(),
                Type = ExportSourceType.Identifiers,
            };
        }
        else
        {
            throw new JsonException();
        }
    }

    public override void WriteJson(JsonWriter writer, SourceManifest value, JsonSerializer serializer)
    {
        if (value.Type != ExportSourceType.Identifiers)
            throw new JsonException();

        writer.WriteStartObject();

        writer.WritePropertyName(nameof(SourceManifest.Type));
        serializer.Serialize(writer, value.Type);

        writer.WritePropertyName(nameof(SourceManifest.Input));
        serializer.Serialize(writer, value.Input);

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
