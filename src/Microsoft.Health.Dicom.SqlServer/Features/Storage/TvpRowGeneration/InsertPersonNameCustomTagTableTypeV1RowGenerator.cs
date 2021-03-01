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
    internal class InsertPersonNameCustomTagTableTypeV1RowGenerator : ITableValuedParameterRowGenerator<(IReadOnlyList<CustomTagStoreEntry>, DicomDataset), InsertPersonNameCustomTagTableTypeV1Row>
    {
        public IEnumerable<InsertPersonNameCustomTagTableTypeV1Row> GenerateRows((IReadOnlyList<CustomTagStoreEntry>, DicomDataset) input)
        {
            List<InsertPersonNameCustomTagTableTypeV1Row> rows = new List<InsertPersonNameCustomTagTableTypeV1Row>();

            if (input.Item1?.Count > 0)
            {
                Dictionary<long, (DicomTag, CustomTagLevel)> personNameDataTags = CustomTagDataTypeRowGeneratorExtensions.DetermineKeysAndTagsForDataType(input.Item1, CustomTagDataType.PersonNameData);
                if (personNameDataTags.Count > 0)
                {
                    foreach (KeyValuePair<long, (DicomTag, CustomTagLevel)> personNameDataTag in personNameDataTags)
                    {
                        var value = input.Item2.GetSingleValueOrDefault<string>(personNameDataTag.Value.Item1, null);
                        if (value != null)
                        {
                            rows.Add(new InsertPersonNameCustomTagTableTypeV1Row(personNameDataTag.Key, value, (byte)personNameDataTag.Value.Item2));
                        }
                    }
                }
            }

            return rows;
        }
    }
}
