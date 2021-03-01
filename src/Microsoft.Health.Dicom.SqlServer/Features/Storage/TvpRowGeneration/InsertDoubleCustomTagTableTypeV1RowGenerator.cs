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
    internal class InsertDoubleCustomTagTableTypeV1RowGenerator : ITableValuedParameterRowGenerator<(IReadOnlyList<CustomTagStoreEntry>, DicomDataset), InsertDoubleCustomTagTableTypeV1Row>
    {
        public IEnumerable<InsertDoubleCustomTagTableTypeV1Row> GenerateRows((IReadOnlyList<CustomTagStoreEntry>, DicomDataset) input)
        {
            List<InsertDoubleCustomTagTableTypeV1Row> rows = new List<InsertDoubleCustomTagTableTypeV1Row>();
            if (input.Item1?.Count > 0)
            {
                Dictionary<long, (DicomTag, CustomTagLevel)> doubleDataTags = CustomTagDataTypeRowGeneratorExtensions.DetermineKeysAndTagsForDataType(input.Item1, CustomTagDataType.DoubleData);
                if (doubleDataTags.Count > 0)
                {
                    foreach (KeyValuePair<long, (DicomTag, CustomTagLevel)> doubleDataTag in doubleDataTags)
                    {
                        var value = input.Item2.GetSingleValueOrDefault<double?>(doubleDataTag.Value.Item1, null);
                        if (value != null)
                        {
                            rows.Add(new InsertDoubleCustomTagTableTypeV1Row(doubleDataTag.Key, value.Value, (byte)doubleDataTag.Value.Item2));
                        }
                    }
                }
            }

            return rows;
        }
    }
}
