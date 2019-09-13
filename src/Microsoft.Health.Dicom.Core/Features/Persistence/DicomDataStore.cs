// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
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

        public async Task DeleteStudyAsync(string studyInstanceUID, CancellationToken cancellationToken)
        {
            IEnumerable<DicomInstance> instances = await _dicomMetadataStore.GetInstancesInStudyAsync(studyInstanceUID, cancellationToken);
            await DeleteInstancesAsync(instances, cancellationToken);
        }

        public async Task DeleteSeriesAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken)
        {
            IEnumerable<DicomInstance> instances = await _dicomMetadataStore.GetInstancesInSeriesAsync(studyInstanceUID, seriesInstanceUID, cancellationToken);
            await DeleteInstancesAsync(instances, cancellationToken);
        }

        public async Task DeleteInstanceAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, CancellationToken cancellationToken)
        {
            var dicomInstance = new DicomInstance(studyInstanceUID, seriesInstanceUID, sopInstanceUID);
            await DeleteInstancesAsync(new[] { dicomInstance }, cancellationToken);
        }

        private async Task DeleteInstancesAsync(IEnumerable<DicomInstance> instances, CancellationToken cancellationToken)
        {
            foreach (IGrouping<DicomSeries, DicomInstance> grouping in instances.GroupBy(x => new DicomSeries(x.StudyInstanceUID, x.SeriesInstanceUID)))
            {
                using (ITransaction transaction = await _transactionService.BeginTransactionAsync(grouping.Key, grouping.ToArray(), cancellationToken))
                {
                    await transaction.Message.DeleteInstancesAsync(
                        _dicomBlobDataStore,
                        _dicomMetadataStore,
                        _dicomInstanceMetadataStore,
                        _dicomIndexDataStore,
                        cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                }
            }
        }
    }
}
