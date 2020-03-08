// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Fhir.SqlServer.Features.Schema.Model;
using Microsoft.Health.Fhir.SqlServer.Features.Storage;

namespace Microsoft.Health.Fhir.SqlServer.Features.Query
{
    internal class SqlQueryGenerator : QueryFilterConditionVisitor
    {
        private IndentedStringBuilder _stringBuilder;
        private DicomQueryOptions _queryOptions;
        private SqlQueryParameterManager _parameters;

        private const string MappingTableAlias = "m";
        private const string StudyTableAlias = "st";
        private const string SeriesTableAlias = "se";

        public SqlQueryGenerator(
            IndentedStringBuilder stringBuilder,
            DicomQueryOptions queryOptions,
            SqlQueryParameterManager sqlQueryParameterManager)
        {
            _stringBuilder = stringBuilder;
            _queryOptions = queryOptions;
            _parameters = sqlQueryParameterManager;

            if (_queryOptions.QueryExpression.IsEmpty ||
                !_queryOptions.QueryExpression.FilterConditions.Any())
            {
                // maaping table is enough to get the dicom instances
                AppendUIDMappingTableQuery();
            }
            else
            {
                AppendMetadataTableQuery();
            }

            AppendOrderBy(MappingTableAlias);
            AppendOffsetAndFetch();
            return;
        }

        private void AppendMetadataTableQuery()
        {
            AppendSelect(MappingTableAlias);
            _stringBuilder.AppendLine($"FROM {VLatest.StudyMetadataCore.TableName} {StudyTableAlias}");

            AppendSeriesTableJoin();

            AppendCrossApplyMappingTable();

            _stringBuilder.AppendLine("WHERE 1 = 1");
            using (IndentedStringBuilder.DelimitedScope delimited = _stringBuilder.BeginDelimitedWhereClause())
            {
                AppendUIDsClause(StudyTableAlias, SeriesTableAlias);
                AppendFilterClause();
            }
        }

        private void AppendUIDMappingTableQuery()
        {
            AppendSelect(MappingTableAlias);
            _stringBuilder.AppendLine($"FROM {VLatest.UIDMapping.TableName} {MappingTableAlias}");

            _stringBuilder.AppendLine("WHERE 1 = 1");
            using (IndentedStringBuilder.DelimitedScope delimited = _stringBuilder.BeginDelimitedWhereClause())
            {
                AppendUIDsClause(MappingTableAlias, MappingTableAlias);
            }

            AppendStatusClause(MappingTableAlias);
        }

        private void AppendCrossApplyMappingTable()
        {
            var tableAlias = "x";
            _stringBuilder.AppendLine("CROSS APPLY").AppendLine(" ( ");
            _stringBuilder.AppendLine("SELECT TOP 1 *");
            _stringBuilder.AppendLine($"FROM {VLatest.UIDMapping.TableName} {tableAlias}");
            _stringBuilder.AppendLine("WHERE 1 = 1");
            using (IndentedStringBuilder.DelimitedScope delimited = _stringBuilder.BeginDelimitedWhereClause())
            {
                _stringBuilder
                    .Append("AND ")
                    .Append(VLatest.UIDMapping.StudyInstanceUID, tableAlias)
                    .Append(" = ")
                    .AppendLine(VLatest.StudyMetadataCore.StudyInstanceUID, StudyTableAlias);
                if (AnySeriesFilters())
                {
                    _stringBuilder
                        .Append("AND ")
                        .Append(VLatest.UIDMapping.SeriesInstanceUID, tableAlias)
                        .Append(" = ")
                        .AppendLine(VLatest.SeriesMetadataCore.SeriesInstanceUID, SeriesTableAlias);
                }

                AppendStatusClause(tableAlias);
                AppendOrderBy(tableAlias);
            }

            _stringBuilder.AppendLine($") {MappingTableAlias}");
        }

        private void AppendSeriesTableJoin()
        {
             if (AnySeriesFilters())
            {
                _stringBuilder.AppendLine($"INNER JOIN {VLatest.SeriesMetadataCore.TableName} {SeriesTableAlias}");
                _stringBuilder
                    .Append("ON ")
                    .Append(VLatest.SeriesMetadataCore.ID, SeriesTableAlias)
                    .Append(" = ")
                    .AppendLine(VLatest.StudyMetadataCore.ID, StudyTableAlias);
            }
        }

        private void AppendSelect(string tableAlias)
        {
            _stringBuilder
                .AppendLine("SELECT ")
                .AppendLine(VLatest.UIDMapping.StudyInstanceUID, tableAlias).Append(",")
                .AppendLine(VLatest.UIDMapping.SeriesInstanceUID, tableAlias).Append(",")
                .AppendLine(VLatest.UIDMapping.SOPInstanceUID, tableAlias);
        }

        private void AppendUIDsClause(string studyUIDtableAlias, string seriesUIDTableAlias)
        {
            if (_queryOptions.StudyInstanceUID != null)
            {
                _stringBuilder
                    .Append("AND ")
                    .AppendLine(VLatest.UIDMapping.StudyInstanceUID, studyUIDtableAlias)
                    .Append(" = ")
                    .Append(_parameters.AddParameter(VLatest.UIDMapping.StudyInstanceUID, _queryOptions.StudyInstanceUID))
                    .AppendLine();
            }

            if (_queryOptions.SeriesInstanceUID != null)
            {
                _stringBuilder
                    .Append("AND ")
                    .Append(VLatest.UIDMapping.SeriesInstanceUID, seriesUIDTableAlias)
                    .Append(" = ")
                    .Append(_parameters.AddParameter(VLatest.UIDMapping.SeriesInstanceUID, _queryOptions.SeriesInstanceUID))
                    .AppendLine();
            }
        }

        private void AppendStatusClause(string tableAlias)
        {
            // TODO set this from a enum
            var readyStatus = 1;
            _stringBuilder
                .Append("AND ")
                .Append(VLatest.UIDMapping.Status, tableAlias)
                .AppendLine($" = {readyStatus} ");
        }

        private void AppendFilterClause()
        {
            foreach (var filterCondition in _queryOptions.QueryExpression.FilterConditions)
            {
                filterCondition.Accept(this);
            }
        }

        private void AppendOffsetAndFetch()
        {
            _stringBuilder.AppendLine($"OFFSET {_queryOptions.QueryExpression.Offset} ROWS");
            _stringBuilder.AppendLine($"FETCH NEXT {_queryOptions.EvaluatedLimit} ROWS ONLY");
        }

        private void AppendOrderBy(string tableAlias)
        {
            _stringBuilder
                .Append("ORDER BY ")
                .Append(VLatest.UIDMapping.Watermark, tableAlias)
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
                .Append(dicomTagSqlEntry.SqlColumn, tableAlias).Append("=")
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
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, rangeValueMatchCondition.Minimum))
                .Append("AND ")
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, rangeValueMatchCondition.Maximum))
                .AppendLine();
        }

        private bool AnySeriesFilters()
        {
            return _queryOptions.QueryExpression.FilterConditions
               .Any(filter => DicomTagSqlEntry.GetDicomTagSqlEntry(filter.DicomTag).SqlTableType == SqlTableType.SeriesTable);
        }

        private string GetTableAlias(DicomTagSqlEntry sqlEntry)
        {
            switch (sqlEntry.SqlTableType)
            {
                case SqlTableType.MappingTable: return MappingTableAlias;
                case SqlTableType.StudyTable: return StudyTableAlias;
                case SqlTableType.SeriesTable: return SeriesTableAlias;
            }

            Debug.Fail("Invalid table type");
            return null;
        }
    }
}
