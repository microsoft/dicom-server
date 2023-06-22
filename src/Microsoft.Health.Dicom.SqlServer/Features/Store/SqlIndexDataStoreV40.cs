// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

/// <summary>
/// Sql IndexDataStore version 6.
/// </summary>
internal class SqlIndexDataStoreV40 : SqlIndexDataStoreV37
{
    public SqlIndexDataStoreV40(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V40;


    public override async Task DeleteInstanceIndexAsync(int partitionKey, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
        EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));
        EnsureArg.IsNotNullOrEmpty(sopInstanceUid, nameof(sopInstanceUid));

        await DeleteInstanceAsync(partitionKey, studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, cancellationToken);
    }

    public override async Task DeleteSeriesIndexAsync(int partitionKey, string studyInstanceUid, string seriesInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));
        EnsureArg.IsNotNullOrEmpty(seriesInstanceUid, nameof(seriesInstanceUid));

        await DeleteInstanceAsync(partitionKey, studyInstanceUid, seriesInstanceUid, sopInstanceUid: null, cleanupAfter, cancellationToken);
    }

    public override async Task DeleteStudyIndexAsync(int partitionKey, string studyInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNullOrEmpty(studyInstanceUid, nameof(studyInstanceUid));

        await DeleteInstanceAsync(partitionKey, studyInstanceUid, seriesInstanceUid: null, sopInstanceUid: null, cleanupAfter, cancellationToken);
    }

    private async Task DeleteInstanceAsync(int partitionKey, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.DeleteInstanceV40.PopulateCommand(
                sqlCommandWrapper,
                cleanupAfter,
                (byte)IndexStatus.Created,
                partitionKey,
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid);

            try
            {
                await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                switch (ex.Number)
                {
                    case SqlErrorCodes.NotFound:
                        if (!string.IsNullOrEmpty(sopInstanceUid))
                        {
                            throw new InstanceNotFoundException();
                        }

                        if (!string.IsNullOrEmpty(seriesInstanceUid))
                        {
                            throw new SeriesNotFoundException();
                        }

                        throw new StudyNotFoundException();

                    default:
                        throw new DataStoreException(ex);
                }
            }
        }
    }


}
