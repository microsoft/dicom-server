// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Transactional.Features.Storage.Models;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Transactional.Features.Storage
{
    public class DicomTransactionResolver
    {
        private readonly CloudBlobContainer _container;
        private readonly IDicomBlobDataStore _dicomBlobDataStore;
        private readonly IDicomMetadataStore _dicomMetadataStore;
        private readonly IDicomInstanceMetadataStore _dicomInstanceMetadataStore;
        private readonly IDicomIndexDataStore _dicomIndexDataStore;
        private readonly ILogger<DicomTransactionService> _logger;
        private readonly JsonSerializer _jsonSerializer;
        private const int MinimumBlobAgeSeconds = 15;

        public DicomTransactionResolver(
            CloudBlobContainer container,
            IDicomIndexDataStore dicomIndexDataStore,
            IDicomMetadataStore dicomMetadataStore,
            IDicomInstanceMetadataStore dicomInstanceMetadataStore,
            IDicomBlobDataStore dicomBlobDataStore,
            ILogger<DicomTransactionService> logger)
        {
            EnsureArg.IsNotNull(container, nameof(container));
            EnsureArg.IsNotNull(dicomIndexDataStore, nameof(dicomIndexDataStore));
            EnsureArg.IsNotNull(dicomMetadataStore, nameof(dicomMetadataStore));
            EnsureArg.IsNotNull(dicomInstanceMetadataStore, nameof(dicomInstanceMetadataStore));
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _container = container;
            _dicomIndexDataStore = dicomIndexDataStore;
            _dicomMetadataStore = dicomMetadataStore;
            _dicomInstanceMetadataStore = dicomInstanceMetadataStore;
            _dicomBlobDataStore = dicomBlobDataStore;
            _logger = logger;
            _jsonSerializer = new JsonSerializer();
        }

        public async Task CleanUpTransactions(CancellationToken cancellationToken)
        {
            var blobContinuationToken = new BlobContinuationToken();

            CloudBlockBlob[] failedTransactions = null;

            do
            {
                BlobResultSegment resultSegment = await _container.ListBlobsSegmentedAsync(blobContinuationToken, cancellationToken);
                failedTransactions = resultSegment.Results.OfType<CloudBlockBlob>()
                                                .Where(x => DateTime.UtcNow.AddSeconds(MinimumBlobAgeSeconds) > x.Properties.LastModified &&
                                                                x.Properties.LeaseState == LeaseState.Available)
                                                .ToArray();

                // Fetch all blobs older than 15 seconds and have a lease state of available.
                foreach (CloudBlockBlob cloudBlockBlob in failedTransactions)
                {
                    await CleanUpTransaction(cloudBlockBlob, cancellationToken);
                }
            }
            while (failedTransactions.Length > 0);
        }

        private async Task CleanUpTransaction(CloudBlockBlob cloudBlockBlob, CancellationToken cancellationToken)
        {
            try
            {
                TransactionMessage transactionMessage = await ReadBlobTransactionAsync(cloudBlockBlob, cancellationToken);

                using (var transaction = new DicomTransaction(cloudBlockBlob, TimeSpan.FromSeconds(15), _logger))
                {
                    try
                    {
                        await transaction.BeginAsync(transactionMessage, cancellationToken);
                        await ResolveTransactionAsync(transactionMessage, cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch
                    {
                        await transaction.AbortAsync(cancellationToken);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when trying to clean up failed transaction: {cloudBlockBlob.Name}. {e}");
            }
        }

        private async Task<TransactionMessage> ReadBlobTransactionAsync(CloudBlockBlob cloudBlockBlob, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(cloudBlockBlob, nameof(cloudBlockBlob));

            using (Stream stream = await cloudBlockBlob.OpenReadAsync(cancellationToken))
            using (var streamReader = new StreamReader(stream, TransactionMessage.MessageEncoding))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                return _jsonSerializer.Deserialize<TransactionMessage>(jsonTextReader);
            }
        }

        private async Task ResolveTransactionAsync(TransactionMessage transactionMessage, CancellationToken cancellationToken)
        {
            // Group the instances by series UID.
            foreach (IGrouping<string, DicomInstance> grouping in transactionMessage.DicomInstances.GroupBy(x => x.StudyInstanceUID + x.SeriesInstanceUID))
            {
                DicomInstance[] instances = grouping.ToArray();

                // Attempt to delete the instance indexes and instance metadata.
                await Task.WhenAll(
                    _dicomIndexDataStore.DeleteInstancesIndexAsync(throwOnNotFound: false, cancellationToken, instances),
                    _dicomMetadataStore.DeleteInstanceAsync(throwOnNotFound: false, cancellationToken, instances));

                await Task.WhenAll(instances.Select(async x =>
                {
                    await _dicomInstanceMetadataStore.DeleteInstanceMetadataAsync(x, cancellationToken);
                    await _dicomBlobDataStore.DeleteInstanceIfExistsAsync(x, cancellationToken);
                }));
            }
        }
    }
}
