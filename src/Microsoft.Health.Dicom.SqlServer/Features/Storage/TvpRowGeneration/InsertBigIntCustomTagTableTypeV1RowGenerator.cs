// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.SqlServer.Features.CustomTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.SqlServer.Features.Storage.TvpRowGeneration
{
    internal class InsertBigIntCustomTagTableTypeV1RowGenerator : ITableValuedParameterRowGenerator<(IReadOnlyList<CustomTagStoreEntry>, DicomDataset), InsertBigIntCustomTagTableTypeV1Row>
    {
        public IEnumerable<InsertBigIntCustomTagTableTypeV1Row> GenerateRows((IReadOnlyList<CustomTagStoreEntry>, DicomDataset) input)
        {
            List<InsertBigIntCustomTagTableTypeV1Row> rows = new List<InsertBigIntCustomTagTableTypeV1Row>();
            if (input.Item1?.Count > 0)
            {
                Dictionary<long, (DicomTag, CustomTagLevel)> longDataTags = CustomTagDataTypeRowGeneratorExtensions.DetermineKeysAndTagsForDataType(input.Item1, CustomTagDataType.LongData);
                if (longDataTags.Count > 0)
                {
                    foreach (KeyValuePair<long, (DicomTag, CustomTagLevel)> longDataTag in longDataTags)
                    {
                        var value = input.Item2.GetSingleValueOrDefault<long?>(longDataTag.Value.Item1, null);
                        if (value != null)
                        {
                            rows.Add(new InsertBigIntCustomTagTableTypeV1Row(longDataTag.Key, value.Value, (byte)longDataTag.Value.Item2));
                        }
                    }
                }
            }

            return rows;
        }
    }
}
