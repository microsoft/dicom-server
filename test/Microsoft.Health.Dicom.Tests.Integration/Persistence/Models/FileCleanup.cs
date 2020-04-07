// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data.SqlClient;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence.Models
{
    public class FileCleanup
    {
        public FileCleanup(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            long watermark,
            DateTime deleteAfter)
        {
            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
            Watermark = watermark;
            DeleteAfter = deleteAfter;
        }

        public FileCleanup(SqlDataReader sqlDataReader)
        {
            StudyInstanceUid = sqlDataReader.GetString(0);
            SeriesInstanceUid = sqlDataReader.GetString(1);
            SopInstanceUid = sqlDataReader.GetString(2);
            Watermark = sqlDataReader.GetInt64(3);
            DeleteAfter = sqlDataReader.GetDateTime(4);
        }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string SopInstanceUid { get; }

        public long Watermark { get; }

        public DateTime DeleteAfter { get; }
    }
}
