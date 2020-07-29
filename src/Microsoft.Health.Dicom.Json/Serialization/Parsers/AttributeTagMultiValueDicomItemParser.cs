// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.Json;
using Dicom;
using EnsureThat;
using Microsoft.Health.DicomCast.Core.Features.DicomWeb.Serializer;

namespace Microsoft.Health.Dicom.Json.Serialization.Parsers
{
    public class AttributeTagMultiValueDicomItemParser : MultiValueDicomItemParser<DicomAttributeTag, DicomTag>
    {
        public AttributeTagMultiValueDicomItemParser()
            : base(supportBulkDataUri: false)
        {
        }

        protected override DicomTag[] ParseValue(DicomTag tag, JsonElement element)
        {
            EnsureArg.IsNotNull(tag, nameof(tag));
            EnsureArg.IsTrue(element.ValueKind == JsonValueKind.Array, nameof(element));

            var values = new DicomTag[element.GetArrayLength()];

            int index = 0;

            foreach (JsonElement item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    string value = item.GetString();
                    ReadOnlySpan<char> valueSpan = value.AsSpan();

                    if (value.Length == 8 &&
                        ushort.TryParse(valueSpan.Slice(0, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort groupNumber) &&
                        ushort.TryParse(valueSpan.Slice(4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort elementNumber))
                    {
                        values[index] = new DicomTag(groupNumber, elementNumber);
                    }
                    else
                    {
                        values[index] = DicomDictionary.Default[value];
                    }
                }
                else
                {
                    throw new Exception("Malformed.");
                }

                index++;
            }

            return values;
        }
    }
}
