// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnsureThat;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.Dicom.Client.Serialization;

internal sealed class ExportDataOptionsJsonConverter<T> : JsonConverter<ExportDataOptions<T>>
{
    private readonly Func<T, Type> _getType;

    public ExportDataOptionsJsonConverter(Func<T, Type> getType)
        => _getType = EnsureArg.IsNotNull(getType, nameof(getType));

    public override ExportDataOptions<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        ExportDataOptions intermediate = JsonSerializer.Deserialize<ExportDataOptions>(ref reader, options);

        Type type = _getType(intermediate.Type);
        return new ExportDataOptions<T>(intermediate.Type, intermediate.Settings.Deserialize(type, options));
    }

    public override void Write(Utf8JsonWriter writer, ExportDataOptions<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(GetPropertyName(nameof(ExportDataOptions<T>.Type), options.PropertyNamingPolicy));
        JsonSerializer.Serialize(writer, value.Type, options);

        writer.WritePropertyName(GetPropertyName(nameof(ExportDataOptions<T>.Settings), options.PropertyNamingPolicy));
        JsonSerializer.Serialize(writer, value.Settings, _getType(value.Type), options);

        writer.WriteEndObject();
    }

    private static string GetPropertyName(string name, JsonNamingPolicy policy)
        => policy != null ? policy.ConvertName(name) : name;

    [SuppressMessage("Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "This class is deserialized.")]
    private sealed class ExportDataOptions
    {
        public T Type { get; set; }

        public JsonElement Settings { get; set; }
    }
}
