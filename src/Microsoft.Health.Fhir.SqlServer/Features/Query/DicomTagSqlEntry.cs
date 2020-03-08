// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Fhir.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Fhir.SqlServer.Features.Query
{
    internal class DicomTagSqlEntry
    {
        private static Dictionary<DicomTag, DicomTagSqlEntry> _tagToSqlMappingCache = new Dictionary<DicomTag, DicomTagSqlEntry>()
        {
                { DicomTag.StudyInstanceUID, new DicomTagSqlEntry(DicomTag.StudyInstanceUID, SqlTableType.StudyTable, VLatest.StudyMetadataCore.StudyInstanceUID) },
                { DicomTag.StudyDate, new DicomTagSqlEntry(DicomTag.StudyDate, SqlTableType.StudyTable, VLatest.StudyMetadataCore.StudyDate) },
                { DicomTag.StudyDescription, new DicomTagSqlEntry(DicomTag.StudyDescription, SqlTableType.StudyTable, VLatest.StudyMetadataCore.StudyDescription) },
                { DicomTag.AccessionNumber, new DicomTagSqlEntry(DicomTag.AccessionNumber, SqlTableType.StudyTable, VLatest.StudyMetadataCore.AccessionNumer) },
                { DicomTag.PatientID, new DicomTagSqlEntry(DicomTag.PatientID, SqlTableType.StudyTable, VLatest.StudyMetadataCore.PatientID) },
                { DicomTag.PatientName, new DicomTagSqlEntry(DicomTag.PatientName, SqlTableType.StudyTable, VLatest.StudyMetadataCore.PatientName) },
                { DicomTag.SeriesInstanceUID, new DicomTagSqlEntry(DicomTag.SeriesInstanceUID, SqlTableType.SeriesTable, VLatest.SeriesMetadataCore.SeriesInstanceUID) },
                { DicomTag.Modality, new DicomTagSqlEntry(DicomTag.Modality, SqlTableType.SeriesTable, VLatest.SeriesMetadataCore.Modality) },
                { DicomTag.PerformedProcedureStepStartDate, new DicomTagSqlEntry(DicomTag.PerformedProcedureStepStartDate, SqlTableType.SeriesTable, VLatest.SeriesMetadataCore.PerformedProcedureStepStartDate) },
                { DicomTag.SOPInstanceUID, new DicomTagSqlEntry(DicomTag.SOPInstanceUID, SqlTableType.InstanceTable, VLatest.Instance.SOPInstanceUID) },
        };

        public DicomTagSqlEntry(DicomTag dicomTag, SqlTableType sqlTableType, Column sqlColumn)
        {
            DicomTag = dicomTag;
            SqlTableType = sqlTableType;
            SqlColumn = sqlColumn;
        }

        public SqlTableType SqlTableType { get; }

        public DicomTag DicomTag { get; }

        public Column SqlColumn { get; }

        public static DicomTagSqlEntry GetDicomTagSqlEntry(DicomTag dicomTag)
        {
            return _tagToSqlMappingCache[dicomTag];
        }
    }
}
