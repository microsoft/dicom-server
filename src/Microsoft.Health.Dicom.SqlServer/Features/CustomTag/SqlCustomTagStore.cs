// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.ChangeFeed
{
    public class SqlCustomTagStore : ICustomTagStore
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;
        private readonly ILogger<SqlCustomTagStore> _logger;

        public SqlCustomTagStore(
           SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
           ILogger<SqlCustomTagStore> logger)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
            _logger = logger;
        }

        public async Task<long> AddCustomTagAsync(string path, string vr, CustomTagLevel level, CustomTagStatus status, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                V2.AddCustomTag.PopulateCommand(sqlCommandWrapper, path, vr, (byte)level, (byte)status);

                try
                {
                    return (long)await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.Conflict:
                            throw new CustomTagAlreadyExistsException();

                        default:
                            throw new DataStoreException(ex);
                    }
                }
            }
        }

        public async Task DeleteCustomTagAsync(long key, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                V2.DeleteCustomTag.PopulateCommand(sqlCommandWrapper, key);

                try
                {
                    await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    switch (ex.ErrorCode)
                    {
                        case SqlErrorCodes.NotFound:
                            throw new CustomTagNotFoundException();

                        default:
                            throw new DataStoreException(ex);
                    }
                }
            }
        }

        public async Task<IEnumerable<VersionedInstanceIdentifier>> GetInstancesInThePastAsync(long maxWatermark, int top, IndexStatus indexStatus = IndexStatus.Created, CancellationToken cancellationToken = default)
        {
            List<VersionedInstanceIdentifier> results = new List<VersionedInstanceIdentifier>();
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                V2.GetInstancesInThePast.PopulateCommand(sqlCommandWrapper, maxWatermark, top, (byte)indexStatus);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (string rStudyInstanceUid, string rSeriesInstanceUid, string rSopInstanceUid, long rWatermark) = reader.ReadRow(
                               VLatest.Instance.StudyInstanceUid,
                               VLatest.Instance.SeriesInstanceUid,
                               VLatest.Instance.SopInstanceUid,
                               VLatest.Instance.Watermark);

                        results.Add(new VersionedInstanceIdentifier(
                               rStudyInstanceUid,
                               rSeriesInstanceUid,
                               rSopInstanceUid,
                               rWatermark));
                    }
                }
            }

            return results;
        }

        public async Task<long?> GetLatestInstanceAsync(CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                V2.GetLatestInstance.PopulateCommand(sqlCommandWrapper);
                return (long?)await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
            }
        }

        public async Task UpdateCustomTagStatusAsync(long key, CustomTagStatus status, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                V2.UpdateCustomTagStatus.PopulateCommand(sqlCommandWrapper, key, (byte)status);
                try
                {
                    await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.NotFound:
                            throw new CustomTagNotFoundException();
                        default:
                            break;
                    }
                }
            }
        }
    }
}
