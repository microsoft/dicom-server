// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Persistence.Store;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public class DicomDataStore : IDicomDataStore
    {
        private readonly ILogger<DicomDataStore> _logger;
        private readonly IDicomBlobDataStore _dicomBlobDataStore;
        private readonly IDicomMetadataStore _dicomMetadataStore;
        private readonly IDicomInstanceMetadataStore _dicomInstanceMetadataStore;
        private readonly IDicomIndexDataStore _dicomIndexDataStore;

        public DicomDataStore(
            IDicomBlobDataStore dicomBlobDataStore,
            IDicomMetadataStore dicomMetadataStore,
            IDicomInstanceMetadataStore dicomInstanceMetadataStore,
            IDicomIndexDataStore dicomIndexDataStore,
            ILogger<DicomDataStore> logger)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(dicomMetadataStore, nameof(dicomMetadataStore));
            EnsureArg.IsNotNull(dicomInstanceMetadataStore, nameof(dicomInstanceMetadataStore));

            _logger = logger;
            _dicomBlobDataStore = dicomBlobDataStore;
            _dicomMetadataStore = dicomMetadataStore;
            _dicomInstanceMetadataStore = dicomInstanceMetadataStore;
            _dicomIndexDataStore = dicomIndexDataStore;
        }

        public StoreTransaction BeginStoreTransaction()
        {
            _logger.LogDebug("Starting a new store transaction.");
            return new StoreTransaction(_dicomBlobDataStore, _dicomMetadataStore, _dicomInstanceMetadataStore, _dicomIndexDataStore);
        }

        public async Task<Stream> GetDicomDataStreamAsync(DicomInstance dicomInstance, CancellationToken cancellationToken = default)
        {
            var storageName = StoreTransaction.GetBlobStorageName(dicomInstance);
            return await _dicomBlobDataStore.GetFileAsStreamAsync(storageName, cancellationToken);
        }

        public async Task DeleteStudyAsync(string studyInstanceUID, CancellationToken cancellationToken)
        {
            IEnumerable<DicomInstance> deletedInstances = await _dicomIndexDataStore.DeleteStudyIndexAsync(studyInstanceUID, cancellationToken);
            await _dicomMetadataStore.DeleteStudyAsync(studyInstanceUID, cancellationToken);

            await DeleteInstanceMetadataAndBlobsAsync(deletedInstances, cancellationToken);
        }

        public async Task DeleteSeriesAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken)
        {
            IEnumerable<DicomInstance> deletedInstances = await _dicomIndexDataStore.DeleteSeriesIndexAsync(studyInstanceUID, seriesInstanceUID, cancellationToken);
            await _dicomMetadataStore.DeleteSeriesAsync(studyInstanceUID, seriesInstanceUID, cancellationToken);

            await DeleteInstanceMetadataAndBlobsAsync(deletedInstances, cancellationToken);
        }

        public async Task DeleteInstanceAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, CancellationToken cancellationToken)
        {
            var dicomInstance = new List<DicomInstance> { new DicomInstance(studyInstanceUID, seriesInstanceUID, sopInstanceUID) };
            await _dicomIndexDataStore.DeleteInstanceIndexAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, cancellationToken);
            await _dicomMetadataStore.DeleteInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, cancellationToken);

            await DeleteInstanceMetadataAndBlobsAsync(dicomInstance, cancellationToken);
        }

        public async Task DeleteInstanceMetadataAndBlobsAsync(IEnumerable<DicomInstance> instances, CancellationToken cancellationToken = default)
        {
            await Task.WhenAll(instances.Select(async x =>
            {
                await _dicomInstanceMetadataStore.DeleteInstanceMetadataAsync(x, cancellationToken);
                await _dicomBlobDataStore.DeleteFileIfExistsAsync(StoreTransaction.GetBlobStorageName(x), cancellationToken);
            }));
        }
    }
}
