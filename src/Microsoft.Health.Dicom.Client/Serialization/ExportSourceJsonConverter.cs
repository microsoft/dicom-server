// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Client.Models.Export;

namespace Microsoft.Health.Dicom.Client.Serialization;

internal sealed class ExportSourceJsonConverter : JsonConverter<ExportSource>
{
    public override ExportSource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException(
            string.Format(CultureInfo.CurrentCulture, DicomClientResource.JsonReadNotSupported, nameof(ExportSource)));

    public override void Write(Utf8JsonWriter writer, ExportSource value, JsonSerializerOptions options)
    {
        Type configType = value.Type switch
        {
            ExportSourceType.Identifiers => typeof(IdentifierExportOptions),
            _ => throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, DicomClientResource.InvalidExportSource, value.Type)),
        };

        writer.WriteStartObject();

        writer.WritePropertyName(GetPropertyName(nameof(ExportSource.Type), options.PropertyNamingPolicy));
        JsonSerializer.Serialize(writer, value.Type, options);

        writer.WritePropertyName(GetPropertyName(nameof(ExportSource.Configuration), options.PropertyNamingPolicy));
        JsonSerializer.Serialize(writer, value, configType, options);

        writer.WriteEndObject();
    }

    private static string GetPropertyName(string name, JsonNamingPolicy policy)
        => policy?.ConvertName(name) ?? name;
}
