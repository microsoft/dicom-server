// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.SqlServer.Features.Query;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Workitem
{
    internal class WorkitemSqlQueryGenerator : BaseSqlQueryGenerator
    {
        private readonly IndentedStringBuilder _stringBuilder;
        private readonly BaseQueryExpression _queryExpression;
        private readonly SchemaVersion _schemaVersion;
        private const string WorkitemTableAlias = "w";

        public WorkitemSqlQueryGenerator(
            IndentedStringBuilder stringBuilder,
            BaseQueryExpression queryExpression,
            SqlQueryParameterManager sqlQueryParameterManager,
            SchemaVersion schemaVersion,
            int partitionKey)
            : base(stringBuilder, sqlQueryParameterManager, partitionKey)
        {
            _stringBuilder = stringBuilder;
            _queryExpression = queryExpression;
            _schemaVersion = schemaVersion;

            if ((int)_schemaVersion < SchemaVersionConstants.SupportUpsRsSchemaVersion)
            {
                throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
            }

            Build();
        }

        protected override int? GetKeyFromQueryTag(QueryTag queryTag)
        {
            return queryTag.IsWorkitemQueryTag ? queryTag.WorkitemQueryTagStoreEntry.Key : null;
        }

        protected override bool IsIndexedQueryTag(QueryTag queryTag)
        {
            return queryTag.IsWorkitemQueryTag;
        }

        private void Build()
        {
            string projectionTableAlias = "f";

            AppendSelect(projectionTableAlias);

            AppendFilterTable(projectionTableAlias);

            AppendOptionRecompile();
        }

        private void AppendFilterTable(string filterAlias)
        {
            _stringBuilder
                .AppendLine("( SELECT ")
                .Append(VLatest.Workitem.WorkitemUid, WorkitemTableAlias).AppendLine(",")
                .Append(VLatest.Workitem.WorkitemKey, WorkitemTableAlias).AppendLine()
                .AppendLine($"FROM {VLatest.Workitem.TableName} {WorkitemTableAlias}");

            AppendLongSchemaQueryTables();

            _stringBuilder.AppendLine("WHERE 1 = 1");

            AppendPartitionWhereClause(WorkitemTableAlias);

            using (IndentedStringBuilder.DelimitedScope delimited = _stringBuilder.BeginDelimitedWhereClause())
            {
                AppendFilterClause();
            }

            AppendFilterPaging();

            _stringBuilder.AppendLine($") {filterAlias}");
        }

        private void AppendPartitionWhereClause(string tableAlias)
        {
            if ((int)_schemaVersion >= SchemaVersionConstants.SupportDataPartitionSchemaVersion)
            {
                _stringBuilder.AppendLine($"AND {tableAlias}.{VLatest.Partition.PartitionKey} = {PartitionKey}");
            }
        }

        private void AppendLongSchemaQueryTables()
        {
            foreach (QueryFilterCondition condition in _queryExpression.FilterConditions.Where(x => x.QueryTag.IsWorkitemQueryTag))
            {
                AppendLongSchemaQueryTables(condition, out string extendedQueryTagTableAlias);

                _stringBuilder
                    .Append("ON ")
                    .Append($"{extendedQueryTagTableAlias}.PartitionKey")
                    .Append(" = ")
                    .AppendLine(VLatest.Workitem.PartitionKey, WorkitemTableAlias);

                _stringBuilder
                    .Append("AND ")
                    .Append($"{extendedQueryTagTableAlias}.ResourceType")
                    .Append(" = ")
                    .AppendLine($"{(int)QueryTagResourceType.Workitem}");

                _stringBuilder
                    .Append("AND ")
                    .Append($"{extendedQueryTagTableAlias}.SopInstanceKey1")
                    .Append(" = ")
                    .AppendLine(VLatest.Workitem.WorkitemKey, WorkitemTableAlias);
            }
        }

        private void AppendSelect(string tableAlias)
        {
            _stringBuilder
                .AppendLine("SELECT ")
                .Append(VLatest.Workitem.WorkitemKey, tableAlias).AppendLine(",")
                .Append(VLatest.Workitem.WorkitemUid, tableAlias).AppendLine()
                .AppendLine("FROM");
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
            BigIntColumn orderColumn = VLatest.Workitem.WorkitemKey;
            string tableAlias = WorkitemTableAlias;

            _stringBuilder.Append($"ORDER BY ")
                .Append(orderColumn, tableAlias)
                .Append(" DESC")
                .AppendLine();
            _stringBuilder.AppendLine($"OFFSET {_queryExpression.Offset} ROWS");
            _stringBuilder.AppendLine($"FETCH NEXT {_queryExpression.EvaluatedLimit} ROWS ONLY");
        }
    }
}
