// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag.Error
{
    internal class SqlExtendedQueryTagErrorStoreV14 : SqlExtendedQueryTagErrorStoreV6
    {
        public SqlExtendedQueryTagErrorStoreV14(
           SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
           ILogger<SqlExtendedQueryTagErrorStoreV14> logger)
            : base(sqlConnectionWrapperFactory, logger)
        {
        }

        public override SchemaVersion Version => SchemaVersion.V14;

        public override async Task AddExtendedQueryTagErrorAsync(int tagKey, ValidationErrorCode errorCode, long watermark, CancellationToken cancellationToken = default)
        {
            EnsureArg.EnumIsDefined(errorCode, nameof(errorCode));

            using SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
            using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand();
            VLatest.AddExtendedQueryTagErrorV14.PopulateCommand(
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
    }
}
