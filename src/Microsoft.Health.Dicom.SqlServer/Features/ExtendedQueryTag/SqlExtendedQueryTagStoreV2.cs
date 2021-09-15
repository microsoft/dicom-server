// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag
{
    internal class SqlExtendedQueryTagStoreV2 : SqlExtendedQueryTagStoreV1
    {
        public SqlExtendedQueryTagStoreV2(
           SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
           ILogger<SqlExtendedQueryTagStoreV2> logger)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            ConnectionWrapperFactory = sqlConnectionWrapperFactory;
            Logger = logger;
        }

        public override SchemaVersion Version => SchemaVersion.V2;

        protected SqlConnectionWrapperFactory ConnectionWrapperFactory { get; }

        protected ILogger Logger { get; }

        public override async Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> AddExtendedQueryTagsAsync(
            IEnumerable<AddExtendedQueryTagEntry> extendedQueryTagEntries,
            int maxCount,
            bool ready = false,
            CancellationToken cancellationToken = default)
        {
            if (ready)
            {
                throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
            }

            using (SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                IEnumerable<AddExtendedQueryTagsInputTableTypeV1Row> rows = extendedQueryTagEntries.Select(ToAddExtendedQueryTagsInputTableTypeV1Row);

                V2.AddExtendedQueryTags.PopulateCommand(sqlCommandWrapper, new V2.AddExtendedQueryTagsTableValuedParameters(rows));

                try
                {
                    await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
                    var allTags = (await GetAllExtendedQueryTagsAsync(cancellationToken)).ToDictionary(x => x.Path);

                    return extendedQueryTagEntries
                        .Select(x => allTags[x.Path])
                        .ToList();
                }
                catch (SqlException ex)
                {
                    throw ex.Number switch
                    {
                        SqlErrorCodes.Conflict => new ExtendedQueryTagsAlreadyExistsException(),
                        _ => new DataStoreException(ex),
                    };
                }
            }
        }

        public override async Task<ExtendedQueryTagStoreEntry> GetExtendedQueryTagAsync(string path, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                V2.GetExtendedQueryTag.PopulateCommand(sqlCommandWrapper, path);

                var executionTimeWatch = Stopwatch.StartNew();
                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        throw new ExtendedQueryTagNotFoundException(string.Format(DicomCoreResource.ExtendedQueryTagNotFound, path));
                    }

                    (int tagKey, string tagPath, string tagVR, string tagPrivateCreator, int tagLevel, int tagStatus) = reader.ReadRow(
                        V2.ExtendedQueryTag.TagKey,
                        V2.ExtendedQueryTag.TagPath,
                        V2.ExtendedQueryTag.TagVR,
                        V2.ExtendedQueryTag.TagPrivateCreator,
                        V2.ExtendedQueryTag.TagLevel,
                        V2.ExtendedQueryTag.TagStatus);

                    executionTimeWatch.Stop();
                    Logger.LogInformation(executionTimeWatch.ElapsedMilliseconds.ToString());

                    return new ExtendedQueryTagStoreEntry(tagKey, tagPath, tagVR, tagPrivateCreator, (QueryTagLevel)tagLevel, (ExtendedQueryTagStatus)tagStatus, QueryStatus.Enabled, 0);
                }
            }
        }

        internal static AddExtendedQueryTagsInputTableTypeV1Row ToAddExtendedQueryTagsInputTableTypeV1Row(AddExtendedQueryTagEntry entry)
        {
            return new AddExtendedQueryTagsInputTableTypeV1Row(entry.Path, entry.VR, entry.PrivateCreator, (byte)((QueryTagLevel)Enum.Parse(typeof(QueryTagLevel), entry.Level, true)));
        }

        public override async Task DeleteExtendedQueryTagAsync(string tagPath, string vr, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                V2.DeleteExtendedQueryTag.PopulateCommand(sqlCommandWrapper, tagPath, (byte)ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[vr]);

                try
                {
                    await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.NotFound:
                            throw new ExtendedQueryTagNotFoundException(
                                string.Format(CultureInfo.InvariantCulture, DicomSqlServerResource.ExtendedQueryTagNotFound, tagPath));
                        case SqlErrorCodes.PreconditionFailed:
                            throw new ExtendedQueryTagBusyException(
                                string.Format(CultureInfo.InvariantCulture, DicomSqlServerResource.ExtendedQueryTagIsBusy, tagPath));
                        default:
                            throw new DataStoreException(ex);
                    }
                }
            }
        }

        protected async Task<List<ExtendedQueryTagStoreEntry>> GetAllExtendedQueryTagsAsync(CancellationToken cancellationToken = default)
        {
            List<ExtendedQueryTagStoreEntry> results = new List<ExtendedQueryTagStoreEntry>();

            using (SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                // V2 version allows NULL to get all tags
                V2.GetExtendedQueryTag.PopulateCommand(sqlCommandWrapper, null);

                var executionTimeWatch = Stopwatch.StartNew();
                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (int tagKey, string tagPath, string tagVR, string tagPrivateCreator, int tagLevel, int tagStatus) = reader.ReadRow(
                            V2.ExtendedQueryTag.TagKey,
                            V2.ExtendedQueryTag.TagPath,
                            V2.ExtendedQueryTag.TagVR,
                            V2.ExtendedQueryTag.TagPrivateCreator,
                            V2.ExtendedQueryTag.TagLevel,
                            V2.ExtendedQueryTag.TagStatus);

                        results.Add(new ExtendedQueryTagStoreEntry(tagKey, tagPath, tagVR, tagPrivateCreator, (QueryTagLevel)tagLevel, (ExtendedQueryTagStatus)tagStatus, QueryStatus.Enabled, 0));
                    }

                    executionTimeWatch.Stop();
                    Logger.LogInformation(executionTimeWatch.ElapsedMilliseconds.ToString());
                }
            }

            return results;
        }
    }
}
