// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Features.Workitem;

internal class SqlWorkitemStoreV21 : SqlWorkitemStoreV14
{
    public SqlWorkitemStoreV21(SqlConnectionWrapperFactory sqlConnectionWrapperFactory, ILogger<ISqlWorkitemStore> logger)
        : base(sqlConnectionWrapperFactory, logger)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V21;

    public override async Task UpdateWorkitemProcedureStepStateAsync(
        WorkitemMetadataStoreEntry workitemMetadata,
        long proposedWatermark,
        string procedureStepState,
        string transactionUid,
        CancellationToken cancellationToken = default)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.UpdateWorkitemProcedureStepStateV21.PopulateCommand(
                sqlCommandWrapper,
                workitemMetadata.WorkitemKey,
                DicomTag.ProcedureStepState.GetPath(),
                procedureStepState,
                workitemMetadata.Watermark,
                proposedWatermark,
                transactionUid);

            try
            {
                await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                throw new DataStoreException(ex);
            }
        }
    }
}
