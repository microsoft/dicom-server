// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnsureThat;

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
            // We do not accept the null or empty specifiers, as TryParseExact does not accept them
            _formatSpecifier = EnsureArg.IsNotNullOrEmpty(formatSpecifier);
            _tryParse = exactMatch ? TryParseExact : Guid.TryParse;
        }

        private bool TryParseExact(string input, out Guid value)
            => Guid.TryParseExact(input, _formatSpecifier, out value);

        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType is JsonTokenType.String && _tryParse(reader.GetString(), out Guid value)
                ? value
                : throw new JsonException();

        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(_formatSpecifier));
    }
}
