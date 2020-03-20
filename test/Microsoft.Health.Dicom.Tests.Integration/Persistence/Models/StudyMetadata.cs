// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data.SqlClient;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence.Models
{
    public class StudyMetadata
    {
        public StudyMetadata(
            long id,
            string studyInstanceUid,
            int version,
            string patientID,
            string patientName,
            string referringPhysicianName,
            DateTime? studyDate,
            string studyDescription,
            string accessionNumber)
        {
            ID = id;
            StudyInstanceUid = studyInstanceUid;
            Version = version;
            PatientID = patientID;
            PatientName = patientName;
            ReferringPhysicianName = referringPhysicianName;
            StudyDate = studyDate;
            StudyDescription = studyDescription;
            AccessionNumber = accessionNumber;
        }

        public StudyMetadata(SqlDataReader sqlDataReader)
        {
            ID = sqlDataReader.GetInt64(0);
            StudyInstanceUid = sqlDataReader.GetString(1);
            Version = sqlDataReader.GetInt32(2);
            PatientID = sqlDataReader.GetString(3);
            PatientName = sqlDataReader.GetString(4);
            ReferringPhysicianName = sqlDataReader.GetString(5);
            StudyDate = sqlDataReader.IsDBNull(6) ? null : (DateTime?)sqlDataReader.GetDateTime(6);
            StudyDescription = sqlDataReader.GetString(7);
            AccessionNumber = sqlDataReader.GetString(8);
        }

        public long ID { get; }

        public string StudyInstanceUid { get; }

        public int Version { get; }

        public string PatientID { get; }

        public string PatientName { get; }

        public string ReferringPhysicianName { get; }

        public DateTime? StudyDate { get; }

        public string StudyDescription { get; }

        public string AccessionNumber { get; }
    }
}
