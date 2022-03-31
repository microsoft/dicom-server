// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Serialization;

internal class DicomIdentifierJsonConverter : JsonConverter<DicomIdentifier>
{
    public override DicomIdentifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                string.Format(CultureInfo.CurrentCulture, DicomCoreResource.UnexpectedJsonToken, JsonTokenType.String, reader.TokenType));
        }

        try
        {
            return DicomIdentifier.Parse(reader.GetString());
        }
        catch (FormatException)
        {
            throw new JsonException();
        }
    }

    public override void Write(Utf8JsonWriter writer, DicomIdentifier value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
