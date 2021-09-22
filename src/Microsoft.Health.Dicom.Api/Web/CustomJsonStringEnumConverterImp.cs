// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnsureThat;

namespace Microsoft.Health.Dicom.Api.Web
{
    internal class CustomJsonStringEnumConverterImp<T> : JsonConverter<T>
        where T : Enum

    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            EnsureArg.IsNotNull(typeToConvert, nameof(typeToConvert));

            if (reader.TokenType == JsonTokenType.String)
            {
                string content = reader.GetString();
                foreach (var item in Enum.GetValues(typeToConvert))
                {
                    if (item.ToString().Equals(content, StringComparison.OrdinalIgnoreCase))
                    {
                        return (T)item;
                    }
                }
            }

            throw new JsonException(
                string.Format(CultureInfo.InvariantCulture, DicomApiResource.InvalidEnumValue, string.Join("\",\"", Enum.GetNames(typeToConvert))));
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            EnsureArg.IsNotNull(writer);
            writer.WriteStringValue(value.ToString());
        }
    }
}
