// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnsureThat;

namespace Microsoft.Health.Dicom.Api.Features.Converter
{
    /// <summary>
    /// Enum JsonConverter to provide better error message to customer.
    /// </summary>
    /// <typeparam name="T">Enum type.</typeparam>
    public sealed class EnumNameJsonConverter<T> : JsonConverter<T>
        where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            EnsureArg.IsNotNull(typeToConvert, nameof(typeToConvert));

            if (reader.TokenType == JsonTokenType.String)
            {
                string content = reader.GetString();
                if (Enum.TryParse(content, true, out T result))
                {
                    return result;
                }
            }

            throw new JsonException(
                string.Format(CultureInfo.InvariantCulture, DicomApiResource.InvalidEnumValue, string.Join("\",\"", Enum.GetNames(typeToConvert))));
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            EnsureArg.IsNotNull(writer);
            writer.WriteStringValue(Enum.GetName<T>(value));
        }
    }
}
