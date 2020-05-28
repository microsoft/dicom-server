// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data.SqlClient;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence.Models
{
    public class Instance
    {
        public Instance(
            long studyKey,
            long seriesKey,
            long instanceKey,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            long watermark,
            byte status,
            DateTime lastStatusUpdatedDate,
            DateTime createdDate)
        {
            StudyKey = studyKey;
            SeriesKey = seriesKey;
            InstanceKey = instanceKey;
            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
            Watermark = watermark;
            Status = status;
            LastStatusUpdatedDate = lastStatusUpdatedDate;
            CreatedDate = createdDate;
        }

        public Instance(SqlDataReader sqlDataReader)
        {
            InstanceKey = sqlDataReader.GetInt64(0);
            SeriesKey = sqlDataReader.GetInt64(1);
            InstanceKey = sqlDataReader.GetInt64(2);
            StudyInstanceUid = sqlDataReader.GetString(3);
            SeriesInstanceUid = sqlDataReader.GetString(4);
            SopInstanceUid = sqlDataReader.GetString(5);
            Watermark = sqlDataReader.GetInt64(6);
            Status = sqlDataReader.GetByte(7);

            DateTime unspecifiedLastStatusUpdatedDate = sqlDataReader.GetDateTime(8);
            LastStatusUpdatedDate = DateTime.SpecifyKind(unspecifiedLastStatusUpdatedDate, DateTimeKind.Utc);

            DateTime unspecifiedCreatedDate = sqlDataReader.GetDateTime(9);
            CreatedDate = DateTime.SpecifyKind(unspecifiedCreatedDate, DateTimeKind.Utc);
        }

        public long InstanceKey { get; private set; }

        public long SeriesKey { get; private set; }

        public long StudyKey { get; private set; }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string SopInstanceUid { get; }

        public long Watermark { get; }

        public byte Status { get; }

        public DateTime LastStatusUpdatedDate { get; }

        public DateTime CreatedDate { get; }
    }
}
