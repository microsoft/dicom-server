// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Azure.Cosmos.Table;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage.Entities
{
    /// <summary>
    /// Entity used to represent a fhir intransient error
    /// </summary>
    public class FhirIntransientEntity : TableEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FhirIntransientEntity"/> class.
        /// </summary>
        /// <param name="studyUID">StudyUID of the changefeed entry that failed </param>
        /// <param name="seriesUID">SeriesUID of the changefeed entry that failed</param>
        /// <param name="instanceUID">InstanceUID of the changefeed entry that failed</param>
        /// <param name="ex">Theexception that was thrown</param>
        public FhirIntransientEntity(string studyUID, string seriesUID, string instanceUID, Exception ex)
        {
            PartitionKey = ex.GetType().Name;
            RowKey = Guid.NewGuid().ToString();

            StudyUID = studyUID;
            SeriesUID = seriesUID;
            InstanceUID = instanceUID;
            Exception = ex.ToString();
        }

        public string StudyUID { get; set; }

        public string SeriesUID { get; set; }

        public string InstanceUID { get; set; }

        public string Exception { get; set; }
    }
}
