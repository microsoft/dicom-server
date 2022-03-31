// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Features.Model;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Serialization.Newtonsoft;

internal class DicomIdentifierJsonConverter : JsonConverter<DicomIdentifier>
{
    public override DicomIdentifier ReadJson(JsonReader reader, Type objectType, DicomIdentifier existingValue, bool hasExistingValue, JsonSerializer serializer)
        => DicomIdentifier.Parse(serializer.Deserialize<string>(reader));

    public override void WriteJson(JsonWriter writer, DicomIdentifier value, JsonSerializer serializer)
        => serializer.Serialize(writer, value.ToString());
}
