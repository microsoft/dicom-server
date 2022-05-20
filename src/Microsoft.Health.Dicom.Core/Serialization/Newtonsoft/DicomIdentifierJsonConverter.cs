// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.Health.Dicom.Core.Models.Common;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Serialization.Newtonsoft;

internal sealed class DicomIdentifierJsonConverter : JsonConverter<DicomIdentifier>
{
    public override DicomIdentifier ReadJson(JsonReader reader, Type objectType, DicomIdentifier existingValue, bool hasExistingValue, JsonSerializer serializer)
        => reader.TokenType == JsonToken.String
            ? DicomIdentifier.Parse(reader.Value as string)
            : throw new JsonException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    DicomCoreResource.UnexpectedJsonToken,
                    JsonToken.String,
                    reader.TokenType));

    public override void WriteJson(JsonWriter writer, DicomIdentifier value, JsonSerializer serializer)
        => writer.WriteValue(value.ToString());
}
