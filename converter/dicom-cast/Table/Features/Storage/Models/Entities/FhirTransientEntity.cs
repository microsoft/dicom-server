// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Azure.Cosmos.Table;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage.Entities
{
    public class FhirTransientEntity : TableEntity
    {
        public FhirTransientEntity(string studyID, string seriesId, string instanceId, Exception ex)
        {
            PartitionKey = ex.GetType().Name;

            // TODO update the rowkey
            RowKey = studyID;

            StudyId = studyID;
            SeriesId = seriesId;
            InstanceId = instanceId;
            Exception = ex.ToString();
        }

        public string StudyId { get; set; }

        public string SeriesId { get; set; }

        public string InstanceId { get; set; }

        public string Exception { get; set; }
    }
}
