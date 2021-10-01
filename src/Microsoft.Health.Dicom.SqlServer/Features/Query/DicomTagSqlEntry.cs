// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.SqlServer.Features.Query
{
    internal class DicomTagSqlEntry
    {
        private static readonly IReadOnlyDictionary<DicomTag, DicomTagSqlEntry> CoreQueryTagToSqlMapping = new Dictionary<DicomTag, DicomTagSqlEntry>()
        {
                { DicomTag.StudyInstanceUID, new DicomTagSqlEntry(SqlTableType.StudyTable, VLatest.Study.StudyInstanceUid) },
                { DicomTag.StudyDate, new DicomTagSqlEntry(SqlTableType.StudyTable, VLatest.Study.StudyDate) },
                { DicomTag.StudyDescription, new DicomTagSqlEntry(SqlTableType.StudyTable, VLatest.Study.StudyDescription) },
                { DicomTag.AccessionNumber, new DicomTagSqlEntry(SqlTableType.StudyTable, VLatest.Study.AccessionNumber) },
                { DicomTag.PatientID, new DicomTagSqlEntry(SqlTableType.StudyTable, VLatest.Study.PatientId) },
                { DicomTag.PatientName, new DicomTagSqlEntry(SqlTableType.StudyTable, VLatest.Study.PatientName, VLatest.StudyTable.PatientNameWords) },
                { DicomTag.ReferringPhysicianName, new DicomTagSqlEntry(SqlTableType.StudyTable, VLatest.Study.ReferringPhysicianName, VLatest.StudyTable.ReferringPhysicianNameWords) },
                { DicomTag.PatientBirthDate, new DicomTagSqlEntry(SqlTableType.StudyTable, VLatest.Study.PatientBirthDate) },
                { DicomTag.SeriesInstanceUID, new DicomTagSqlEntry(SqlTableType.SeriesTable, VLatest.Series.SeriesInstanceUid) },
                { DicomTag.Modality, new DicomTagSqlEntry(SqlTableType.SeriesTable, VLatest.Series.Modality) },
                { DicomTag.PerformedProcedureStepStartDate, new DicomTagSqlEntry(SqlTableType.SeriesTable, VLatest.Series.PerformedProcedureStepStartDate) },
                { DicomTag.ManufacturerModelName, new DicomTagSqlEntry(SqlTableType.SeriesTable, VLatest.Series.ManufacturerModelName) },
                { DicomTag.SOPInstanceUID, new DicomTagSqlEntry(SqlTableType.InstanceTable, VLatest.Instance.SopInstanceUid) },
        };

        private static readonly IReadOnlyDictionary<DicomVR, DicomTagSqlEntry> ExtendedQueryTagVRToSqlMapping = new Dictionary<DicomVR, DicomTagSqlEntry>()
        {
                { DicomVR.DA, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagDateTimeTable, VLatest.ExtendedQueryTagDateTime.TagValue, null, VLatest.ExtendedQueryTagDateTime.TagKey, true) },
                { DicomVR.DT, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagDateTimeTable, VLatest.ExtendedQueryTagDateTime.TagValue, null, VLatest.ExtendedQueryTagDateTime.TagKey, true) },
                { DicomVR.TM, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagLongTable, VLatest.ExtendedQueryTagLong.TagValue, null, VLatest.ExtendedQueryTagLong.TagKey, true) },
                { DicomVR.AE, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagStringTable, VLatest.ExtendedQueryTagString.TagValue, null, VLatest.ExtendedQueryTagString.TagKey, true) },
                { DicomVR.AS, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagStringTable, VLatest.ExtendedQueryTagString.TagValue, null, VLatest.ExtendedQueryTagString.TagKey, true) },
                { DicomVR.CS, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagStringTable, VLatest.ExtendedQueryTagString.TagValue, null, VLatest.ExtendedQueryTagString.TagKey, true) },
                { DicomVR.DS, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagStringTable, VLatest.ExtendedQueryTagString.TagValue, null, VLatest.ExtendedQueryTagString.TagKey, true) },
                { DicomVR.IS, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagStringTable, VLatest.ExtendedQueryTagString.TagValue, null, VLatest.ExtendedQueryTagString.TagKey, true) },
                { DicomVR.LO, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagStringTable, VLatest.ExtendedQueryTagString.TagValue, null, VLatest.ExtendedQueryTagString.TagKey, true) },
                { DicomVR.SH, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagStringTable, VLatest.ExtendedQueryTagString.TagValue, null, VLatest.ExtendedQueryTagString.TagKey, true) },
                { DicomVR.UI, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagStringTable, VLatest.ExtendedQueryTagString.TagValue, null, VLatest.ExtendedQueryTagString.TagKey, true) },
                { DicomVR.SL, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagLongTable, VLatest.ExtendedQueryTagLong.TagValue, null, VLatest.ExtendedQueryTagLong.TagKey, true) },
                { DicomVR.SS, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagLongTable, VLatest.ExtendedQueryTagLong.TagValue, null, VLatest.ExtendedQueryTagLong.TagKey, true) },
                { DicomVR.UL, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagLongTable, VLatest.ExtendedQueryTagLong.TagValue, null, VLatest.ExtendedQueryTagLong.TagKey, true) },
                { DicomVR.US, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagLongTable, VLatest.ExtendedQueryTagLong.TagValue, null, VLatest.ExtendedQueryTagLong.TagKey, true) },
                { DicomVR.FL, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagDoubleTable, VLatest.ExtendedQueryTagDouble.TagValue, null, VLatest.ExtendedQueryTagDouble.TagKey, true) },
                { DicomVR.FD, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagDoubleTable, VLatest.ExtendedQueryTagDouble.TagValue, null, VLatest.ExtendedQueryTagDouble.TagKey, true) },
                { DicomVR.PN, new DicomTagSqlEntry(SqlTableType.ExtendedQueryTagPersonNameTable, VLatest.ExtendedQueryTagPersonName.TagValue, VLatest.ExtendedQueryTagPersonNameTable.TagValueWords, VLatest.ExtendedQueryTagPersonName.TagKey, true) },
        };

        private DicomTagSqlEntry(SqlTableType sqlTableType, Column sqlColumn, string fullTextIndexColumnName = null, Column sqlKeyColumn = null, bool isExtendedQueryTag = false)
        {
            SqlTableType = sqlTableType;
            SqlColumn = sqlColumn;
            FullTextIndexColumnName = fullTextIndexColumnName;
            SqlKeyColumn = sqlKeyColumn;
            IsExtendedQueryTag = isExtendedQueryTag;
        }

        public SqlTableType SqlTableType { get; }

        public Column SqlColumn { get; }

        public string FullTextIndexColumnName { get; }

        public Column SqlKeyColumn { get; }

        public bool IsExtendedQueryTag { get; }

        public static DicomTagSqlEntry GetDicomTagSqlEntry(QueryTag queryTag)
        {
            return queryTag.IsExtendedQueryTag ? ExtendedQueryTagVRToSqlMapping[queryTag.VR] : CoreQueryTagToSqlMapping[queryTag.Tag];
        }
    }
}
