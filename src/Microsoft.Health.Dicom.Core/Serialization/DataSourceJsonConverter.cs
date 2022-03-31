// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Serialization;

public class DataSourceJsonConverter : JsonConverter<SourceManifest>
{
    public override SourceManifest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // TODO: Validation on each step
        // TODO: validate first token is StartObject
        SourceManifest result = new SourceManifest();

        // Property Name
        reader.Read();
        // Type
        string propertyName = reader.GetString();
        if (propertyName != nameof(SourceManifest.Type))
        {
            throw new JsonException("Invalid JsonFormat");
        }

        reader.Read();
        string propertyValue = reader.GetString();
        ExportSourceType sourceType;
        if (!Enum.TryParse(propertyValue, out sourceType))
        {
            throw new JsonException($"Invalid ExportSourceType {sourceType}");
        }
        result.Type = sourceType;

        // Metadata
        reader.Read();
        propertyName = reader.GetString();
        if (propertyName != nameof(SourceManifest.Input))
        {
            throw new JsonException("Invalid JsonFormat");
        }

        reader.Read();
        // Metadata
        if (sourceType == ExportSourceType.Identifiers)
        {
            // metadata
            result.Input = (UidsSource)JsonSerializer.Deserialize(ref reader, typeof(UidsSource), options);
        }

        // end object
        reader.Read();

        return result;
    }

    public override void Write(Utf8JsonWriter writer, SourceManifest value, JsonSerializerOptions options)
    {
        EnsureArg.IsNotNull(writer, nameof(writer));
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        // write type
        writer.WriteString(nameof(SourceManifest.Type), value.Type.ToString());
        // write metadata

        if (value.Type == ExportSourceType.Identifiers)
        {
            writer.WritePropertyName(nameof(SourceManifest.Input));
            JsonSerializer.Serialize(writer, (UidsSource)value.Input, options);
        }
        else
        {
            // TODO: localize string
            throw new JsonException($"Invalid ExportSourceType: {value.Type}");
        }

        writer.WriteEndObject();
    }
}
