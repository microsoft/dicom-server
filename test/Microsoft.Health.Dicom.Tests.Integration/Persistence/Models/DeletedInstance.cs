// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data.SqlClient;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence.Models
{
    public class DeletedInstance
    {
        public DeletedInstance(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            long watermark,
            DateTime deletedDateTime,
            int retryCount,
            DateTime? retryAfter)
        {
            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
            Watermark = watermark;
            DeletedDateTime = deletedDateTime;
            RetryCount = retryCount;
            RetryAfter = retryAfter;
        }

        public DeletedInstance(SqlDataReader sqlDataReader)
        {
            StudyInstanceUid = sqlDataReader.GetString(0);
            SeriesInstanceUid = sqlDataReader.GetString(1);
            SopInstanceUid = sqlDataReader.GetString(2);
            Watermark = sqlDataReader.GetInt64(3);
            DeletedDateTime = sqlDataReader.GetDateTime(4);
            RetryCount = sqlDataReader.GetInt32(5);

            if (sqlDataReader.GetValue(6) != DBNull.Value)
            {
                RetryAfter = sqlDataReader.GetDateTime(6);
            }
        }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string SopInstanceUid { get; }

        public long Watermark { get; }

        public DateTime DeletedDateTime { get; }

        public int RetryCount { get; }

        public DateTime? RetryAfter { get; }
    }
}
