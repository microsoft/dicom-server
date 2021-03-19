// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.SqlServer.Features.Query
{
    internal class DicomTagSqlEntry
    {
        private static Dictionary<DicomTag, DicomTagSqlEntry> _tagToSqlMappingCache = new Dictionary<DicomTag, DicomTagSqlEntry>()
        {
                { DicomTag.StudyInstanceUID, new DicomTagSqlEntry(SqlTableType.StudyTable, VLatest.Study.StudyInstanceUid) },
                { DicomTag.StudyDate, new DicomTagSqlEntry(SqlTableType.StudyTable, VLatest.Study.StudyDate) },
                { DicomTag.StudyDescription, new DicomTagSqlEntry(SqlTableType.StudyTable, VLatest.Study.StudyDescription) },
                { DicomTag.AccessionNumber, new DicomTagSqlEntry(SqlTableType.StudyTable, VLatest.Study.AccessionNumber) },
                { DicomTag.PatientID, new DicomTagSqlEntry(SqlTableType.StudyTable, VLatest.Study.PatientId) },
                { DicomTag.PatientName, new DicomTagSqlEntry(SqlTableType.StudyTable, VLatest.Study.PatientName, VLatest.StudyTable.PatientNameWords) },
                { DicomTag.ReferringPhysicianName, new DicomTagSqlEntry(SqlTableType.StudyTable, VLatest.Study.ReferringPhysicianName) },
                { DicomTag.SeriesInstanceUID, new DicomTagSqlEntry(SqlTableType.SeriesTable, VLatest.Series.SeriesInstanceUid) },
                { DicomTag.Modality, new DicomTagSqlEntry(SqlTableType.SeriesTable, VLatest.Series.Modality) },
                { DicomTag.PerformedProcedureStepStartDate, new DicomTagSqlEntry(SqlTableType.SeriesTable, VLatest.Series.PerformedProcedureStepStartDate) },
                { DicomTag.SOPInstanceUID, new DicomTagSqlEntry(SqlTableType.InstanceTable, VLatest.Instance.SopInstanceUid) },
        };

        private static Dictionary<DicomVR, DicomTagSqlEntry> _customTagToSqlMappingCache = new Dictionary<DicomVR, DicomTagSqlEntry>()
        {
                { DicomVR.DA, new DicomTagSqlEntry(SqlTableType.CustomTagDateTimeTable, VLatest.CustomTagDateTime.TagValue, null, VLatest.CustomTagDateTime.TagKey, true) },
                { DicomVR.AE, new DicomTagSqlEntry(SqlTableType.CustomTagStringTable, VLatest.CustomTagString.TagValue, null, VLatest.CustomTagString.TagKey, true) },
                { DicomVR.AS, new DicomTagSqlEntry(SqlTableType.CustomTagStringTable, VLatest.CustomTagString.TagValue, null, VLatest.CustomTagString.TagKey, true) },
                { DicomVR.CS, new DicomTagSqlEntry(SqlTableType.CustomTagStringTable, VLatest.CustomTagString.TagValue, null, VLatest.CustomTagString.TagKey, true) },
                { DicomVR.DS, new DicomTagSqlEntry(SqlTableType.CustomTagStringTable, VLatest.CustomTagString.TagValue, null, VLatest.CustomTagString.TagKey, true) },
                { DicomVR.IS, new DicomTagSqlEntry(SqlTableType.CustomTagStringTable, VLatest.CustomTagString.TagValue, null, VLatest.CustomTagString.TagKey, true) },
                { DicomVR.LO, new DicomTagSqlEntry(SqlTableType.CustomTagStringTable, VLatest.CustomTagString.TagValue, null, VLatest.CustomTagString.TagKey, true) },
                { DicomVR.SH, new DicomTagSqlEntry(SqlTableType.CustomTagStringTable, VLatest.CustomTagString.TagValue, null, VLatest.CustomTagString.TagKey, true) },
                { DicomVR.UI, new DicomTagSqlEntry(SqlTableType.CustomTagStringTable, VLatest.CustomTagString.TagValue, null, VLatest.CustomTagString.TagKey, true) },
                { DicomVR.SL, new DicomTagSqlEntry(SqlTableType.CustomTagBigIntTable, VLatest.CustomTagBigInt.TagValue, null, VLatest.CustomTagBigInt.TagKey, true) },
                { DicomVR.SS, new DicomTagSqlEntry(SqlTableType.CustomTagBigIntTable, VLatest.CustomTagBigInt.TagValue, null, VLatest.CustomTagBigInt.TagKey, true) },
                { DicomVR.UL, new DicomTagSqlEntry(SqlTableType.CustomTagBigIntTable, VLatest.CustomTagBigInt.TagValue, null, VLatest.CustomTagBigInt.TagKey, true) },
                { DicomVR.US, new DicomTagSqlEntry(SqlTableType.CustomTagBigIntTable, VLatest.CustomTagBigInt.TagValue, null, VLatest.CustomTagBigInt.TagKey, true) },
                { DicomVR.FL, new DicomTagSqlEntry(SqlTableType.CustomTagDoubleTable, VLatest.CustomTagDouble.TagValue, null, VLatest.CustomTagDouble.TagKey, true) },
                { DicomVR.FD, new DicomTagSqlEntry(SqlTableType.CustomTagDoubleTable, VLatest.CustomTagDouble.TagValue, null, VLatest.CustomTagDouble.TagKey, true) },
                { DicomVR.PN, new DicomTagSqlEntry(SqlTableType.CustomTagPersonNameTable, VLatest.CustomTagPersonName.TagValue, VLatest.CustomTagPersonNameTable.TagValueWords, VLatest.CustomTagPersonName.TagKey, true) },
        };

        private DicomTagSqlEntry(SqlTableType sqlTableType, Column sqlColumn, string fullTextIndexColumnName = null, Column sqlKeyColumn = null, bool isCustomTag = false)
        {
            SqlTableType = sqlTableType;
            SqlColumn = sqlColumn;
            FullTextIndexColumnName = fullTextIndexColumnName;
            SqlKeyColumn = sqlKeyColumn;
            IsCustomTag = isCustomTag;
        }

        public SqlTableType SqlTableType { get; }

        public Column SqlColumn { get; }

        public string FullTextIndexColumnName { get; }

        public Column SqlKeyColumn { get; }

        public bool IsCustomTag { get; }

        public static DicomTagSqlEntry GetDicomTagSqlEntry(DicomTag dicomTag, DicomVR customTagVR = null)
        {
            if (_tagToSqlMappingCache.ContainsKey(dicomTag))
            {
                return _tagToSqlMappingCache[dicomTag];
            }

            return _customTagToSqlMappingCache[customTagVR];
        }
    }
}
