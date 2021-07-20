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
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag.Error
{
    internal class SqlExtendedQueryTagErrorStoreV4 : SqlExtendedQueryTagErrorStoreV3
    {
        public SqlExtendedQueryTagErrorStoreV4(
           SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
           ILogger<SqlExtendedQueryTagErrorStoreV4> logger)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            ConnectionWrapperFactory = sqlConnectionWrapperFactory;
            Logger = logger;
        }

        public override SchemaVersion Version => SchemaVersion.V4;

        protected SqlConnectionWrapperFactory ConnectionWrapperFactory { get; }

        protected ILogger Logger { get; }

        public override async Task<int> AddExtendedQueryTagErrorAsync(
            int tagKey,
            int errorCode,
            long watermark,
            DateTime createdTime,
            CancellationToken cancellationToken = default)
        {
            using SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
            using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand();
            VLatest.AddExtendedQueryTagError.PopulateCommand(
                sqlCommandWrapper,
                tagKey,
                errorCode,
                watermark,
                createdTime);
            try
            {
                return (int)await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
            }
            catch (SqlException e)
            {
                switch (e.Number)
                {
                    case SqlErrorCodes.Conflict:
                        throw new ExtendedQueryTagErrorAlreadyExistsException();
                    case SqlErrorCodes.NotFound:
                        throw new ExtendedQueryTagNotFoundException("Attempted to add error on non existing query tag.");
                }

                throw new DataStoreException(e);
            }
        }

        public override async Task<IReadOnlyList<ExtendedQueryTagError>> GetExtendedQueryTagErrorsAsync(string tagPath, CancellationToken cancellationToken = default)
        {
            List<ExtendedQueryTagError> results = new List<ExtendedQueryTagError>();

            using SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
            using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand();

            VLatest.GetExtendedQueryTagErrors.PopulateCommand(sqlCommandWrapper, tagPath);

            using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                (int tagkey, int errorCode, DateTime createdTime, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid) = reader.ReadRow(
                    VLatest.ExtendedQueryTagError.TagKey,
                    VLatest.ExtendedQueryTagError.ErrorCode,
                    VLatest.ExtendedQueryTagError.CreatedTime,
                    VLatest.Instance.StudyInstanceUid,
                    VLatest.Instance.SeriesInstanceUid,
                    VLatest.Instance.SopInstanceUid);

                //TODO: build the error message here
                string errorMessage = "error" + errorCode;

                results.Add(new ExtendedQueryTagError(createdTime, studyInstanceUid, seriesInstanceUid, sopInstanceUid, errorMessage));
            }

            return results;
        }

        public override async Task<bool> DeleteExtendedQueryTagErrorsAsync(string tagPath, CancellationToken cancellationToken = default)
        {
            using SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
            using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand();

            VLatest.DeleteExtendedQueryTagErrors.PopulateCommand(sqlCommandWrapper, tagPath);

            return await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken) != 0;
        }
    }
}
