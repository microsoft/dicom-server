// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using Dicom;
using Dicom.IO.Buffer;
using EnsureThat;

namespace Microsoft.Health.Dicom.Json.Serialization.Parsers
{
    public class StringSingleValueDicomItemParser<TDicomItemType> : IDicomItemParser
        where TDicomItemType : DicomItem
    {
        private readonly Func<DicomTag, string, DicomItem> _constructorWithValueDelegate;
        private readonly Func<DicomTag, IByteBuffer, DicomItem> _constructorWithDataDelegate;

        public DicomItem Parse(DicomTag tag, JsonElement element)
        {
            if (element.TryGetProperty("BulkDataURI", out JsonElement bulkDataUriElement))
            {
                return CreateByBulkDataUri(tag, bulkDataUriElement);
            }
            else
            {
                if (element.TryGetProperty("Value", out JsonElement propertyElement) ||
                    propertyElement.ValueKind != JsonValueKind.Array)
                {
                    throw new Exception("Malformed.");
                }

                string value = ParseValue(tag, propertyElement);

                return _constructorWithValueDelegate(tag, value);
            }
        }

        protected DicomItem CreateByBulkDataUri(DicomTag tag, JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                throw new Exception("Malformed.");
            }

            var value = new BulkDataUriByteBuffer(element.GetString());

            return _constructorWithDataDelegate(tag, value);
        }

        private string ParseValue(DicomTag tag, JsonElement element)
        {
            EnsureArg.IsNotNull(tag, nameof(tag));
            EnsureArg.IsTrue(element.ValueKind == JsonValueKind.Array, nameof(element));

            if (element.GetArrayLength() > 1)
            {
                throw new Exception("Malformed.");
            }

            foreach (JsonElement item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    return item.GetString();
                }
            }

            throw new Exception("Malformed.");
        }
    }
}
