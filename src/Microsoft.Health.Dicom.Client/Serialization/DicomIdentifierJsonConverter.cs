// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnsureThat;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.Dicom.Client.Serialization;

internal sealed class DicomIdentifierJsonConverter : JsonConverter<DicomIdentifier>
{
    public override DicomIdentifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException(
            string.Format(CultureInfo.CurrentCulture, DicomClientResource.JsonReadNotSupported, nameof(DicomIdentifier)));

    public override void Write(Utf8JsonWriter writer, DicomIdentifier value, JsonSerializerOptions options)
        => EnsureArg.IsNotNull(writer, nameof(writer)).WriteStringValue(value.ToString());
}
