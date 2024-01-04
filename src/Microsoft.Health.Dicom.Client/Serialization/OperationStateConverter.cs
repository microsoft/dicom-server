// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Client.Serialization;

internal sealed class OperationStateConverter : JsonConverter<IOperationState<DicomOperation>>
{
    public override IOperationState<DicomOperation> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        JsonObject obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options);

        if (!obj.TryGetPropertyValue(nameof(IOperationState<DicomOperation>.Type), out JsonNode value))
            throw new JsonException();

        return value.Deserialize<DicomOperation>(options) switch
        {
            DicomOperation.Export => obj.Deserialize<OperationState<DicomOperation, ExportResults>>(options),
            DicomOperation.Update => obj.Deserialize<OperationState<DicomOperation, UpdateResults>>(options),
            _ => obj.Deserialize<OperationState<DicomOperation>>(options),
        };
    }

    public override void Write(Utf8JsonWriter writer, IOperationState<DicomOperation> value, JsonSerializerOptions options)
        => throw new NotSupportedException(
            string.Format(CultureInfo.CurrentCulture, DicomClientResource.JsonWriteNotSupported, nameof(DicomIdentifier)));
}
