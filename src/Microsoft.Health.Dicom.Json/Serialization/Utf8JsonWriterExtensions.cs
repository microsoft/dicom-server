// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using EnsureThat;

namespace Microsoft.Health.Dicom.Json.Serialization
{
    public static class Utf8JsonWriterExtensions
    {
        public static void WriteNumberValue(this Utf8JsonWriter writer, ushort value)
        {
            EnsureArg.IsNotNull(writer, nameof(writer));

            writer.WriteNumberValue(value);
        }

        public static void WriteNumberValue(this Utf8JsonWriter writer, short value)
        {
            EnsureArg.IsNotNull(writer, nameof(writer));

            writer.WriteNumberValue(value);
        }
    }
}
