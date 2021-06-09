// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Models.Operations.Serialization
{
    internal class OperationStatusConverter : JsonConverter<OperationStatus>
    {
        public override bool CanWrite => false;

        public override OperationStatus ReadJson(JsonReader reader, Type objectType, OperationStatus existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // TODO: Support Nullable<OperationStatus> is necessary
            if (reader.TokenType is not (JsonToken.String or JsonToken.Null))
            {
                throw new JsonReaderException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.UnexpectedJsonToken,
                        JsonToken.String,
                        reader.TokenType));
            }

            string value = reader.Value as string;
            if (value is null)
            {
                return OperationStatus.Unknown;
            }
            else if (value.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                return OperationStatus.Pending;
            }
            else if (value.Equals("Running", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("ContinuedAsNew", StringComparison.OrdinalIgnoreCase))
            {
                return OperationStatus.Running;
            }
            else if (value.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                return OperationStatus.Completed;
            }
            else if (value.Equals("Failed", StringComparison.OrdinalIgnoreCase))
            {
                return OperationStatus.Failed;
            }
            else if (value.Equals("Canceled", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Terminated", StringComparison.OrdinalIgnoreCase))
            {
                return OperationStatus.Canceled;
            }
            else
            {
                return OperationStatus.Unknown;
            }
        }

        public override void WriteJson(JsonWriter writer, OperationStatus value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
