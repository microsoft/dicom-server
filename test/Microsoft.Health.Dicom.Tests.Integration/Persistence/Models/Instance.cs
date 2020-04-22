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
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            long watermark,
            byte status,
            DateTimeOffset lastStatusUpdatedDate,
            DateTimeOffset createdDate)
        {
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
            StudyInstanceUid = sqlDataReader.GetString(0);
            SeriesInstanceUid = sqlDataReader.GetString(1);
            SopInstanceUid = sqlDataReader.GetString(2);
            Watermark = sqlDataReader.GetInt64(3);
            Status = sqlDataReader.GetByte(4);
            LastStatusUpdatedDate = sqlDataReader.GetDateTimeOffset(5);
            CreatedDate = sqlDataReader.GetDateTimeOffset(6);
        }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string SopInstanceUid { get; }

        public long Watermark { get; }

        public byte Status { get; }

        public DateTimeOffset LastStatusUpdatedDate { get; }

        public DateTimeOffset CreatedDate { get; }
    }
}
