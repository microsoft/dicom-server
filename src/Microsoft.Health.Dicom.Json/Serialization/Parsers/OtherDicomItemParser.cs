// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using Dicom;
using Dicom.IO.Buffer;

namespace Microsoft.Health.Dicom.Json.Serialization.Parsers
{
    public class OtherDicomItemParser<TDicomItemType> : IDicomItemParser
        where TDicomItemType : DicomItem
    {
        private readonly Type _dicomItemType;
        private readonly Func<DicomTag, IByteBuffer, DicomItem> _constructorDelegate;

        public OtherDicomItemParser()
        {
            _dicomItemType = typeof(TDicomItemType);
        }

        public DicomItem Parse(DicomTag tag, JsonElement element)
        {
            IByteBuffer buffer = EmptyBuffer.Value;

            if (element.TryGetProperty("InlineBinary", out JsonElement inlineBinaryElement))
            {
                buffer = ParseInlineBinary(inlineBinaryElement);
            }
            else if (element.TryGetProperty("BulkdDataURI", out JsonElement bulkDataUriElement))
            {
                buffer = ParseBulkDataUri(bulkDataUriElement);
            }

            return _constructorDelegate(tag, buffer);
        }

        private static IByteBuffer ParseInlineBinary(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                throw new Exception("Malformed.");
            }

            return new MemoryByteBuffer(element.GetBytesFromBase64());
        }

        private static IBulkDataUriByteBuffer ParseBulkDataUri(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                throw new Exception("Malformed.");
            }

            return new BulkDataUriByteBuffer(element.GetString());
        }
    }
}
