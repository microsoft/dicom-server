// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    /// <summary>
    /// Sql IndexDataStore version 6.
    /// </summary>
    internal class SqlIndexDataStoreV6 : SqlIndexDataStoreV5
    {
        public SqlIndexDataStoreV6(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
            : base(sqlConnectionWrapperFactory)
        {
        }

        public override SchemaVersion Version => SchemaVersion.V6;

        public override async Task EndCreateInstanceIndexAsync(
            DicomDataset dicomDataset,
            long watermark,
            IEnumerable<QueryTag> queryTags,
            bool allowExpiredTags = false,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {

                VLatest.UpdateInstanceStatus.PopulateCommand(
                    sqlCommandWrapper,
                    dicomDataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                    dicomDataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                    dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
                    watermark,
                    (byte)IndexStatus.Created,
                    allowExpiredTags ? null : ExtendedQueryTagDataRowsBuilder.GetMaxTagKey(queryTags));

                try
                {
                    await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    throw ex.Number switch
                    {
                        SqlErrorCodes.NotFound => new InstanceNotFoundException(),
                        SqlErrorCodes.Conflict when ex.State == 10 => new ExtendedQueryTagsOutOfDateException(),
                        _ => new DataStoreException(ex),
                    };
                }
            }
        }
    }
}
