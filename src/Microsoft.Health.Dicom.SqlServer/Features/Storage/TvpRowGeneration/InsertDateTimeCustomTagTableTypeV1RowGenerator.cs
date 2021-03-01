// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.SqlServer.Features.CustomTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.SqlServer.Features.Storage.TvpRowGeneration
{
    internal class InsertDateTimeCustomTagTableTypeV1RowGenerator : ITableValuedParameterRowGenerator<(IReadOnlyList<CustomTagStoreEntry>, DicomDataset), InsertDateTimeCustomTagTableTypeV1Row>
    {
        public IEnumerable<InsertDateTimeCustomTagTableTypeV1Row> GenerateRows((IReadOnlyList<CustomTagStoreEntry>, DicomDataset) input)
        {
            List<InsertDateTimeCustomTagTableTypeV1Row> rows = new List<InsertDateTimeCustomTagTableTypeV1Row>();
            if (input.Item1?.Count > 0)
            {
                Dictionary<long, (DicomTag, CustomTagLevel)> dateTimeDataTags = CustomTagDataTypeRowGeneratorExtensions.DetermineKeysAndTagsForDataType(input.Item1, CustomTagDataType.DateTimeData);
                if (dateTimeDataTags.Count > 0)
                {
                    foreach (KeyValuePair<long, (DicomTag, CustomTagLevel)> dateTimeDataTag in dateTimeDataTags)
                    {
                        var value = input.Item2.GetSingleValueOrDefault<DateTime>(dateTimeDataTag.Value.Item1, DateTime.MinValue);
                        if (value != DateTime.MinValue)
                        {
                            rows.Add(new InsertDateTimeCustomTagTableTypeV1Row(dateTimeDataTag.Key, value, (byte)dateTimeDataTag.Value.Item2));
                        }
                    }
                }
            }

            return rows;
        }
    }
}
