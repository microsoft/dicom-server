// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Fhir.SqlServer.Features.Schema.Model;
using Microsoft.Health.Fhir.SqlServer.Features.Storage;

namespace Microsoft.Health.Fhir.SqlServer.Features.Query
{
    internal class SqlQueryGenerator : QueryFilterConditionVisitor
    {
        private readonly IndentedStringBuilder _stringBuilder;
        private readonly DicomQueryExpression _queryExpression;
        private readonly SqlQueryParameterManager _parameters;
        private readonly FilterTableContext _tableContext;
        private const string SqlDateFormat = "yyyy-MM-dd";
        private static HashSet<DicomTag> _dicomUIDTags = new HashSet<DicomTag>()
        {
            DicomTag.StudyInstanceUID,
            DicomTag.SeriesInstanceUID,
            DicomTag.SOPInstanceUID,
        };

        private const string InstanceTableAlias = "i";
        private const string StudyTableAlias = "st";
        private const string SeriesTableAlias = "se";

        public SqlQueryGenerator(
            IndentedStringBuilder stringBuilder,
            DicomQueryExpression queryExpression,
            SqlQueryParameterManager sqlQueryParameterManager)
        {
            _stringBuilder = stringBuilder;
            _queryExpression = queryExpression;
            _parameters = sqlQueryParameterManager;

            if (!_queryExpression.HasFilters
                || IsUidOnlyQuery())
            {
                _tableContext = FilterTableContext.InstanceTable;
            }
            else
            {
                _tableContext = FilterTableContext.MetadataTable;
            }

            Build();
        }

        private enum FilterTableContext
        {
            InstanceTable,
            MetadataTable,
        }

        private void Build()
        {
            string projectionTableAlias;
            string filterAlias = "f";
            string crossApplyAlias = "x";
            if (_queryExpression.IsInstanceIELevel())
            {
                projectionTableAlias = filterAlias;
            }
            else
            {
                projectionTableAlias = crossApplyAlias;
            }

            AppendSelect(projectionTableAlias);

            // get distinct UIDs based on IE Level
            AppendFilterTable(filterAlias);

            // cross apply with Instance table if needed to find the missing UIDs
            AppendCrossApplyTable(crossApplyAlias, filterAlias);

            AppendOrderBy(projectionTableAlias);
            AppendOffsetAndFetch();
        }

