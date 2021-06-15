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
    internal class OperationRuntimeStatusConverter : JsonConverter<OperationRuntimeStatus>
    {
        public override bool CanWrite => false;

        public override OperationRuntimeStatus ReadJson(JsonReader reader, Type objectType, OperationRuntimeStatus existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // TODO: Support Nullable<OperationStatus> is necessary
            if (reader.TokenType is not (JsonToken.String or JsonToken.Null))
            {
                throw new JsonReaderException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomFunctionsClientResource.UnexpectedJsonToken,
                        JsonToken.String,
                        reader.TokenType));
            }

            string value = reader.Value as string;
            if (value is null)
            {
                return OperationRuntimeStatus.Unknown;
            }
            else if (value.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                return OperationRuntimeStatus.Pending;
            }
            else if (value.Equals("Running", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("ContinuedAsNew", StringComparison.OrdinalIgnoreCase))
            {
                return OperationRuntimeStatus.Running;
            }
            else if (value.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                return OperationRuntimeStatus.Completed;
            }
            else if (value.Equals("Failed", StringComparison.OrdinalIgnoreCase))
            {
                return OperationRuntimeStatus.Failed;
            }
            else if (value.Equals("Canceled", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Terminated", StringComparison.OrdinalIgnoreCase))
            {
                return OperationRuntimeStatus.Canceled;
            }
            else
            {
                return OperationRuntimeStatus.Unknown;
            }
        }

        public override void WriteJson(JsonWriter writer, OperationRuntimeStatus value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
