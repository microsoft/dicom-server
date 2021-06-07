// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Models.Operations.Serialization
{
    internal class OperationTypeConverter : JsonConverter<OperationType>
    {
        public override bool CanWrite => false;

        public override OperationType ReadJson(JsonReader reader, Type objectType, OperationType existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // TODO: Support Nullable<OperationType> is necessary
            string value = reader.ReadAsString();
            return string.Equals(value, "Reindex", StringComparison.OrdinalIgnoreCase)
                ? OperationType.Reindex
                : OperationType.Unknown;
        }

        public override void WriteJson(JsonWriter writer, OperationType value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
