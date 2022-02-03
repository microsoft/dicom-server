// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Query
{
    internal abstract class BaseSqlQueryGenerator : QueryFilterConditionVisitor
    {
        private readonly IndentedStringBuilder _stringBuilder;
        private readonly SqlQueryParameterManager _parameters;
        private const string SqlDateFormat = "yyyy-MM-dd HH:mm:ss.ffffff";
        private const string InstanceTableAlias = "i";
        private const string StudyTableAlias = "st";
        private const string SeriesTableAlias = "se";
        protected readonly int PartitionKey;
        protected const string ExtendedQueryTagLongTableAlias = "ctl";
        protected const string ExtendedQueryTagDateTimeTableAlias = "ctdt";
        protected const string ExtendedQueryTagDoubleTableAlias = "ctd";
        protected const string ExtendedQueryTagPersonNameTableAlias = "ctpn";
        protected const string ExtendedQueryTagStringTableAlias = "cts";

        public BaseSqlQueryGenerator(
            IndentedStringBuilder stringBuilder,
            SqlQueryParameterManager sqlQueryParameterManager,
            int partitionKey)
        {
            _stringBuilder = stringBuilder;
            _parameters = sqlQueryParameterManager;
            PartitionKey = partitionKey;
        }

        protected abstract int? GetKeyFromQueryTag(QueryTag queryTag);

        protected abstract bool IsIndexedQueryTag(QueryTag queryTag);

        protected void AppendOptionRecompile()
        {
            _stringBuilder.AppendLine("OPTION(RECOMPILE)");
        }

        protected void AppendLongSchemaQueryTables(QueryFilterCondition condition, out string extendedQueryTagTableAlias)
        {
            QueryTag queryTag = condition.QueryTag;
            int tagKey = GetKeyFromQueryTag(queryTag).Value;
            ExtendedQueryTagDataType dataType = ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[queryTag.VR.Code];
            _stringBuilder.Append("INNER JOIN ");
            extendedQueryTagTableAlias = null;
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
        }

        public override void Visit(StringSingleValueMatchCondition stringSingleValueMatchCondition)
        {
            var queryTag = stringSingleValueMatchCondition.QueryTag;
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag, IsIndexedQueryTag(queryTag));

            var tableAlias = GetTableAlias(dicomTagSqlEntry, GetKeyFromQueryTag(queryTag));
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
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag, IsIndexedQueryTag(queryTag));
            var tableAlias = GetTableAlias(dicomTagSqlEntry, GetKeyFromQueryTag(queryTag));
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
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag, IsIndexedQueryTag(queryTag));
            var tableAlias = GetTableAlias(dicomTagSqlEntry, GetKeyFromQueryTag(queryTag));
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
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag, IsIndexedQueryTag(queryTag));
            var tableAlias = GetTableAlias(dicomTagSqlEntry, GetKeyFromQueryTag(queryTag));
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
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag, IsIndexedQueryTag(queryTag));
            var tableAlias = GetTableAlias(dicomTagSqlEntry, GetKeyFromQueryTag(queryTag));
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
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag, IsIndexedQueryTag(queryTag));
            var tableAlias = GetTableAlias(dicomTagSqlEntry, GetKeyFromQueryTag(queryTag));
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
            var dicomTagSqlEntry = DicomTagSqlEntry.GetDicomTagSqlEntry(queryTag, IsIndexedQueryTag(queryTag));
            var tableAlias = GetTableAlias(dicomTagSqlEntry, GetKeyFromQueryTag(queryTag));

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
            if (dicomTagSqlEntry.IsIndexedQueryTag)
            {
                _stringBuilder
                    .Append(dicomTagSqlEntry.SqlKeyColumn, tableAlias)
                    .Append("=")
                    .Append(_parameters.AddParameter(dicomTagSqlEntry.SqlKeyColumn, GetKeyFromQueryTag(filterCondition.QueryTag)))
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
