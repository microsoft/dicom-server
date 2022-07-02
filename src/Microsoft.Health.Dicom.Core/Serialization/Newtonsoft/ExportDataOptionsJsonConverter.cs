// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Models.Export;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Health.Dicom.Core.Serialization.Newtonsoft;

internal sealed class ExportSourceOptionsJsonConverter : ExportDataOptionsJsonConverter<ExportSourceType>
{
    public ExportSourceOptionsJsonConverter(NamingStrategy namingStrategy)
        : base(MapSourceType, namingStrategy)
    { }

    private static Type MapSourceType(ExportSourceType type)
        => type switch
        {
            ExportSourceType.Identifiers => typeof(IdentifierExportOptions),
            _ => throw new JsonException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    DicomCoreResource.UnexpectedValue,
                    nameof(ExportDataOptions<ExportSourceType>.Type),
                    nameof(ExportSourceType.Identifiers))),
        };
}

internal sealed class ExportDestinationOptionsJsonConverter : ExportDataOptionsJsonConverter<ExportDestinationType>
{
    public ExportDestinationOptionsJsonConverter(NamingStrategy namingStrategy)
        : base(MapDestinationType, namingStrategy)
    { }

    private static Type MapDestinationType(ExportDestinationType type)
        => type switch
        {
            ExportDestinationType.AzureBlob => typeof(AzureBlobExportOptions),
            _ => throw new JsonException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    DicomCoreResource.UnexpectedValue,
                    nameof(ExportDataOptions<ExportDestinationType>.Type),
                    nameof(ExportDestinationType.AzureBlob))),
        };
}

internal abstract class ExportDataOptionsJsonConverter<T> : JsonConverter<ExportDataOptions<T>>
{
    private readonly Func<T, Type> _getType;
    private readonly NamingStrategy _namingStrategy;

    protected ExportDataOptionsJsonConverter(Func<T, Type> getType, NamingStrategy namingStrategy)
    {
        _getType = EnsureArg.IsNotNull(getType, nameof(getType));
        _namingStrategy = EnsureArg.IsNotNull(namingStrategy, nameof(namingStrategy));
    }

    public override ExportDataOptions<T> ReadJson(JsonReader reader, Type objectType, ExportDataOptions<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        ExportDataOptions intermediate = serializer.Deserialize<ExportDataOptions>(reader);

        Type type = _getType(intermediate.Type);
        return new ExportDataOptions<T>(intermediate.Type, intermediate.Settings.ToObject(type, serializer));
    }

    public override void WriteJson(JsonWriter writer, ExportDataOptions<T> value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();

        writer.WritePropertyName(_namingStrategy.GetPropertyName(nameof(ExportDataOptions<T>.Type), false));
        serializer.Serialize(writer, value.Type);

        writer.WritePropertyName(_namingStrategy.GetPropertyName(nameof(ExportDataOptions<T>.Settings), false));
        serializer.Serialize(writer, value.Settings);

        writer.WriteEndObject();
    }

    private sealed class ExportDataOptions
    {
        public T Type { get; set; }

        public JObject Settings { get; set; }
    }
}
