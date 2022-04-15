// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Health.Dicom.Core.Serialization.Newtonsoft;

internal sealed class ConfigurationJsonConverter : JsonConverter<IConfiguration>
{
    private readonly NamingStrategy _namingStrategy;

    public ConfigurationJsonConverter(NamingStrategy namingStrategy)
        => _namingStrategy = EnsureArg.IsNotNull(namingStrategy, nameof(namingStrategy));

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
        => WriteConfiguration(writer, value);

    private static IEnumerable<KeyValuePair<string, string>> EnumeratePairs(JObject config)
        => EnumeratePairs(string.Empty, config);

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Boolean types in JSON are lowercase.")]
    private static IEnumerable<KeyValuePair<string, string>> EnumeratePairs(string path, JToken token)
    {
        if (token is JObject section)
        {
            foreach (KeyValuePair<string, string> p in section.Properties().SelectMany(x => EnumeratePairs(GetPath(path, x.Name), x.Value)))
            {
                yield return p;
            }
        }
        else if (token is JArray a)
        {
            foreach (KeyValuePair<string, string> p in a.Children().SelectMany((x, i) => EnumeratePairs(GetPath(path, i.ToString(CultureInfo.InvariantCulture)), x)))
            {
                yield return p;
            }
        }
        else if (token is JValue value)
        {
            if (value.Type != JTokenType.Null)
            {
                // Newtonsoft by default parses the values and does not provide a means of retrieving the raw string values.
                // What this ultimately means is that the representation may look different if read as a string from the
                // configuration or if re-written to JSON (E.g. DateTimes will use the "O" format).
                // We could circumvent this by parsing this with JRaw types or manually enumerating the JSON.
                yield return token.Type switch
                {
                    JTokenType.Boolean => KeyValuePair.Create(path, token.Value<bool>().ToString().ToLowerInvariant()), // bool type returns "True"/"False" for ToString()
                    JTokenType.Date => KeyValuePair.Create(path, token.Value<DateTime>().ToString("O", CultureInfo.InvariantCulture)),
                    JTokenType.Float => KeyValuePair.Create(path, token.Value<float>().ToString(CultureInfo.InvariantCulture)),
                    JTokenType.Guid => KeyValuePair.Create(path, token.Value<Guid>().ToString("D", CultureInfo.InvariantCulture)),
                    JTokenType.Integer => KeyValuePair.Create(path, token.Value<int>().ToString(CultureInfo.InvariantCulture)),
                    JTokenType.String => KeyValuePair.Create(path, token.Value<string>()),
                    JTokenType.TimeSpan => KeyValuePair.Create(path, token.Value<TimeSpan>().ToString("c", CultureInfo.InvariantCulture)),
                    JTokenType.Uri => KeyValuePair.Create(path, token.Value<Uri>().ToString()), // URL may be relative. TODO: Should we try to use AbsoluteUri when possible?
                    _ => throw new JsonException(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.InvalidJsonToken, token.Type))
                };
            }
        }
        else
        {
            throw new JsonException(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.InvalidJsonToken, token.Type));
        }
    }

    private void WriteConfiguration(JsonWriter writer, IConfiguration config)
    {
        writer.WriteStartObject();

        foreach (IConfigurationSection section in config.GetChildren())
        {
            writer.WritePropertyName(_namingStrategy.GetPropertyName(section.Key, false));

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
