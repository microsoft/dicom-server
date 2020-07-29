// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.BulkImport
{
    public class BulkImportSourceService : IBulkImportSourceService
    {
        private readonly IBulkImportDataStore _bulkImportDataStore;
        private readonly IBulkImportService _bulkImportService;

        public BulkImportSourceService(
            IBulkImportDataStore bulkImportDataStore,
            IBulkImportService bulkImportService)
        {
            EnsureArg.IsNotNull(bulkImportDataStore, nameof(bulkImportDataStore));

            _bulkImportDataStore = bulkImportDataStore;
            _bulkImportService = bulkImportService;
        }

        public async Task EnableBulkImportSourceAsync(string accountName, CancellationToken cancellationToken)
        {
            await _bulkImportDataStore.EnableBulkImportSourceAsync(accountName, cancellationToken);
            await _bulkImportService.RetrieveInitialBlobsAsync(accountName, cancellationToken);
            await _bulkImportDataStore.UpdateBulkImportSourceAsync(accountName, BulkImportSourceStatus.Initialized, cancellationToken);
        }
    }
}
