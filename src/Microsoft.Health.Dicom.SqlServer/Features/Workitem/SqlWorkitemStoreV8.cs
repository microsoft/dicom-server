// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Workitem
{
    internal class SqlWorkitemStoreV8 : ISqlWorkitemStore
    {

        protected SqlConnectionWrapperFactory SqlConnectionWrapperFactory;

        public SqlWorkitemStoreV8(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        {
            SqlConnectionWrapperFactory = EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
        }

        public virtual SchemaVersion Version => SchemaVersion.V8;

        public virtual async Task<long> AddWorkitemAsync(int partitionKey, string workitemUid, CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                var parameters = new VLatest.AddWorkitemTableValuedParameters(
                    new List<InsertStringExtendedQueryTagTableTypeV1Row>(),
                    new List<InsertDateTimeExtendedQueryTagTableTypeV2Row>(),
                    new List<InsertPersonNameExtendedQueryTagTableTypeV1Row>()
                );

                VLatest.AddWorkitem.PopulateCommand(
                    sqlCommandWrapper,
                    partitionKey,
                    workitemUid,
                    parameters);

                try
                {
                    return (long)await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    if (ex.Number == SqlErrorCodes.Conflict)
                    {
                        throw new InstanceAlreadyExistsException();
                    }

                    throw new DataStoreException(ex);
                }

            }
        }
    }
}
