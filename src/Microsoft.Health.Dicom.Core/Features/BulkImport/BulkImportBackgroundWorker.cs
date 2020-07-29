// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Features.BulkImport
{
    public class BulkImportBackgroundWorker
    {
        private readonly IBulkImportService _bulkImportService;
        private readonly IBulkImportDataStore _bulkImportDataStore;
        private readonly IStoreService _storeService;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private readonly ILogger _logger;

        private readonly AzureServiceTokenProvider _azureServiceTokenProvider = new AzureServiceTokenProvider();

        public BulkImportBackgroundWorker(
            IBulkImportService bulkImportService,
            IBulkImportDataStore bulkImportDataStore,
            IStoreService storeSerivce,
            RecyclableMemoryStreamManager recyclableMemoryStreamManager,
            ILogger<BulkImportBackgroundWorker> logger)
        {
            _bulkImportService = bulkImportService;
            _bulkImportDataStore = bulkImportDataStore;
            _storeService = storeSerivce;
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                IReadOnlyList<BulkImportEntry> entries = await _bulkImportDataStore.RetrieveBulkImportEntriesAsync(cancellationToken);

                if (entries?.Any() == true)
                {
                    var results = new List<(long, BulkImportEntryStatus)>();

                    IEnumerable<IGrouping<string, BulkImportEntry>> entriesByAccounts = entries.GroupBy(entry => entry.AccountName);

                    foreach (IGrouping<string, BulkImportEntry> entriesByAccount in entriesByAccounts)
                    {
                        var storageAccountUri = new Uri($"https://{entriesByAccount.Key}.blob.core.windows.net");

                        string accessToken = await _azureServiceTokenProvider.GetAccessTokenAsync(storageAccountUri.ToString());

                        var client = new CloudBlobClient(storageAccountUri, new StorageCredentials(new TokenCredential(accessToken)));

                        foreach (IGrouping<string, BulkImportEntry> entryByContainer in entriesByAccount.GroupBy(entry => entry.ContainerName))
                        {
                            CloudBlobContainer containerReference = client.GetContainerReference(entryByContainer.Key);

                            foreach (BulkImportEntry entry in entryByContainer)
                            {
                                CloudBlob blobReference = containerReference.GetBlobReference(entry.BlobName);

                                using (Stream stream = _recyclableMemoryStreamManager.GetStream())
                                {
                                    await blobReference.DownloadToStreamAsync(stream, cancellationToken);

                                    stream.Seek(0, SeekOrigin.Begin);

                                    try
                                    {
                                        await _storeService.ProcessAsync(new StreamOriginatedDicomInstanceEntry(stream), cancellationToken);

                                        results.Add((entry.Sequence, BulkImportEntryStatus.Imported));
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to process instance.");

                                        results.Add((entry.Sequence, BulkImportEntryStatus.Failed));
                                    }
                                }
                            }
                        }
                    }

                    await _bulkImportDataStore.UpdateBulkImportEntriesAsync(results, cancellationToken);
                }
                else
                {
                    await Task.Delay(5000, cancellationToken);
                }
            }
        }
    }
}
