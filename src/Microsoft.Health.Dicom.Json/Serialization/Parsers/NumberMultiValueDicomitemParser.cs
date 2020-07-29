// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Json.Serialization.Parsers
{
    public class NumberMultiValueDicomItemParser<TDicomItemType, TValueType> : MultiValueDicomItemParser<TDicomItemType, TValueType>
        where TDicomItemType : DicomItem
    {
        private readonly Func<JsonElement, TValueType> _elementReader;

        public NumberMultiValueDicomItemParser(Func<JsonElement, TValueType> elementReader)
            : base(supportBulkDataUri: true)
        {
            _elementReader = elementReader;
        }

        protected override TValueType[] ParseValue(DicomTag tag, JsonElement element)
        {
            EnsureArg.IsNotNull(tag, nameof(tag));
            EnsureArg.IsTrue(element.ValueKind == JsonValueKind.Array, nameof(element));

            var values = new TValueType[element.GetArrayLength()];

            int index = 0;

            foreach (JsonElement item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Number)
                {
                    values[index] = _elementReader(item);
                }

                index++;
            }

            return values;
        }
    }
}
