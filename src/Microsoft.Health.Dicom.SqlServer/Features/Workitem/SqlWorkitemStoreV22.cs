// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Features.Workitem;
internal class SqlWorkitemStoreV22 : SqlWorkitemStoreV21
{
    public SqlWorkitemStoreV22(SqlConnectionWrapperFactory sqlConnectionWrapperFactory, ILogger<ISqlWorkitemStore> logger)
        : base(sqlConnectionWrapperFactory, logger)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V22;

    public override async Task UpdateWorkitemTransactionAsync(
        WorkitemMetadataStoreEntry workitemMetadata,
        long proposedWatermark,
        DicomDataset dataset,
        IEnumerable<QueryTag> queryTags,
        CancellationToken cancellationToken = default)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            var rows = ExtendedQueryTagDataRowsBuilder.Build(dataset, queryTags, Version);
            var parameters = new VLatest.UpdateWorkitemTransactionTableValuedParameters(
                rows.StringRows,
                rows.DateTimeWithUtcRows,
                rows.PersonNameRows
            );

            string workitemUid = workitemMetadata.WorkitemUid;

            VLatest.UpdateWorkitemTransaction.PopulateCommand(
                sqlCommandWrapper,
                workitemMetadata.WorkitemKey,
                workitemMetadata.PartitionKey,
                workitemMetadata.Watermark,
                proposedWatermark,
                parameters);

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
