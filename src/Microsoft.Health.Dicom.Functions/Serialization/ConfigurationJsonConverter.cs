// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Functions.Serialization;

internal class ConfigurationJsonConverter : JsonConverter<IConfiguration>
{
    public override IConfiguration ReadJson(JsonReader reader, Type objectType, IConfiguration existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var builder = new ConfigurationBuilder();

        if (reader.TokenType == JsonToken.Null)
            return hasExistingValue ? existingValue : null;

        if (hasExistingValue)
            builder.AddConfiguration(existingValue);

        return builder
            .AddInMemoryCollection(EnumeratePairs(serializer.Deserialize<JObject>(reader)))
            .Build();
    }

    public override void WriteJson(JsonWriter writer, IConfiguration value, JsonSerializer serializer)
    {
        if (value == null)
            writer.WriteNull();

        WriteConfiguration(writer, value);
    }

    private static IEnumerable<KeyValuePair<string, string>> EnumeratePairs(JObject config)
        => EnumeratePairs("", config);

    private static IEnumerable<KeyValuePair<string, string>> EnumeratePairs(string path, JToken token)
    {
        if (token is JObject section)
        {
            foreach (KeyValuePair<string, string> pair in section.SelectMany((KeyValuePair<string, JToken> x) => EnumeratePairs(path + ":" + x.Key, x.Value)))
            {
                yield return pair;
            }
        }
        else if (token.Type == JTokenType.String)
        {
            yield return KeyValuePair.Create(path, token.Value<string>());
        }
        else if (token is JArray a)
        {
            for (int i = 0; i < a.Count; i++)
            {
                JToken e = a[i];
                if (e.Type != JTokenType.String)
                    throw new JsonException();

                yield return KeyValuePair.Create(path + ":" + i.ToString(CultureInfo.InvariantCulture), e.Value<string>());
            }
        }
        else if (token.Type == JTokenType.Null)
        {
            yield break;
        }
        else
        {
            throw new JsonException();
        }
    }

    private static void WriteConfiguration(JsonWriter writer, IConfiguration config)
    {
        writer.WriteStartObject();

        foreach (IConfigurationSection section in config.GetChildren())
        {
            writer.WritePropertyName(section.Key);

            if (section.Value == null)
                WriteConfiguration(writer, section);
            else
                writer.WriteValue(section.Value);
        }

        writer.WriteEndObject();
    }
}
