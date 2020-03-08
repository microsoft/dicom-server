// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Fhir.SqlServer.Features.Schema.Model;
using Microsoft.Health.Fhir.SqlServer.Features.Storage;

namespace Microsoft.Health.Fhir.SqlServer.Features.Query
{
    internal class SqlQueryGenerator : QueryFilterConditionVisitor
    {
        private IndentedStringBuilder _stringBuilder;
        private DicomQueryExpression _queryExpression;
        private SqlQueryParameterManager _parameters;
        private TableContext _tableContext;
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

            if (!_queryExpression.AnyFilters
                || IsUIDOnlyQuery())
            {
                _tableContext = TableContext.InstanceTable;
            }

            _tableContext = TableContext.MetadataTable;
            Build();
        }

        private enum TableContext
        {
            InstanceTable,
            MetadataTable,
        }

        private void Build()
        {
            if (_tableContext == TableContext.InstanceTable)
            {
                AppendInstanceTableQuery();
            }
            else
            {
                AppendMetadataTableQuery();
            }

            AppendOrderBy(InstanceTableAlias);
            AppendOffsetAndFetch();
        }

        private void AppendMetadataTableQuery()
        {
            AppendSelect(InstanceTableAlias);
            _stringBuilder.AppendLine($"FROM {VLatest.StudyMetadataCore.TableName} {StudyTableAlias}");

            AppendSeriesTableJoin();

            AppendInstanceTableJoin();

            _stringBuilder.AppendLine("WHERE 1 = 1");
            using (IndentedStringBuilder.DelimitedScope delimited = _stringBuilder.BeginDelimitedWhereClause())
            {
                AppendFilterClause();
            }
        }

        private void AppendInstanceTableQuery()
        {
            AppendSelect(InstanceTableAlias);
            _stringBuilder.AppendLine($"FROM {VLatest.Instance.TableName} {InstanceTableAlias}");

            _stringBuilder.AppendLine("WHERE 1 = 1");
            using (IndentedStringBuilder.DelimitedScope delimited = _stringBuilder.BeginDelimitedWhereClause())
            {
                AppendFilterClause();
            }

            AppendStatusClause(InstanceTableAlias);
        }

        private void AppendInstanceTableJoin()
        {
            var tableAlias = "x";
            _stringBuilder.AppendLine("CROSS APPLY").AppendLine(" ( ");
            _stringBuilder.AppendLine("SELECT TOP 1 *");
            _stringBuilder.AppendLine($"FROM {VLatest.Instance.TableName} {tableAlias}");
            _stringBuilder.AppendLine("WHERE 1 = 1");
            using (IndentedStringBuilder.DelimitedScope delimited = _stringBuilder.BeginDelimitedWhereClause())
            {
                _stringBuilder
                    .Append("AND ")
                    .Append(VLatest.Instance.StudyInstanceUID, tableAlias)
                    .Append(" = ")
                    .AppendLine(VLatest.StudyMetadataCore.StudyInstanceUID, StudyTableAlias);

                if (AnySeriesFilters())
                {
                    _stringBuilder
                        .Append("AND ")
                        .Append(VLatest.Instance.SeriesInstanceUID, tableAlias)
                        .Append(" = ")
                        .AppendLine(VLatest.SeriesMetadataCore.SeriesInstanceUID, SeriesTableAlias);
                }

                var instanceUIDFilter = _queryExpression.FilterConditions.Where(fc => fc.DicomTag == DicomTag.SOPInstanceUID).FirstOrDefault();
                if (instanceUIDFilter != null)
                {
                    var cond = instanceUIDFilter as StringSingleValueMatchCondition;
                    _stringBuilder
                        .Append("AND ")
                        .Append(VLatest.Instance.SOPInstanceUID, tableAlias)
                        .Append(" = ")
                        .Append(_parameters.AddParameter(VLatest.Instance.SOPInstanceUID, cond.Value))
                        .AppendLine();
                }

                AppendStatusClause(tableAlias);
                AppendOrderBy(tableAlias);
            }

            _stringBuilder.AppendLine($") {InstanceTableAlias}");
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
                .AppendLine(VLatest.Instance.StudyInstanceUID, tableAlias).Append(",")
                .AppendLine(VLatest.Instance.SeriesInstanceUID, tableAlias).Append(",")
                .AppendLine(VLatest.Instance.SOPInstanceUID, tableAlias);
        }

        private void AppendStatusClause(string tableAlias)
        {
            // TODO set this from a enum
            var readyStatus = 1;
            _stringBuilder
                .Append("AND ")
                .Append(VLatest.Instance.Status, tableAlias)
                .AppendLine($" = {readyStatus} ");
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
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, rangeValueMatchCondition.Minimum))
                .Append("AND ")
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, rangeValueMatchCondition.Maximum))
                .AppendLine();
        }

        private bool IsUIDOnlyQuery()
        {
            return !_queryExpression.FilterConditions
               .Any(filter => !_dicomUIDTags.Contains(filter.DicomTag));
        }

        private bool AnySeriesFilters()
        {
            return _queryExpression.FilterConditions
               .Any(filter => DicomTagSqlEntry.GetDicomTagSqlEntry(filter.DicomTag).SqlTableType == SqlTableType.SeriesTable);
        }

        private string GetTableAlias(DicomTagSqlEntry sqlEntry)
        {
            if (_tableContext == TableContext.InstanceTable
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
