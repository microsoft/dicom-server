// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Query
{
    internal class SqlQueryGenerator : QueryFilterConditionVisitor
    {
        private readonly IndentedStringBuilder _stringBuilder;
        private readonly QueryExpression _queryExpression;
        private readonly SqlQueryParameterManager _parameters;
        private readonly SchemaVersion _schemaVersion;
        private const string SqlDateFormat = "yyyy-MM-dd HH:mm:ss.ffffff";
        private const string InstanceTableAlias = "i";
        private const string StudyTableAlias = "st";
        private const string SeriesTableAlias = "se";
        private const string ExtendedQueryTagLongTableAlias = "ctl";
        private const string ExtendedQueryTagDateTimeTableAlias = "ctdt";
        private const string ExtendedQueryTagDoubleTableAlias = "ctd";
        private const string ExtendedQueryTagPersonNameTableAlias = "ctpn";
        private const string ExtendedQueryTagStringTableAlias = "cts";

        public SqlQueryGenerator(
            IndentedStringBuilder stringBuilder,
            QueryExpression queryExpression,
            SqlQueryParameterManager sqlQueryParameterManager,
            SchemaVersion schemaVersion)
        {
            _stringBuilder = stringBuilder;
            _queryExpression = queryExpression;
            _parameters = sqlQueryParameterManager;
            _schemaVersion = schemaVersion;

            Build();
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

            AppendOptionRecompile();
        }

        private void AppendOptionRecompile()
        {
            _stringBuilder.AppendLine("OPTION(RECOMPILE)");
        }

        private void AppendFilterTable(string filterAlias)
        {
            _stringBuilder.AppendLine("( SELECT ");
            if (_queryExpression.IsInstanceIELevel())
            {
                _stringBuilder.AppendLine(VLatest.Study.StudyInstanceUid, InstanceTableAlias);
                _stringBuilder.Append(",").AppendLine(VLatest.Series.SeriesInstanceUid, InstanceTableAlias);
                _stringBuilder.Append(",").AppendLine(VLatest.Instance.SopInstanceUid, InstanceTableAlias);
                _stringBuilder.Append(",").AppendLine(VLatest.Instance.Watermark, InstanceTableAlias);
            }
            else
            {
                _stringBuilder.AppendLine(VLatest.Study.StudyKey, StudyTableAlias);
                if (_queryExpression.IsSeriesIELevel())
                {
                    _stringBuilder.Append(",").AppendLine(VLatest.Series.SeriesKey, SeriesTableAlias);
                }
            }

            _stringBuilder.AppendLine($"FROM {VLatest.Study.TableName} {StudyTableAlias}");
            if (_queryExpression.IsSeriesIELevel() || _queryExpression.IsInstanceIELevel())
            {
                _stringBuilder.AppendLine($"INNER JOIN {VLatest.Series.TableName} {SeriesTableAlias}");
                _stringBuilder
                    .Append("ON ")
                    .Append(VLatest.Series.StudyKey, SeriesTableAlias)
                    .Append(" = ")
                    .AppendLine(VLatest.Study.StudyKey, StudyTableAlias);
            }

            if (_queryExpression.IsInstanceIELevel())
            {
                _stringBuilder.AppendLine($"INNER JOIN {VLatest.Instance.TableName} {InstanceTableAlias}");
                _stringBuilder
                    .Append("ON ")
                    .Append(VLatest.Instance.SeriesKey, InstanceTableAlias)
                    .Append(" = ")
                    .AppendLine(VLatest.Series.SeriesKey, SeriesTableAlias);
                AppendStatusClause(InstanceTableAlias);
            }

            AppendExtendedQueryTagTables();

            _stringBuilder.AppendLine("WHERE 1 = 1");

            if ((int)_schemaVersion >= SchemaVersionConstants.SupportDataPartitionSchemaVersion)
            {
                // TODO: Actual PartitionKey should be passed as a filter condition
                _stringBuilder.AppendLine($"AND {StudyTableAlias}.{VLatest.Study.PartitionKey} = {DefaultPartition.Key}");
            }

            using (IndentedStringBuilder.DelimitedScope delimited = _stringBuilder.BeginDelimitedWhereClause())
            {
                AppendFilterClause();
            }

            AppendFilterPaging();

            _stringBuilder.AppendLine($") {filterAlias}");
        }

        private void AppendExtendedQueryTagTables()
        {
            foreach (QueryFilterCondition condition in _queryExpression.FilterConditions.Where(x => x.QueryTag.IsExtendedQueryTag))
            {
                QueryTag queryTag = condition.QueryTag;
                int tagKey = queryTag.ExtendedQueryTagStoreEntry.Key;
                ExtendedQueryTagDataType dataType = ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[queryTag.VR.Code];
                string extendedQueryTagTableAlias = null;
                _stringBuilder.Append("INNER JOIN ");
                switch (dataType)
                {
                    case ExtendedQueryTagDataType.StringData:
                        extendedQueryTagTableAlias = ExtendedQueryTagStringTableAlias + tagKey;
                        _stringBuilder.AppendLine($"{VLatest.ExtendedQueryTagString.TableName} {extendedQueryTagTableAlias}");

                        break;
                    case ExtendedQueryTagDataType.LongData:
                        extendedQueryTagTableAlias = ExtendedQueryTagLongTableAlias + tagKey;
                        _stringBuilder.AppendLine($"{VLatest.ExtendedQueryTagLong.TableName} {extendedQueryTagTableAlias}");

                        break;
                    case ExtendedQueryTagDataType.DoubleData:
                        extendedQueryTagTableAlias = ExtendedQueryTagDoubleTableAlias + tagKey;
                        _stringBuilder.AppendLine($"{VLatest.ExtendedQueryTagDouble.TableName} {extendedQueryTagTableAlias}");

                        break;
                    case ExtendedQueryTagDataType.DateTimeData:
                        extendedQueryTagTableAlias = ExtendedQueryTagDateTimeTableAlias + tagKey;
                        _stringBuilder.AppendLine($"{VLatest.ExtendedQueryTagDateTime.TableName} {extendedQueryTagTableAlias}");

                        break;
                    case ExtendedQueryTagDataType.PersonNameData:
                        extendedQueryTagTableAlias = ExtendedQueryTagPersonNameTableAlias + tagKey;
                        _stringBuilder.AppendLine($"{VLatest.ExtendedQueryTagPersonName.TableName} {extendedQueryTagTableAlias}");

                        break;
                }

                _stringBuilder
                    .Append("ON ")
                    .Append($"{extendedQueryTagTableAlias}.StudyKey")
                    .Append(" = ")
                    .AppendLine(VLatest.Study.StudyKey, StudyTableAlias);

                using (IndentedStringBuilder.DelimitedScope delimited = _stringBuilder.BeginDelimitedOnClause())
                {
                    if ((_queryExpression.IsSeriesIELevel() || _queryExpression.IsInstanceIELevel()) && queryTag.Level < QueryTagLevel.Study)
                    {
                        _stringBuilder
                            .Append("AND ")
                            .Append($"{extendedQueryTagTableAlias}.SeriesKey")
                            .Append(" = ")
                            .AppendLine(VLatest.Series.SeriesKey, SeriesTableAlias);
                    }

                    if (_queryExpression.IsInstanceIELevel() && queryTag.Level < QueryTagLevel.Series)
                    {
                        _stringBuilder
                            .Append("AND ")
                            .Append($"{extendedQueryTagTableAlias}.InstanceKey")
                            .Append(" = ")
                            .AppendLine(VLatest.Instance.InstanceKey, InstanceTableAlias);
                    }
                }
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
                    .Append(VLatest.Instance.StudyKey, tableAlias)
                    .Append(" = ")
                    .AppendLine(VLatest.Study.StudyKey, filterAlias);

                if (_queryExpression.IsSeriesIELevel())
                {
                    _stringBuilder
                        .Append("AND ")
                        .Append(VLatest.Instance.SeriesKey, tableAlias)
                        .Append(" = ")
                        .AppendLine(VLatest.Series.SeriesKey, filterAlias);
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
            byte validStatus = (byte)IndexStatus.Created;
            _stringBuilder
                .Append("AND ")
                .Append(VLatest.Instance.Status, tableAlias)
                .AppendLine($" = {validStatus} ");
        }

        private void AppendFilterClause()
        {
            foreach (var filterCondition in _queryExpression.FilterConditions)
            {
                filterCondition.Accept(this);
            }
        }

        private void AppendFilterPaging()
        {
            BigIntColumn orderColumn = VLatest.Instance.Watermark;
            string tableAlias = InstanceTableAlias;
            if (_queryExpression.IsStudyIELevel())
            {
                orderColumn = VLatest.Study.StudyKey;
                tableAlias = StudyTableAlias;
            }
            else if (_queryExpression.IsSeriesIELevel())
            {
                orderColumn = VLatest.Series.SeriesKey;
                tableAlias = SeriesTableAlias;
            }

            _stringBuilder.Append($"ORDER BY ")
                .Append(orderColumn, tableAlias)
                .Append(" DESC")
                .AppendLine();
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
            var queryTag = stringSingleValueMatchCondition.QueryTag;
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag);
            var tableAlias = GetTableAlias(dicomTagSqlEntry, queryTag.IsExtendedQueryTag ? queryTag.ExtendedQueryTagStoreEntry.Key : null);
            _stringBuilder
                .Append("AND ");

            AppendExtendedQueryTagKeyFilter(dicomTagSqlEntry, tableAlias, stringSingleValueMatchCondition);

            _stringBuilder
                .Append(dicomTagSqlEntry.SqlColumn, tableAlias)
                .Append("=")
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, stringSingleValueMatchCondition.Value))
                .AppendLine();
        }

        public override void Visit(DoubleSingleValueMatchCondition doubleSingleValueMatchCondition)
        {
            var queryTag = doubleSingleValueMatchCondition.QueryTag;
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag);
            var tableAlias = GetTableAlias(dicomTagSqlEntry, queryTag.IsExtendedQueryTag ? queryTag.ExtendedQueryTagStoreEntry.Key : null);
            _stringBuilder
                .Append("AND ");

            AppendExtendedQueryTagKeyFilter(dicomTagSqlEntry, tableAlias, doubleSingleValueMatchCondition);

            _stringBuilder
                .Append(dicomTagSqlEntry.SqlColumn, tableAlias)
                .Append("=")
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, doubleSingleValueMatchCondition.Value))
                .AppendLine();
        }

        public override void Visit(LongRangeValueMatchCondition rangeValueMatchCondition)
        {
            var queryTag = rangeValueMatchCondition.QueryTag;
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag);
            var tableAlias = GetTableAlias(dicomTagSqlEntry, queryTag.IsExtendedQueryTag ? queryTag.ExtendedQueryTagStoreEntry.Key : null);
            _stringBuilder
                .Append("AND ");

            AppendExtendedQueryTagKeyFilter(dicomTagSqlEntry, tableAlias, rangeValueMatchCondition);

            _stringBuilder
                .Append(dicomTagSqlEntry.SqlColumn, tableAlias).Append(" BETWEEN ")
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, rangeValueMatchCondition.Minimum.ToString()))
                .Append(" AND ")
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, rangeValueMatchCondition.Maximum.ToString()))
                .AppendLine();
        }

        public override void Visit(LongSingleValueMatchCondition longSingleValueMatchCondition)
        {
            var queryTag = longSingleValueMatchCondition.QueryTag;
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag);
            var tableAlias = GetTableAlias(dicomTagSqlEntry, queryTag.IsExtendedQueryTag ? queryTag.ExtendedQueryTagStoreEntry.Key : null);
            _stringBuilder
                .Append("AND ");

            AppendExtendedQueryTagKeyFilter(dicomTagSqlEntry, tableAlias, longSingleValueMatchCondition);

            _stringBuilder
                .Append(dicomTagSqlEntry.SqlColumn, tableAlias)
                .Append("=")
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, longSingleValueMatchCondition.Value))
                .AppendLine();
        }

        public override void Visit(DateRangeValueMatchCondition rangeValueMatchCondition)
        {
            var queryTag = rangeValueMatchCondition.QueryTag;
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag);
            var tableAlias = GetTableAlias(dicomTagSqlEntry, queryTag.IsExtendedQueryTag ? queryTag.ExtendedQueryTagStoreEntry.Key : null);
            _stringBuilder
                .Append("AND ");

            AppendExtendedQueryTagKeyFilter(dicomTagSqlEntry, tableAlias, rangeValueMatchCondition);

            _stringBuilder
                .Append(dicomTagSqlEntry.SqlColumn, tableAlias).Append(" BETWEEN ")
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, rangeValueMatchCondition.Minimum.ToString(SqlDateFormat)))
                .Append(" AND ")
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, rangeValueMatchCondition.Maximum.ToString(SqlDateFormat)))
                .AppendLine();
        }

        public override void Visit(DateSingleValueMatchCondition dateSingleValueMatchCondition)
        {
            var queryTag = dateSingleValueMatchCondition.QueryTag;
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag);
            var tableAlias = GetTableAlias(dicomTagSqlEntry, queryTag.IsExtendedQueryTag ? queryTag.ExtendedQueryTagStoreEntry.Key : null);
            _stringBuilder
                .Append("AND ");

            AppendExtendedQueryTagKeyFilter(dicomTagSqlEntry, tableAlias, dateSingleValueMatchCondition);

            _stringBuilder
                .Append(dicomTagSqlEntry.SqlColumn, tableAlias)
                .Append("=")
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, dateSingleValueMatchCondition.Value.ToString(SqlDateFormat)))
                .AppendLine();
        }

        public override void Visit(PersonNameFuzzyMatchCondition fuzzyMatchCondition)
        {
            var queryTag = fuzzyMatchCondition.QueryTag;
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag);
            var tableAlias = GetTableAlias(dicomTagSqlEntry, queryTag.IsExtendedQueryTag ? queryTag.ExtendedQueryTagStoreEntry.Key : null);

            var fuzzyMatchString = $"\"{fuzzyMatchCondition.Value}*\"";
            _stringBuilder
                .Append("AND ");

            AppendExtendedQueryTagKeyFilter(dicomTagSqlEntry, tableAlias, fuzzyMatchCondition);
            _stringBuilder
                .Append("CONTAINS(")
                .Append(tableAlias)
                .Append(".")
                .Append(dicomTagSqlEntry.FullTextIndexColumnName)
                .Append(", ")
                .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlColumn, fuzzyMatchString))
                .Append(")")
                .AppendLine();
        }

        private void AppendExtendedQueryTagKeyFilter(DicomTagSqlEntry dicomTagSqlEntry, string tableAlias, QueryFilterCondition filterCondition)
        {
            if (dicomTagSqlEntry.IsExtendedQueryTag)
            {
                _stringBuilder
                    .Append(dicomTagSqlEntry.SqlKeyColumn, tableAlias)
                    .Append("=")
                    .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlKeyColumn, filterCondition.QueryTag.ExtendedQueryTagStoreEntry.Key))
                    .AppendLine()
                    .Append("AND ");
            }
        }

        private static string GetTableAlias(DicomTagSqlEntry sqlEntry, int? extendedQueryTagKey)
        {
            string ret = null;
            switch (sqlEntry.SqlTableType)
            {
                case SqlTableType.InstanceTable:
                    ret = InstanceTableAlias;
                    break;
                case SqlTableType.StudyTable:
                    ret = StudyTableAlias;
                    break;
                case SqlTableType.SeriesTable:
                    ret = SeriesTableAlias;
                    break;
                case SqlTableType.ExtendedQueryTagLongTable:
                    ret = ExtendedQueryTagLongTableAlias;
                    break;
                case SqlTableType.ExtendedQueryTagDateTimeTable:
                    ret = ExtendedQueryTagDateTimeTableAlias;
                    break;
                case SqlTableType.ExtendedQueryTagDoubleTable:
                    ret = ExtendedQueryTagDoubleTableAlias;
                    break;
                case SqlTableType.ExtendedQueryTagPersonNameTable:
                    ret = ExtendedQueryTagPersonNameTableAlias;
                    break;
                case SqlTableType.ExtendedQueryTagStringTable:
                    ret = ExtendedQueryTagStringTableAlias;
                    break;
            }

            if (string.IsNullOrEmpty(ret))
            {
                Debug.Fail("Invalid table type");
                return null;
            }

            return ret + extendedQueryTagKey;
        }
    }
}
