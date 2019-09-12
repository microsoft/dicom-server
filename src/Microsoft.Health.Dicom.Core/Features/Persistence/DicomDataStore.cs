// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Persistence.Store;
using Microsoft.Health.Dicom.Core.Features.Transaction;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public class DicomDataStore
    {
        private readonly ILogger<DicomDataStore> _logger;
        private readonly IDicomTransactionService _transactionService;
        private readonly IDicomBlobDataStore _dicomBlobDataStore;
        private readonly IDicomMetadataStore _dicomMetadataStore;
        private readonly IDicomInstanceMetadataStore _dicomInstanceMetadataStore;
        private readonly IDicomIndexDataStore _dicomIndexDataStore;

        public DicomDataStore(
            ILogger<DicomDataStore> logger,
            IDicomTransactionService transactionService,
            IDicomBlobDataStore dicomBlobDataStore,
            IDicomMetadataStore dicomMetadataStore,
            IDicomInstanceMetadataStore dicomInstanceMetadataStore,
            IDicomIndexDataStore dicomIndexDataStore)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(transactionService, nameof(transactionService));
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(dicomMetadataStore, nameof(dicomMetadataStore));
            EnsureArg.IsNotNull(dicomInstanceMetadataStore, nameof(dicomInstanceMetadataStore));

            _logger = logger;
            _transactionService = transactionService;
            _dicomBlobDataStore = dicomBlobDataStore;
            _dicomMetadataStore = dicomMetadataStore;
            _dicomInstanceMetadataStore = dicomInstanceMetadataStore;
            _dicomIndexDataStore = dicomIndexDataStore;
        }

        public StoreTransaction BeginStoreTransaction()
        {
            _logger.LogDebug("Starting a new store transaction.");
            return new StoreTransaction(_transactionService, _dicomBlobDataStore, _dicomMetadataStore, _dicomInstanceMetadataStore, _dicomIndexDataStore);
        }

        public async Task<Stream> GetDicomDataStreamAsync(DicomInstance dicomInstance, CancellationToken cancellationToken = default)
        {
            return await _dicomBlobDataStore.GetInstanceAsStreamAsync(dicomInstance, cancellationToken);
        }

        public async Task DeleteStudyAsync(string studyInstanceUID, CancellationToken cancellationToken)
        {
            DicomInstance[] deletedInstances = (await _dicomIndexDataStore.DeleteStudyIndexAsync(studyInstanceUID, cancellationToken)).ToArray();
            await _dicomMetadataStore.DeleteStudyAsync(studyInstanceUID);

            await DeleteInstanceMetadataAndBlobsAsync(deletedInstances);
        }

        public async Task DeleteSeriesAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken)
        {
            DicomInstance[] deletedInstances = (await _dicomIndexDataStore.DeleteSeriesIndexAsync(studyInstanceUID, seriesInstanceUID, cancellationToken)).ToArray();
            await _dicomMetadataStore.DeleteSeriesAsync(studyInstanceUID, seriesInstanceUID);

            await DeleteInstanceMetadataAndBlobsAsync(deletedInstances);
        }

        public async Task DeleteInstanceAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, CancellationToken cancellationToken)
        {
            var dicomInstance = new DicomInstance(studyInstanceUID, seriesInstanceUID, sopInstanceUID);
            await _dicomIndexDataStore.DeleteInstanceIndexAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, cancellationToken);
            await _dicomMetadataStore.DeleteInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID);

            await DeleteInstanceMetadataAndBlobsAsync(dicomInstance);
        }

        public async Task DeleteInstanceMetadataAndBlobsAsync(params DicomInstance[] instances)
        {
            await Task.WhenAll(instances.Select(async x =>
            {
                await _dicomInstanceMetadataStore.DeleteInstanceMetadataAsync(x);
                await _dicomBlobDataStore.DeleteInstanceIfExistsAsync(x);
            }));
        }
    }
}
