// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.CustomTag
{
    public class SqlCustomTagJobStore : ICustomTagJobStore
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;
        private readonly ILogger<SqlCustomTagJobStore> _logger;

        public SqlCustomTagJobStore(
           SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
           ILogger<SqlCustomTagJobStore> logger)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<CustomTagJob>> AcquireCustomTagJobsAsync(int maxCount, CancellationToken cancellationToken = default)
        {
            var results = new List<CustomTagJob>();

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.AcquireCustomTagJobs.PopulateCommand(sqlCommandWrapper, maxCount);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (long rKey, int rType, long? rCompletedWatermark, long rMaxWatermark, DateTime? rHeartBeatTimeStamp, int rStatus) = reader.ReadRow(
                            VLatest.Job.Key,
                            VLatest.Job.Type,
                            VLatest.Job.CompletedWatermark,
                            VLatest.Job.MaxWatermark,
                            VLatest.Job.HeartBeatTimeStamp,
                            VLatest.Job.Status);

                        results.Add(new CustomTagJob(
                                rKey,
                                (CustomTagJobType)rType,
                                rCompletedWatermark,
                                rMaxWatermark,
                                rHeartBeatTimeStamp,
                                (CustomTagJobStatus)rStatus));
                    }
                }
            }

            return results;
        }

        public async Task<CustomTagJob> GetCustomTagJobAsync(long jobKey, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return null;
        }

        public async Task<IEnumerable<CustomTagStoreEntry>> GetCustomTagsOnJobAsync(int jobKey, CancellationToken cancellationToken)
        {
            var results = new List<CustomTagStoreEntry>();

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.GetCustomTagsOnJob.PopulateCommand(sqlCommandWrapper, jobKey);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (long rKey, string rPath, string rVR, byte rLevel, byte rStatus) = reader.ReadRow(
                            VLatest.CustomTag.Key,
                            VLatest.CustomTag.Path,
                            VLatest.CustomTag.VR,
                            VLatest.CustomTag.Level,
                            VLatest.CustomTag.Status);

                        results.Add(new CustomTagStoreEntry(
                                rKey,
                                rPath,
                                rVR,
                                (CustomTagLevel)rLevel,
                                (CustomTagStatus)rStatus));
                    }
                }
            }

            return results;
        }

        public Task<IEnumerable<CustomTagStoreEntry>> GetCustomTagsOnJobAsync(long jobKey, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
