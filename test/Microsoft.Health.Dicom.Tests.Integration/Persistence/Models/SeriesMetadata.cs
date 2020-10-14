// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence.Models
{
    public class SeriesMetadata
    {
        public SeriesMetadata(long seriesKey, long studyKey, string seriesInstanceUid, string modality, DateTime? performedProcedureStepStartDate)
        {
            SeriesKey = seriesKey;
            StudyKey = studyKey;
            SeriesInstanceUid = seriesInstanceUid;
            Modality = modality;
            PerformedProcedureStepStartDate = performedProcedureStepStartDate;
        }

        public SeriesMetadata(SqlDataReader sqlDataReader)
        {
            SeriesKey = sqlDataReader.GetInt64(0);
            StudyKey = sqlDataReader.GetInt64(1);
            SeriesInstanceUid = sqlDataReader.GetString(2);
            Modality = sqlDataReader.GetString(3);
            PerformedProcedureStepStartDate = sqlDataReader.IsDBNull(4) ? null : (DateTime?)sqlDataReader.GetDateTime(4);
        }

        public long SeriesKey { get; private set; }

        public long StudyKey { get; private set; }

        public string SeriesInstanceUid { get; }

        public string Modality { get; }

        public DateTime? PerformedProcedureStepStartDate { get; }
    }
}
