// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.BulkImport;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.BulkImport
{
    public class SqlBulkImportDataStore : IBulkImportDataStore
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;
        private readonly ILogger _logger;

        public SqlBulkImportDataStore(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
            ILogger<SqlBulkImportDataStore> logger)
        {
            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
            _logger = logger;
        }

        public async Task EnableBulkImportSourceAsync(string accountName, CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapper())
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.EnableBulkImportSource.PopulateCommand(sqlCommandWrapper, accountName, (byte)BulkImportSourceStatus.Created);

                await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public async Task UpdateBulkImportSourceAsync(string accountName, BulkImportSourceStatus status, CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapper())
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.UpdateBulkImportSourceStatus.PopulateCommand(sqlCommandWrapper, accountName, (byte)status);

                await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public async Task QueueBulkImportEntriesAsync(string accountName, IReadOnlyList<BlobReference> blobReferences, CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapper())
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.AddBulkImportEntries.PopulateCommand(
                    sqlCommandWrapper,
                    accountName,
                    (byte)BulkImportEntryStatus.Queued,
                    new VLatest.AddBulkImportEntriesTableValuedParameters(
                        blobReferences.Select(reference => new VLatest.BulkImportEntryTableTypeRow(reference.ContainerName, reference.BlobName))));

                await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public async Task<IReadOnlyList<BulkImportEntry>> RetrieveBulkImportEntriesAsync(CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapper())
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.GetBulkImportEntries.PopulateCommand(
                    sqlCommandWrapper,
                    (byte)BulkImportSourceStatus.Initialized,
                    (byte)BulkImportEntryStatus.Queued,
                    10);

                using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken);

                var entries = new List<BulkImportEntry>();

                while (await reader.ReadAsync())
                {
                    (long sequence, string accountName, DateTimeOffset timestamp, string containerName, string blobName) = reader.ReadRow(
                        VLatest.BulkImportEntry.Sequence,
                        VLatest.BulkImportSource.AccountName,
                        VLatest.BulkImportEntry.Timestamp,
                        VLatest.BulkImportEntry.ContainerName,
                        VLatest.BulkImportEntry.BlobName);

                    entries.Add(new BulkImportEntry(sequence, accountName, timestamp, containerName, blobName));
                }

                return entries;
            }
        }

        public async Task UpdateBulkImportEntriesAsync(IEnumerable<(long Sequence, BulkImportEntryStatus Status)> entries, CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapper())
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.UpdateBulkImportEntries.PopulateCommand(
                    sqlCommandWrapper,
                    new VLatest.UpdateBulkImportEntriesTableValuedParameters(entries.Select(entry => new VLatest.UpdateBulkImportEntryTableTypeRow(entry.Sequence, (byte)entry.Status))));

                await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }
}
