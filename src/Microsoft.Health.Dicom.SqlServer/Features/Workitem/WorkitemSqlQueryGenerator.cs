// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Query;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Workitem;

internal class WorkitemSqlQueryGenerator : BaseSqlQueryGenerator
{
    private readonly BaseQueryExpression _queryExpression;
    private const string WorkitemTableAlias = "w";

    public WorkitemSqlQueryGenerator(
        IndentedStringBuilder stringBuilder,
        BaseQueryExpression queryExpression,
        SqlQueryParameterManager sqlQueryParameterManager,
        SchemaVersion schemaVersion,
        int partitionKey)
        : base(stringBuilder, queryExpression, sqlQueryParameterManager, schemaVersion, partitionKey)
    {
        _queryExpression = queryExpression;

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
        StringBuilder
            .AppendLine("( SELECT ")
            .Append(VLatest.Workitem.WorkitemUid, WorkitemTableAlias).AppendLine(",")
            .Append(VLatest.Workitem.WorkitemKey, WorkitemTableAlias).AppendLine();

        StringBuilder
            .Append(",")
            .Append(VLatest.Workitem.Watermark, WorkitemTableAlias)
            .AppendLine();

        StringBuilder.AppendLine($"FROM {VLatest.Workitem.TableName} {WorkitemTableAlias}");

        AppendLongSchemaQueryTables();

        StringBuilder.AppendLine("WHERE 1 = 1");

        AppendStatusClause(WorkitemTableAlias);

        AppendPartitionWhereClause(WorkitemTableAlias);

        using (IndentedStringBuilder.DelimitedScope delimited = StringBuilder.BeginDelimitedWhereClause())
        {
            AppendFilterClause();
        }

        AppendFilterPaging();

        StringBuilder.AppendLine($") {filterAlias}");
    }

    private void AppendLongSchemaQueryTables()
    {
        foreach (QueryFilterCondition condition in _queryExpression.FilterConditions.Where(x => x.QueryTag.IsWorkitemQueryTag))
        {
            AppendLongSchemaQueryTables(condition, out string extendedQueryTagTableAlias);

            StringBuilder
                .Append("ON ")
                .Append($"{extendedQueryTagTableAlias}.PartitionKey")
                .Append(" = ")
                .AppendLine(VLatest.Workitem.PartitionKey, WorkitemTableAlias);

            StringBuilder
                .Append("AND ")
                .Append($"{extendedQueryTagTableAlias}.ResourceType")
                .Append(" = ")
                .AppendLine($"{(int)QueryTagResourceType.Workitem}");

            StringBuilder
                .Append("AND ")
                .Append($"{extendedQueryTagTableAlias}.SopInstanceKey1")
                .Append(" = ")
                .AppendLine(VLatest.Workitem.WorkitemKey, WorkitemTableAlias);
        }
    }

    private void AppendSelect(string tableAlias)
    {
        StringBuilder
            .AppendLine("SELECT ")
            .Append(VLatest.Workitem.WorkitemKey, tableAlias).AppendLine(",")
            .Append(VLatest.Workitem.WorkitemUid, tableAlias).AppendLine();

        StringBuilder
            .Append(",")
            .Append(VLatest.Workitem.Watermark, tableAlias)
            .AppendLine();

        StringBuilder.AppendLine("FROM");
    }

    private void AppendStatusClause(string tableAlias)
    {
        byte validStatus = (byte)IndexStatus.Created;
        StringBuilder
            .Append("AND ")
            .Append(VLatest.Workitem.Status, tableAlias)
            .AppendLine($" = {validStatus} ");
    }

    private void AppendFilterPaging()
    {
        BigIntColumn orderColumn = VLatest.Workitem.WorkitemKey;
        string tableAlias = WorkitemTableAlias;

        StringBuilder.Append($"ORDER BY ")
            .Append(orderColumn, tableAlias)
            .Append(" DESC")
            .AppendLine();
        StringBuilder.AppendLine($"OFFSET {_queryExpression.Offset} ROWS");
        StringBuilder.AppendLine($"FETCH NEXT {_queryExpression.EvaluatedLimit} ROWS ONLY");
    }
}
