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
            string studyInstanceUid,
            long studyKey,
            string patientID,
            string patientName,
            string referringPhysicianName,
            DateTime? studyDate,
            string studyDescription,
            string accessionNumber)
        {
            StudyInstanceUid = studyInstanceUid;
            StudyKey = studyKey;
            PatientID = patientID;
            PatientName = patientName;
            ReferringPhysicianName = referringPhysicianName;
            StudyDate = studyDate;
            StudyDescription = studyDescription;
            AccessionNumber = accessionNumber;
        }

        public StudyMetadata(SqlDataReader sqlDataReader)
        {
            StudyKey = sqlDataReader.GetInt64(0);
            StudyInstanceUid = sqlDataReader.GetString(1);
            PatientID = sqlDataReader.GetString(2);
            PatientName = sqlDataReader.GetString(3);
            ReferringPhysicianName = sqlDataReader.GetString(4);
            StudyDate = sqlDataReader.IsDBNull(5) ? null : (DateTime?)sqlDataReader.GetDateTime(5);
            StudyDescription = sqlDataReader.GetString(6);
            AccessionNumber = sqlDataReader.GetString(7);
        }

        public long StudyKey { get; private set; }

        public string StudyInstanceUid { get; }

        public string Version { get; }

        public string PatientID { get; }

        public string PatientName { get; }

        public string ReferringPhysicianName { get; }

        public DateTime? StudyDate { get; }

        public string StudyDescription { get; }

        public string AccessionNumber { get; }
    }
}
