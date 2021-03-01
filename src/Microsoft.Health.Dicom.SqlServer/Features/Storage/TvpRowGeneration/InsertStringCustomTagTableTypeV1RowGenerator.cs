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
    internal class InsertStringCustomTagTableTypeV1RowGenerator : ITableValuedParameterRowGenerator<(IReadOnlyList<CustomTagStoreEntry>, DicomDataset), InsertStringCustomTagTableTypeV1Row>
    {
        public IEnumerable<InsertStringCustomTagTableTypeV1Row> GenerateRows((IReadOnlyList<CustomTagStoreEntry>, DicomDataset) input)
        {
            List<InsertStringCustomTagTableTypeV1Row> rows = new List<InsertStringCustomTagTableTypeV1Row>();

            if (input.Item1?.Count > 0)
            {
                Dictionary<long, (DicomTag, CustomTagLevel)> stringDataTags = CustomTagDataTypeRowGeneratorExtensions.DetermineKeysAndTagsForDataType(input.Item1, CustomTagDataType.StringData);
                if (stringDataTags.Count > 0)
                {
                    foreach (KeyValuePair<long, (DicomTag, CustomTagLevel)> stringDataTag in stringDataTags)
                    {
                        var value = input.Item2.GetSingleValueOrDefault<string>(stringDataTag.Value.Item1, null);
                        if (value != null)
                        {
                            rows.Add(new InsertStringCustomTagTableTypeV1Row(stringDataTag.Key, value, (byte)stringDataTag.Value.Item2));
                        }
                    }
                }
            }

            return rows;
        }
    }
}
