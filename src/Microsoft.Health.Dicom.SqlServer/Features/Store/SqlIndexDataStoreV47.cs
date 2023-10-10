// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.ApplicationInsights;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Diagnostic;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

internal class SqlIndexDataStoreV47 : SqlIndexDataStoreV46
{
    private readonly ILogger<SqlIndexDataStoreV47> _logger;
    private readonly TelemetryClient _telemetryClient;

    public SqlIndexDataStoreV47(
        SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
        ILogger<SqlIndexDataStoreV47> logger,
        TelemetryClient telemetryClient
        )
        : base(sqlConnectionWrapperFactory)
    {
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _telemetryClient = EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
    }

    public override SchemaVersion Version => SchemaVersion.V47;

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

    private async Task DeleteInstanceAsync(int partitionKey, string studyInstanceUid, string seriesInstanceUid,
        string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.DeleteInstanceV47.PopulateCommand(
                sqlCommandWrapper,
                cleanupAfter,
                (byte)IndexStatus.Created,
                partitionKey,
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid);

            try
            {
                using (var reader =
                       await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (long rWatermark, int rPartitionKey, string rStudyInstanceUid, string rSeriesInstanceUid,
                            string rSopInstanceUid) = reader.ReadRow(
                            VLatest.DeletedInstance.Watermark,
                            VLatest.DeletedInstance.PartitionKey,
                            VLatest.DeletedInstance.StudyInstanceUid,
                            VLatest.DeletedInstance.SeriesInstanceUid,
                            VLatest.DeletedInstance.SopInstanceUid);
                        _logger.LogInformation(
                            "Instance queued for deletion. Instance Watermark: {Watermark} , PartitionKey: {PartitionKey}",
                            rWatermark, rPartitionKey);
                        _telemetryClient.ForwardLogTrace("Instance queued for deletion", rStudyInstanceUid, rSeriesInstanceUid, rSopInstanceUid);
                    }
                }
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
