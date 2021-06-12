// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Functions.Client.Serialization
{
    internal class OperationTypeConverter : JsonConverter<OperationType>
    {
        public override bool CanWrite => false;

        public override OperationType ReadJson(JsonReader reader, Type objectType, OperationType existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType is not (JsonToken.String or JsonToken.Null))
            {
                throw new JsonReaderException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomFunctionsClientResource.UnexpectedJsonToken,
                        JsonToken.String,
                        reader.TokenType));
            }

            // TODO: Support Nullable<OperationType> is necessary
            return string.Equals(reader.Value as string, "Reindex", StringComparison.OrdinalIgnoreCase)
                ? OperationType.Reindex
                : OperationType.Unknown;
        }

        public override void WriteJson(JsonWriter writer, OperationType value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
