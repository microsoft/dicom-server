// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Json.Serialization.Parsers
{
    public class StringMultiValueDicomItemParser<TDicomItemType> : MultiValueDicomItemParser<TDicomItemType, string>
        where TDicomItemType : DicomItem
    {
        public StringMultiValueDicomItemParser(bool supportBulkDataUri = false)
            : base(supportBulkDataUri)
        {
        }

        protected override string[] ParseValue(DicomTag tag, JsonElement element)
        {
            EnsureArg.IsNotNull(tag, nameof(tag));
            EnsureArg.IsTrue(element.ValueKind == JsonValueKind.Array, nameof(element));

            var values = new string[element.GetArrayLength()];

            int index = 0;

            foreach (JsonElement item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    values[index] = item.GetString();
                }

                index++;
            }

            return values;
        }
    }
}
