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
        public SeriesMetadata(long id, string seriesInstanceUid, int version, string modality, DateTime? performedProcedureStepStartDate)
        {
            ID = id;
            SeriesInstanceUid = seriesInstanceUid;
            Version = version;
            Modality = modality;
            PerformedProcedureStepStartDate = performedProcedureStepStartDate;
        }

        public SeriesMetadata(SqlDataReader sqlDataReader)
        {
            ID = sqlDataReader.GetInt64(0);
            SeriesInstanceUid = sqlDataReader.GetString(1);
            Version = sqlDataReader.GetInt32(2);
            Modality = sqlDataReader.GetString(3);
            PerformedProcedureStepStartDate = sqlDataReader.IsDBNull(4) ? null : (DateTime?)sqlDataReader.GetDateTime(4);
        }

        public long ID { get; }

        public string SeriesInstanceUid { get; }

        public int Version { get; }

        public string Modality { get; }

        public DateTime? PerformedProcedureStepStartDate { get; }
    }
}
