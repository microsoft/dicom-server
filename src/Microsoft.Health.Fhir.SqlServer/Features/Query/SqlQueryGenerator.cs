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

            AppendOrderBy();
            AppendOffsetAndFetch();
            return;
        }

        private void AppendMetadataTableQuery()
        {
            AppendSelect(MappingTableAlias);
            _stringBuilder.AppendLine($"FROM {VLatest.StudyMetadataCore.TableName} {StudyTableAlias}");

            AppendSeriesTableJoin();

            AppendCrossApplyMappingTable();

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

            // append where clause
            if (_queryOptions.AnyFilterCondition)
            {
                using (IndentedStringBuilder.DelimitedScope delimited = _stringBuilder.BeginDelimitedWhereClause())
                {
                    AppendUIDsClause(MappingTableAlias, MappingTableAlias);
                }
            }
        }

        private void AppendCrossApplyMappingTable()
        {
            var tableAlias = "x";
            _stringBuilder.AppendLine("CROSS APPLY").AppendLine(" ( ");
            _stringBuilder.AppendLine("SELECT TOP 1 *");
            _stringBuilder.AppendLine($"FROM {VLatest.UIDMapping.TableName} {tableAlias}");
            using (IndentedStringBuilder.DelimitedScope delimited = _stringBuilder.BeginDelimitedWhereClause())
            {
                _stringBuilder.AppendLine(VLatest.UIDMapping.StudyInstanceUID, tableAlias)
                   .Append(" = ")
                   .Append(VLatest.StudyMetadataCore.StudyInstanceUID, StudyTableAlias);
                _stringBuilder.AppendLine(VLatest.UIDMapping.SeriesInstanceUID, tableAlias)
                   .Append(" = ")
                   .Append(VLatest.SeriesMetadataCore.SeriesInstanceUID, SeriesTableAlias);

                // TODO filter on status
            }

            _stringBuilder.AppendLine($") {MappingTableAlias}");
        }

        private void AppendSeriesTableJoin()
        {
             if (_queryOptions.QueryExpression.FilterConditions
                .Any(filter => DicomTagSqlEntry.GetDicomTagSqlEntry(filter.DicomTag).SqlTableType == SqlTableType.SeriesTable))
            {
                _stringBuilder.AppendLine($"INNER JOIN {VLatest.SeriesMetadataCore.TableName} {SeriesTableAlias}");
                _stringBuilder.AppendLine("ON")
                    .Append(VLatest.SeriesMetadataCore.ID, SeriesTableAlias).Append(" = ").Append(VLatest.StudyMetadataCore.ID, StudyTableAlias);
            }
        }

        private void AppendSelect(string tableAlias)
        {
            _stringBuilder.Append("SELECT ")
                .AppendLine(VLatest.UIDMapping.StudyInstanceUID, tableAlias).Append(",")
                .AppendLine(VLatest.UIDMapping.SeriesInstanceUID, tableAlias).Append(",")
                .AppendLine(VLatest.UIDMapping.SOPInstanceUID, tableAlias);
        }

        private void AppendUIDsClause(string studyUIDtableAlias, string seriesUIDTableAlias)
        {
            if (_queryOptions.StudyInstanceUID != null)
            {
                _stringBuilder.AppendLine(VLatest.UIDMapping.StudyInstanceUID, studyUIDtableAlias)
                    .Append(" = ")
                    .Append(_parameters.AddParameter(VLatest.UIDMapping.StudyInstanceUID, _queryOptions.StudyInstanceUID));
            }

            if (_queryOptions.SeriesInstanceUID != null)
            {
                _stringBuilder.AppendLine(VLatest.UIDMapping.SeriesInstanceUID, seriesUIDTableAlias)
                    .Append(" = ")
                    .Append(_parameters.AddParameter(VLatest.UIDMapping.SeriesInstanceUID, _queryOptions.SeriesInstanceUID));
            }
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
            _stringBuilder.AppendLine($"FETCH NEXT  {_queryOptions.EvaluatedLimit}  ROWS ONLY");
        }

        private void AppendOrderBy()
        {
            _stringBuilder.AppendLine("ORDER BY ")
                .Append(VLatest.UIDMapping.Watermark, MappingTableAlias).Append(" DESC");
        }

        public override void Visit(StringSingleValueMatchCondition stringSingleValueMatchCondition)
        {
            // TODO if PatientName and FuzzyMatch true generate a different condition
            DicomTagSqlEntry dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(stringSingleValueMatchCondition.DicomTag);
            var tableAlias = GetTableAlias(dicomTagSqlEntry);
            _stringBuilder.AppendLine(dicomTagSqlEntry.SqlColumn, tableAlias).Append("=")
                  .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, stringSingleValueMatchCondition.Value));
        }

        public override void Visit(DateRangeValueMatchCondition rangeValueMatchCondition)
        {
            DicomTagSqlEntry dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(rangeValueMatchCondition.DicomTag);
            var tableAlias = GetTableAlias(dicomTagSqlEntry);
            _stringBuilder.AppendLine(dicomTagSqlEntry.SqlColumn, tableAlias).Append(" BETWEEN ")
                  .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, rangeValueMatchCondition.Minimum))
                  .Append("AND ")
                  .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, rangeValueMatchCondition.Maximum));
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
