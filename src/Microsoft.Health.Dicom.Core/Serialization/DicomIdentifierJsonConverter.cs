// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Models.Common;

namespace Microsoft.Health.Dicom.Core.Serialization;

internal sealed class DicomIdentifierJsonConverter : JsonConverter<DicomIdentifier>
{
    public override DicomIdentifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DicomIdentifier.Parse(reader.GetString());

    public override void Write(Utf8JsonWriter writer, DicomIdentifier value, JsonSerializerOptions options)
        => EnsureArg.IsNotNull(writer, nameof(writer)).WriteStringValue(value.ToString());
}
