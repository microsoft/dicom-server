// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Validation;
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
            ConnectionWrapperFactory = EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            Logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public override SchemaVersion Version => SchemaVersion.V4;

        protected SqlConnectionWrapperFactory ConnectionWrapperFactory { get; }

        protected ILogger Logger { get; }

        public override async Task AddExtendedQueryTagErrorAsync(
            int tagKey,
            ValidationErrorCode errorCode,
            long watermark,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.EnumIsDefined(errorCode, nameof(errorCode));

            using SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
            using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand();
            VLatest.AddExtendedQueryTagError.PopulateCommand(
                sqlCommandWrapper,
                tagKey,
                (short)errorCode,
                watermark);

            try
            {
                await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException e)
            {
                if (e.Number == SqlErrorCodes.NotFound)
                {
                    throw new ExtendedQueryTagNotFoundException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            DicomSqlServerResource.ExtendedQueryTagNotFoundWhenAddingError,
                            tagKey));
                }

                throw new DataStoreException(e);
            }
        }

        public override async Task<IReadOnlyList<ExtendedQueryTagError>> GetExtendedQueryTagErrorsAsync(string tagPath, int limit, int offset, CancellationToken cancellationToken = default)
        {
            List<ExtendedQueryTagError> results = new List<ExtendedQueryTagError>();

            using SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
            using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand();

            VLatest.GetExtendedQueryTagErrors.PopulateCommand(sqlCommandWrapper, tagPath);

            try
            {
                using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    (int tagkey, short errorCode, DateTime createdTime, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid) = reader.ReadRow(
                        VLatest.ExtendedQueryTagError.TagKey,
                        VLatest.ExtendedQueryTagError.ErrorCode,
                        VLatest.ExtendedQueryTagError.CreatedTime,
                        VLatest.Instance.StudyInstanceUid,
                        VLatest.Instance.SeriesInstanceUid,
                        VLatest.Instance.SopInstanceUid);

                    results.Add(new ExtendedQueryTagError(createdTime, studyInstanceUid, seriesInstanceUid, sopInstanceUid, ((ValidationErrorCode)errorCode).GetMessage()));
                }
            }
            catch (SqlException e)
            {
                if (e.Number == SqlErrorCodes.NotFound)
                {
                    throw new ExtendedQueryTagNotFoundException(
                        string.Format(CultureInfo.InvariantCulture, DicomSqlServerResource.ExtendedQueryTagNotFound, tagPath));
                }

                throw new DataStoreException(e);
            }

            return results;
        }
    }
}
