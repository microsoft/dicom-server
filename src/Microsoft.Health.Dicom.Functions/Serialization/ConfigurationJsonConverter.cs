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

        if (hasExistingValue)
            builder.AddConfiguration(existingValue);

        return builder
            .AddInMemoryCollection(EnumeratePairs(serializer.Deserialize<JObject>(reader)))
            .Build();
    }

    public override void WriteJson(JsonWriter writer, IConfiguration value, JsonSerializer serializer)
        => WriteConfiguration(writer, value);

    private static IEnumerable<KeyValuePair<string, string>> EnumeratePairs(JObject config)
        => EnumeratePairs(string.Empty, config);

    private static IEnumerable<KeyValuePair<string, string>> EnumeratePairs(string path, JToken token)
    {
        if (token is JProperty prop)
        {
            foreach (KeyValuePair<string, string> p in EnumeratePairs(GetPath(path, prop.Name), prop.Value))
            {
                yield return p;
            }
        }
        else if (token is JObject section)
        {
            foreach (KeyValuePair<string, string> p in section.SelectMany((KeyValuePair<string, JToken> x) => EnumeratePairs(GetPath(path, x.Key), x.Value)))
            {
                yield return p;
            }
        }
        else if (TryGetPair(path, token, out KeyValuePair<string, string> pair))
        {
            yield return KeyValuePair.Create(path, token.Value<string>());
        }
        else if (token is JArray a)
        {
            for (int i = 0; i < a.Count; i++)
            {
                foreach (KeyValuePair<string, string> p in EnumeratePairs(GetPath(path, i.ToString(CultureInfo.InvariantCulture)), a[i]))
                {
                    yield return pair;
                }
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

    private static bool TryGetPair(string path, JToken token, out KeyValuePair<string, string> pair)
    {
        switch (token.Type)
        {
            case JTokenType.Boolean:
            case JTokenType.Date:
            case JTokenType.Float:
            case JTokenType.Guid:
            case JTokenType.Integer:
            case JTokenType.String:
            case JTokenType.TimeSpan:
            case JTokenType.Uri:
                pair = KeyValuePair.Create(path, token.ToString());
                return true;
            default:
                pair = default;
                return false;
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

    private static string GetPath(string current, string key)
        => string.IsNullOrEmpty(current) ? key : current + ':' + key;
}
