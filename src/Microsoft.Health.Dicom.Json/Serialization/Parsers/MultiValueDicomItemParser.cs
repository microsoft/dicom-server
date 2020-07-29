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
    public abstract class MultiValueDicomItemParser<TDicomItemType, TValueType> : IDicomItemParser
        where TDicomItemType : DicomItem
    {
        private readonly Func<DicomTag, TValueType[], DicomItem> _constructorWithValuesDelegate;
        private readonly Func<DicomTag, IByteBuffer, DicomItem> _constructorWithDataDelegate;

        protected MultiValueDicomItemParser(bool supportBulkDataUri)
        {
            DicomItemType = typeof(TDicomItemType);
            SupportBulkDataUri = supportBulkDataUri;
        }

        protected Type DicomItemType { get; }

        protected bool SupportBulkDataUri { get; }

        public DicomItem Parse(DicomTag tag, JsonElement element)
        {
            if (SupportBulkDataUri &&
                element.TryGetProperty("BulkDataURI", out JsonElement bulkDataUriElement))
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

                TValueType[] values = ParseValue(tag, propertyElement);

                return _constructorWithValuesDelegate(tag, values);
            }
        }

        protected abstract TValueType[] ParseValue(DicomTag tag, JsonElement element);

        protected DicomItem CreateByBulkDataUri(DicomTag tag, JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                throw new Exception("Malformed.");
            }

            var value = new BulkDataUriByteBuffer(element.GetString());

            return _constructorWithDataDelegate(tag, value);
        }
    }
}
