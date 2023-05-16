// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Extensions;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;

internal class SqlExtendedQueryTagStoreV36 : SqlExtendedQueryTagStoreV16
{
    public SqlExtendedQueryTagStoreV36(SqlConnectionWrapperFactory sqlConnectionWrapperFactory, ILogger<SqlExtendedQueryTagStoreV36> logger)
        : base(sqlConnectionWrapperFactory, logger)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V36;

    public override async Task<IReadOnlyList<ExtendedQueryTagStoreJoinEntry>> GetExtendedQueryTagsAsync(int limit, long offset = 0, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsGte(limit, 1, nameof(limit));
        EnsureArg.IsGte(offset, 0, nameof(offset));

        var results = new List<ExtendedQueryTagStoreJoinEntry>();

        using (SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.GetExtendedQueryTagsV36.PopulateCommand(sqlCommandWrapper, limit, offset);

            var executionTimeWatch = Stopwatch.StartNew();
            using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    (int tagKey, string tagPath, string tagVR, string tagPrivateCreator, int tagLevel, int tagStatus, byte queryStatus, int errorCount, Guid? operationId) = reader.ReadRow(
                        VLatest.ExtendedQueryTag.TagKey,
                        VLatest.ExtendedQueryTag.TagPath,
                        VLatest.ExtendedQueryTag.TagVR,
                        VLatest.ExtendedQueryTag.TagPrivateCreator,
                        VLatest.ExtendedQueryTag.TagLevel,
                        VLatest.ExtendedQueryTag.TagStatus,
                        VLatest.ExtendedQueryTag.QueryStatus,
                        VLatest.ExtendedQueryTag.ErrorCount,
                        VLatest.ExtendedQueryTagOperation.OperationId.AsNullable());

                    results.Add(new ExtendedQueryTagStoreJoinEntry(tagKey, tagPath, tagVR, tagPrivateCreator, (QueryTagLevel)tagLevel, (ExtendedQueryTagStatus)tagStatus, (QueryStatus)queryStatus, errorCount, operationId));
                }

                executionTimeWatch.Stop();
                Logger.StoredProcedureSucceeded(nameof(VLatest.GetExtendedQueryTags), executionTimeWatch);
            }
        }

        return results;
    }
}
