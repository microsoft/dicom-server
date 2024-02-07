// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

internal class SqlIndexDataStoreV57 : SqlIndexDataStoreV55
{
    protected static readonly Health.SqlServer.Features.Schema.Model.BigIntColumn TotalIndexedFileCount = new("TotalIndexedFileCount");
    protected static readonly Health.SqlServer.Features.Schema.Model.NullableBigIntColumn TotalIndexedBytes = new("TotalIndexedBytes");

    public SqlIndexDataStoreV57(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V57;

    public override async Task<IndexedFileProperties> GetIndexedFileMetricsAsync(CancellationToken cancellationToken = default)
    {
        using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken, true);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();
        VLatest.GetIndexedFileMetrics.PopulateCommand(sqlCommandWrapper);

        try
        {
            using SqlDataReader sqlDataReader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken);

            await sqlDataReader.ReadAsync(cancellationToken);

            (long count, long? sum) = sqlDataReader.ReadRow(TotalIndexedFileCount, TotalIndexedBytes);

            return new IndexedFileProperties
            {
                TotalIndexed = count,
                TotalSum = sum ?? 0,
            };
        }
        catch (SqlException ex)
        {
            throw new DataStoreException(ex);
        }
    }
}
