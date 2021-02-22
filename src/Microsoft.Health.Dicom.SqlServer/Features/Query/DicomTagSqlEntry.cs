// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.SqlServer.Features.Query
{
    internal class DicomTagSqlEntry
    {
        private static Dictionary<DicomTag, DicomTagSqlEntry> _tagToSqlMappingCache = new Dictionary<DicomTag, DicomTagSqlEntry>()
        {
                { DicomTag.StudyInstanceUID, new DicomTagSqlEntry(DicomTag.StudyInstanceUID, SqlTableType.StudyTable, VLatest.Study.StudyInstanceUid) },
                { DicomTag.StudyDate, new DicomTagSqlEntry(DicomTag.StudyDate, SqlTableType.StudyTable, VLatest.Study.StudyDate) },
                { DicomTag.StudyDescription, new DicomTagSqlEntry(DicomTag.StudyDescription, SqlTableType.StudyTable, VLatest.Study.StudyDescription) },
                { DicomTag.AccessionNumber, new DicomTagSqlEntry(DicomTag.AccessionNumber, SqlTableType.StudyTable, VLatest.Study.AccessionNumber) },
                { DicomTag.PatientID, new DicomTagSqlEntry(DicomTag.PatientID, SqlTableType.StudyTable, VLatest.Study.PatientId) },
                { DicomTag.PatientName, new DicomTagSqlEntry(DicomTag.PatientName, SqlTableType.StudyTable, VLatest.Study.PatientName, VLatest.StudyTable.PatientNameWords) },
                { DicomTag.ReferringPhysicianName, new DicomTagSqlEntry(DicomTag.ReferringPhysicianName, SqlTableType.StudyTable, VLatest.Study.ReferringPhysicianName) },
                { DicomTag.SeriesInstanceUID, new DicomTagSqlEntry(DicomTag.SeriesInstanceUID, SqlTableType.SeriesTable, VLatest.Series.SeriesInstanceUid) },
                { DicomTag.Modality, new DicomTagSqlEntry(DicomTag.Modality, SqlTableType.SeriesTable, VLatest.Series.Modality) },
                { DicomTag.PerformedProcedureStepStartDate, new DicomTagSqlEntry(DicomTag.PerformedProcedureStepStartDate, SqlTableType.SeriesTable, VLatest.Series.PerformedProcedureStepStartDate) },
                { DicomTag.SOPInstanceUID, new DicomTagSqlEntry(DicomTag.SOPInstanceUID, SqlTableType.InstanceTable, VLatest.Instance.SopInstanceUid) },
        };

        private static Dictionary<DicomVR, DicomTagSqlEntry> _customTagToSqlMappingCache = new Dictionary<DicomVR, DicomTagSqlEntry>()
        {
                { DicomVR.DA, new DicomTagSqlEntry(null, SqlTableType.CustomTagDateTimeTable, VLatest.CustomTagDateTime.TagValue) },
                { DicomVR.DT, new DicomTagSqlEntry(null, SqlTableType.CustomTagDateTimeTable, VLatest.CustomTagDateTime.TagValue) },
                { DicomVR.TM, new DicomTagSqlEntry(null, SqlTableType.CustomTagDateTimeTable, VLatest.CustomTagDateTime.TagValue) },
                { DicomVR.AE, new DicomTagSqlEntry(null, SqlTableType.CustomTagStringTable, VLatest.CustomTagString.TagValue) },
                { DicomVR.AS, new DicomTagSqlEntry(null, SqlTableType.CustomTagStringTable, VLatest.CustomTagString.TagValue) },
                { DicomVR.CS, new DicomTagSqlEntry(null, SqlTableType.CustomTagStringTable, VLatest.CustomTagString.TagValue) },
                { DicomVR.DS, new DicomTagSqlEntry(null, SqlTableType.CustomTagStringTable, VLatest.CustomTagString.TagValue) },
                { DicomVR.IS, new DicomTagSqlEntry(null, SqlTableType.CustomTagStringTable, VLatest.CustomTagString.TagValue) },
                { DicomVR.LO, new DicomTagSqlEntry(null, SqlTableType.CustomTagStringTable, VLatest.CustomTagString.TagValue) },
                { DicomVR.SH, new DicomTagSqlEntry(null, SqlTableType.CustomTagStringTable, VLatest.CustomTagString.TagValue) },
                { DicomVR.UI, new DicomTagSqlEntry(null, SqlTableType.CustomTagStringTable, VLatest.CustomTagString.TagValue) },
                { DicomVR.AT, new DicomTagSqlEntry(null, SqlTableType.CustomTagBigIntTable, VLatest.CustomTagBigInt.TagValue) },
                { DicomVR.SL, new DicomTagSqlEntry(null, SqlTableType.CustomTagBigIntTable, VLatest.CustomTagBigInt.TagValue) },
                { DicomVR.SS, new DicomTagSqlEntry(null, SqlTableType.CustomTagBigIntTable, VLatest.CustomTagBigInt.TagValue) },
                { DicomVR.UL, new DicomTagSqlEntry(null, SqlTableType.CustomTagBigIntTable, VLatest.CustomTagBigInt.TagValue) },
                { DicomVR.US, new DicomTagSqlEntry(null, SqlTableType.CustomTagBigIntTable, VLatest.CustomTagBigInt.TagValue) },
                { DicomVR.FL, new DicomTagSqlEntry(null, SqlTableType.CustomTagDoubleTable, VLatest.CustomTagDouble.TagValue) },
                { DicomVR.FD, new DicomTagSqlEntry(null, SqlTableType.CustomTagDoubleTable, VLatest.CustomTagDouble.TagValue) },
                { DicomVR.PN, new DicomTagSqlEntry(null, SqlTableType.CustomTagPersonNameTable, VLatest.CustomTagPersonName.TagValue, VLatest.CustomTagPersonNameTable.TagValueWords) },
        };

        private DicomTagSqlEntry(DicomTag dicomTag, SqlTableType sqlTableType, Column sqlColumn, string fullTextIndexColumnName = null)
        {
            DicomTag = dicomTag;
            SqlTableType = sqlTableType;
            SqlColumn = sqlColumn;
            FullTextIndexColumnName = fullTextIndexColumnName;
        }

        public SqlTableType SqlTableType { get; }

        public DicomTag DicomTag { get; private set; }

        public Column SqlColumn { get; }

        public string FullTextIndexColumnName { get; }

        public static DicomTagSqlEntry GetDicomTagSqlEntry(DicomTag dicomTag)
        {
            if (_tagToSqlMappingCache.ContainsKey(dicomTag))
            {
                return _tagToSqlMappingCache[dicomTag];
            }

            DicomTagSqlEntry existingEntry = _customTagToSqlMappingCache[dicomTag.GetDefaultVR()];
            DicomTagSqlEntry entry = new DicomTagSqlEntry(dicomTag, existingEntry.SqlTableType, existingEntry.SqlColumn, existingEntry.FullTextIndexColumnName);
            entry.DicomTag = dicomTag;
            return entry;
        }
    }
}