        private void AppendFilterTable(string filterAlias)
        {
            if (_tableContext == FilterTableContext.InstanceTable)
            {
                _stringBuilder.AppendLine("( SELECT DISTINCT");
                _stringBuilder.AppendLine(VLatest.Instance.StudyInstanceUid, InstanceTableAlias);
                if (_queryExpression.IsSeriesIELevel() || _queryExpression.IsInstanceIELevel())
                {
                    _stringBuilder.Append(",").AppendLine(VLatest.Instance.SeriesInstanceUid, InstanceTableAlias);
                }

                if (_queryExpression.IsInstanceIELevel())
                {
                    _stringBuilder.Append(",").AppendLine(VLatest.Instance.SopInstanceUid, InstanceTableAlias);
                    _stringBuilder.Append(",").AppendLine(VLatest.Instance.Watermark, InstanceTableAlias);
                }

                _stringBuilder.AppendLine($"FROM {VLatest.Instance.TableName} {InstanceTableAlias}");
                _stringBuilder.AppendLine("WHERE 1 = 1");
                AppendStatusClause(InstanceTableAlias);
                using (IndentedStringBuilder.DelimitedScope delimited = _stringBuilder.BeginDelimitedWhereClause())
                {
                    AppendFilterClause();
                }

                _stringBuilder.AppendLine($") {filterAlias}");
            }
            else
            {
                _stringBuilder.AppendLine("( SELECT DISTINCT");
                _stringBuilder.AppendLine(VLatest.StudyMetadataCore.StudyInstanceUid, StudyTableAlias);
                if (_queryExpression.IsSeriesIELevel() || _queryExpression.IsInstanceIELevel())
                {
                    _stringBuilder.Append(",").AppendLine(VLatest.SeriesMetadataCore.SeriesInstanceUid, SeriesTableAlias);
                }

                if (_queryExpression.IsInstanceIELevel())
                {
                    _stringBuilder.Append(",").AppendLine(VLatest.Instance.SopInstanceUid, InstanceTableAlias);
                    _stringBuilder.Append(",").AppendLine(VLatest.Instance.Watermark, InstanceTableAlias);
                }

                _stringBuilder.AppendLine($"FROM {VLatest.StudyMetadataCore.TableName} {StudyTableAlias}");
                if (_queryExpression.IsSeriesIELevel() || _queryExpression.IsInstanceIELevel())
                {
                    _stringBuilder.AppendLine($"INNER JOIN {VLatest.SeriesMetadataCore.TableName} {SeriesTableAlias}");
                    _stringBuilder
                        .Append("ON ")
                        .Append(VLatest.SeriesMetadataCore.ID, SeriesTableAlias)
                        .Append(" = ")
                        .AppendLine(VLatest.StudyMetadataCore.ID, StudyTableAlias);
                }

                if (_queryExpression.IsInstanceIELevel())
                {
                    _stringBuilder.AppendLine($"INNER JOIN {VLatest.Instance.TableName} {InstanceTableAlias}");
                    _stringBuilder
                        .Append("ON ")
                        .Append(VLatest.Instance.StudyInstanceUid, InstanceTableAlias)
                        .Append(" = ")
                        .AppendLine(VLatest.StudyMetadataCore.StudyInstanceUid, StudyTableAlias);
                    _stringBuilder
                        .Append("AND ")
                        .Append(VLatest.Instance.SeriesInstanceUid, InstanceTableAlias)
                        .Append(" = ")
                        .AppendLine(VLatest.SeriesMetadataCore.SeriesInstanceUid, SeriesTableAlias);
                    AppendStatusClause(InstanceTableAlias);
                }

                _stringBuilder.AppendLine("WHERE 1 = 1");
                using (IndentedStringBuilder.DelimitedScope delimited = _stringBuilder.BeginDelimitedWhereClause())
                {
                    AppendFilterClause();
                }

                _stringBuilder.AppendLine($") {filterAlias}");
            }
        }

        private void AppendCrossApplyTable(string crossApplyAlias, string filterAlias)
        {
            // already have the 3 UID projects needed so skip crossapply for projection
            if (_queryExpression.IsInstanceIELevel())
            {
                return;
            }

            string tableAlias = "a";

            _stringBuilder.AppendLine("CROSS APPLY").AppendLine(" ( ");
            _stringBuilder.AppendLine("SELECT TOP 1");
            _stringBuilder.Append(VLatest.Instance.StudyInstanceUid, tableAlias).AppendLine(",");
            _stringBuilder.Append(VLatest.Instance.SeriesInstanceUid, tableAlias).AppendLine(",");
            _stringBuilder.Append(VLatest.Instance.SopInstanceUid, tableAlias).AppendLine(",");
            _stringBuilder.AppendLine(VLatest.Instance.Watermark, tableAlias);
            _stringBuilder.AppendLine($"FROM {VLatest.Instance.TableName} {tableAlias}");
            _stringBuilder.AppendLine("WHERE 1 = 1");
            using (IndentedStringBuilder.DelimitedScope delimited = _stringBuilder.BeginDelimitedWhereClause())
            {
                _stringBuilder
                    .Append("AND ")
                    .Append(VLatest.Instance.StudyInstanceUid, tableAlias)
                    .Append(" = ")
                    .AppendLine(VLatest.StudyMetadataCore.StudyInstanceUid, filterAlias);

                if (_queryExpression.IsSeriesIELevel())
                {
                    _stringBuilder
                        .Append("AND ")
                        .Append(VLatest.Instance.SeriesInstanceUid, tableAlias)
                        .Append(" = ")
                        .AppendLine(VLatest.SeriesMetadataCore.SeriesInstanceUid, filterAlias);
                }

                AppendStatusClause(tableAlias);
                AppendOrderBy(tableAlias);
            }

            _stringBuilder.AppendLine($") {crossApplyAlias}");
        }

