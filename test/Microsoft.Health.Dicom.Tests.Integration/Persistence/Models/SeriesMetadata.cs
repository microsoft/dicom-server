// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data.SqlClient;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence.Models
{
    public class SeriesMetadata
    {
        public SeriesMetadata(string studyInstanceUid, string seriesInstanceUid, string version, string modality, DateTime? performedProcedureStepStartDate)
        {
            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            Version = version;
            Modality = modality;
            PerformedProcedureStepStartDate = performedProcedureStepStartDate;
        }

        public SeriesMetadata(SqlDataReader sqlDataReader)
        {
            SeriesInstanceUid = sqlDataReader.GetString(0);
            StudyInstanceUid = sqlDataReader.GetString(1);
            Version = sqlDataReader.GetString(2);
            Modality = sqlDataReader.GetString(3);
            PerformedProcedureStepStartDate = sqlDataReader.IsDBNull(4) ? null : (DateTime?)sqlDataReader.GetDateTime(4);
        }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string Version { get; }

        public string Modality { get; }

        public DateTime? PerformedProcedureStepStartDate { get; }
    }
}
