// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Features.Model;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Serialization.Newtonsoft;

internal class PartitionedDicomIdentifierJsonConverter : JsonConverter<PartitionedDicomIdentifier>
{
    public override PartitionedDicomIdentifier ReadJson(JsonReader reader, Type objectType, PartitionedDicomIdentifier existingValue, bool hasExistingValue, JsonSerializer serializer)
        => reader.TokenType == JsonToken.Null ? null : PartitionedDicomIdentifier.Parse(serializer.Deserialize<string>(reader));

    public override void WriteJson(JsonWriter writer, PartitionedDicomIdentifier value, JsonSerializer serializer)
        => serializer.Serialize(writer, value.ToString());
}
