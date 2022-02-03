// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Query
{
    internal class SqlQueryGenerator : BaseSqlQueryGenerator
    {
        private readonly IndentedStringBuilder _stringBuilder;
        private readonly QueryExpression _queryExpression;
        private readonly SchemaVersion _schemaVersion;
        private const string InstanceTableAlias = "i";
        private const string StudyTableAlias = "st";
        private const string SeriesTableAlias = "se";

        public SqlQueryGenerator(
            IndentedStringBuilder stringBuilder,
            QueryExpression queryExpression,
            SqlQueryParameterManager sqlQueryParameterManager,
            SchemaVersion schemaVersion,
            int partitionKey)
            : base(stringBuilder, sqlQueryParameterManager, partitionKey)
        {
            _stringBuilder = stringBuilder;
            _queryExpression = queryExpression;
            _schemaVersion = schemaVersion;

            Build();
        }

        protected override int? GetKeyFromQueryTag(QueryTag queryTag)
        {
            return queryTag.IsExtendedQueryTag ? queryTag.ExtendedQueryTagStoreEntry.Key : null;
        }

        protected override bool IsIndexedQueryTag(QueryTag queryTag)
        {
            return queryTag.IsExtendedQueryTag;
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
                AppendPartitionJoinClause(SeriesTableAlias, StudyTableAlias);
            }

            if (_queryExpression.IsInstanceIELevel())
            {
                _stringBuilder.AppendLine($"INNER JOIN {VLatest.Instance.TableName} {InstanceTableAlias}");
                _stringBuilder
                    .Append("ON ")
                    .Append(VLatest.Instance.SeriesKey, InstanceTableAlias)
                    .Append(" = ")
                    .AppendLine(VLatest.Series.SeriesKey, SeriesTableAlias);
                AppendPartitionJoinClause(InstanceTableAlias, SeriesTableAlias);
                AppendStatusClause(InstanceTableAlias);
            }

            AppendExtendedQueryTagTables();

            _stringBuilder.AppendLine("WHERE 1 = 1");

            AppendPartitionWhereClause(StudyTableAlias);

            using (IndentedStringBuilder.DelimitedScope delimited = _stringBuilder.BeginDelimitedWhereClause())
            {
                AppendFilterClause();
            }

            AppendFilterPaging();

            _stringBuilder.AppendLine($") {filterAlias}");
        }

        private void AppendPartitionJoinClause(string tableAlias1, string tableAlias2)
        {
            if ((int)_schemaVersion >= SchemaVersionConstants.SupportDataPartitionSchemaVersion)
            {
                _stringBuilder.AppendLine($"AND {tableAlias1}.{VLatest.Partition.PartitionKey} = {tableAlias2}.{VLatest.Partition.PartitionKey}");
            }
        }

        private void AppendPartitionWhereClause(string tableAlias)
        {
            if ((int)_schemaVersion >= SchemaVersionConstants.SupportDataPartitionSchemaVersion)
            {
                _stringBuilder.AppendLine($"AND {tableAlias}.{VLatest.Partition.PartitionKey} = {PartitionKey}");
            }
        }

        private void AppendExtendedQueryTagTables()
        {
            foreach (QueryFilterCondition condition in _queryExpression.FilterConditions.Where(x => x.QueryTag.IsExtendedQueryTag))
            {
                AppendLongSchemaQueryTables(condition, out string extendedQueryTagTableAlias);

                _stringBuilder
                    .Append("ON ")
                    .Append($"{extendedQueryTagTableAlias}.PartitionKey")
                    .Append(" = ")
                    .AppendLine(VLatest.Study.PartitionKey, StudyTableAlias);

                var sopInstanceKey1Name = $"{extendedQueryTagTableAlias}.StudyKey";
                var sopInstanceKey2Name = $"{extendedQueryTagTableAlias}.SeriesKey";
                var sopInstanceKey3Name = $"{extendedQueryTagTableAlias}.InstanceKey";

                if ((int)_schemaVersion >= SchemaVersionConstants.SupportUpsRsSchemaVersion)
                {
                    sopInstanceKey1Name = $"{extendedQueryTagTableAlias}.SopInstanceKey1";
                    sopInstanceKey2Name = $"{extendedQueryTagTableAlias}.SopInstanceKey2";
                    sopInstanceKey3Name = $"{extendedQueryTagTableAlias}.SopInstanceKey3";

                    _stringBuilder
                        .Append("AND ")
                        .Append($"{extendedQueryTagTableAlias}.ResourceType")
                        .Append(" = ")
                        .AppendLine($"{(int)QueryTagResourceType.Image}");
                }

                _stringBuilder
                    .Append("AND ")
                    .Append(sopInstanceKey1Name)
                    .Append(" = ")
                    .AppendLine(VLatest.Study.StudyKey, StudyTableAlias);

                using (IndentedStringBuilder.DelimitedScope delimited = _stringBuilder.BeginDelimitedOnClause())
                {
                    if ((_queryExpression.IsSeriesIELevel() || _queryExpression.IsInstanceIELevel()) && condition.QueryTag.Level < QueryTagLevel.Study)
                    {
                        _stringBuilder
                            .Append("AND ")
                            .Append(sopInstanceKey2Name)
                            .Append(" = ")
                            .AppendLine(VLatest.Series.SeriesKey, SeriesTableAlias);
                    }

                    if (_queryExpression.IsInstanceIELevel() && condition.QueryTag.Level < QueryTagLevel.Series)
                    {
                        _stringBuilder
                            .Append("AND ")
                            .Append(sopInstanceKey3Name)
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
    }
}
