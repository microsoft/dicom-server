// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

internal class SqlIndexDataStoreV55 : SqlIndexDataStoreV54
{
    public SqlIndexDataStoreV55(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V55;

    public override async Task UpdateFilePropertiesContentLengthAsync(
        IReadOnlyDictionary<long, FileProperties> filePropertiesByWatermark,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<FilePropertyTableTypeV2Row> fpRows = filePropertiesByWatermark
            .Select(fp =>
                new FilePropertyTableTypeV2Row(
                    fp.Key,
                    fp.Value.Path,
                    fp.Value.ETag,
                    fp.Value.ContentLength))
            .ToList();

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.UpdateFilePropertiesContentLength.PopulateCommand(sqlCommandWrapper, fpRows);

            try
            {
                await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                throw new DataStoreException(ex);
            }
        }
    }
}