        private void AppendSelect(string tableAlias)
        {
            _stringBuilder
                .AppendLine("SELECT ")
                .Append(VLatest.Instance.StudyInstanceUid, tableAlias).AppendLine(",")
                .Append(VLatest.Instance.SeriesInstanceUid, tableAlias).AppendLine(",")
                .Append(VLatest.Instance.SopInstanceUid, tableAlias).AppendLine(",")
                .AppendLine(VLatest.Instance.Watermark, tableAlias)
                .AppendLine("FROM");
        }

        private void AppendStatusClause(string tableAlias)
        {
            byte invalidStatus = DicomIndexStatus.Creating;
            _stringBuilder
                .Append("AND ")
                .Append(VLatest.Instance.Status, tableAlias)
                .AppendLine($" <> {invalidStatus} ");
        }

        private void AppendFilterClause()
        {
            foreach (var filterCondition in _queryExpression.FilterConditions)
            {
                filterCondition.Accept(this);
            }
        }

        private void AppendOffsetAndFetch()
        {
            _stringBuilder.AppendLine($"OFFSET {_queryExpression.Offset} ROWS");
            _stringBuilder.AppendLine($"FETCH NEXT {_queryExpression.EvaluatedLimit} ROWS ONLY");
        }

        private void AppendOrderBy(string tableAlias)
        {
            _stringBuilder
                .Append("ORDER BY ")
                .Append(VLatest.Instance.Watermark, tableAlias)
                .Append(" DESC")
                .AppendLine();
        }

        public override void Visit(StringSingleValueMatchCondition stringSingleValueMatchCondition)
        {
            // TODO if PatientName and FuzzyMatch true generate a different condition
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(stringSingleValueMatchCondition.DicomTag);
            var tableAlias = GetTableAlias(dicomTagSqlEntry);
            _stringBuilder
                .Append("AND ")
                .Append(dicomTagSqlEntry.SqlColumn, tableAlias)
                .Append("=")
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, stringSingleValueMatchCondition.Value))
                .AppendLine();
        }

        public override void Visit(DateRangeValueMatchCondition rangeValueMatchCondition)
        {
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(rangeValueMatchCondition.DicomTag);
            var tableAlias = GetTableAlias(dicomTagSqlEntry);
            _stringBuilder
                .Append("AND ")
                .Append(dicomTagSqlEntry.SqlColumn, tableAlias).Append(" BETWEEN ")
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, rangeValueMatchCondition.Minimum.ToString(SqlDateFormat)))
                .Append(" AND ")
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, rangeValueMatchCondition.Maximum.ToString(SqlDateFormat)))
                .AppendLine();
        }

        public override void Visit(DateSingleValueMatchCondition dateSingleValueMatchCondition)
        {
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(dateSingleValueMatchCondition.DicomTag);
            var tableAlias = GetTableAlias(dicomTagSqlEntry);
            _stringBuilder
                .Append("AND ")
                .Append(dicomTagSqlEntry.SqlColumn, tableAlias)
                .Append("=")
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, dateSingleValueMatchCondition.Value.ToString(SqlDateFormat)))
                .AppendLine();
        }

        private bool IsUidOnlyQuery()
        {
            return !_queryExpression.FilterConditions
               .Any(filter => !_dicomUIDTags.Contains(filter.DicomTag));
        }

        private string GetTableAlias(DicomTagSqlEntry sqlEntry)
        {
            if (_tableContext == FilterTableContext.InstanceTable
                && _dicomUIDTags.Contains(sqlEntry.DicomTag))
            {
                return InstanceTableAlias;
            }

            switch (sqlEntry.SqlTableType)
            {
                case SqlTableType.InstanceTable: return InstanceTableAlias;
                case SqlTableType.StudyTable: return StudyTableAlias;
                case SqlTableType.SeriesTable: return SeriesTableAlias;
            }

            Debug.Fail("Invalid table type");
            return null;
        }
    }
}
