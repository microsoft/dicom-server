// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Serialization
{
    internal class JsonGuidConverter : JsonConverter<Guid>
    {
        private delegate bool TryParse(string input, out Guid value);

        private readonly string _formatSpecifier;
        private readonly TryParse _tryParse;

        public JsonGuidConverter(string formatSpecifier)
            : this(formatSpecifier, false)
        {
        }

        public JsonGuidConverter(string formatSpecifier, bool exactMatch)
        {
            // Note that null and empty string are equivalent to "D"
            _formatSpecifier = formatSpecifier;
            _tryParse = exactMatch ? TryParseExact : Guid.TryParse;
        }

        public override Guid ReadJson(JsonReader reader, Type objectType, Guid existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            EnsureArg.IsNotNull(reader, nameof(reader));

            if (reader.TokenType is not JsonToken.String)
            {
                throw new JsonReaderException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.InvalidJsonToken,
                        JsonToken.String,
                        reader.TokenType));
            }

            if (!_tryParse(reader.Value as string, out Guid value))
            {
                throw new JsonReaderException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.InvalidStringParse,
                        reader.Value,
                        typeof(Guid)));
            }

            return value;
        }

        public override void WriteJson(JsonWriter writer, Guid value, JsonSerializer serializer)
        {
            EnsureArg.IsNotNull(writer, nameof(writer));
            writer.WriteValue(value.ToString(_formatSpecifier));
        }

        private bool TryParseExact(string input, out Guid value)
            => Guid.TryParseExact(input, _formatSpecifier, out value);
    }
}
