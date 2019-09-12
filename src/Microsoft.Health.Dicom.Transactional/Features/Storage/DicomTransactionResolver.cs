// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Transaction;
using Microsoft.Health.Dicom.Transactional.Features.Storage.Models;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Transactional.Features.Storage
{
    internal class DicomTransactionResolver : ITransactionResolver
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
            CloudBlobClient client,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            IDicomIndexDataStore dicomIndexDataStore,
            IDicomMetadataStore dicomMetadataStore,
            IDicomInstanceMetadataStore dicomInstanceMetadataStore,
            IDicomBlobDataStore dicomBlobDataStore,
            ILogger<DicomTransactionService> logger)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
            EnsureArg.IsNotNull(dicomIndexDataStore, nameof(dicomIndexDataStore));
            EnsureArg.IsNotNull(dicomMetadataStore, nameof(dicomMetadataStore));
            EnsureArg.IsNotNull(dicomInstanceMetadataStore, nameof(dicomInstanceMetadataStore));
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationName);

            _container = client.GetContainerReference(containerConfiguration.ContainerName);
            _dicomIndexDataStore = dicomIndexDataStore;
            _dicomMetadataStore = dicomMetadataStore;
            _dicomInstanceMetadataStore = dicomInstanceMetadataStore;
            _dicomBlobDataStore = dicomBlobDataStore;
            _logger = logger;
            _jsonSerializer = new JsonSerializer();
        }

        public async Task ResolveTransactionAsync(ICloudBlob cloudBlob, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(cloudBlob, nameof(cloudBlob));

            TransactionMessage transactionMessage = await cloudBlob.ReadTransactionMessageAsync(cancellationToken);
            await DeleteTransactionHelper.DeleteInstancesAsync(
                _dicomBlobDataStore,
                _dicomMetadataStore,
                _dicomInstanceMetadataStore,
                _dicomIndexDataStore,
                transactionMessage.DicomInstances,
                cancellationToken);
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

        private async Task CleanUpTransaction(CloudBlockBlob cloudBlob, CancellationToken cancellationToken)
        {
            try
            {
                TransactionMessage transactionMessage = await cloudBlob.ReadTransactionMessageAsync(cancellationToken);

                using (var transaction = new DicomTransaction(this, cloudBlob, transactionMessage, TimeSpan.FromSeconds(15), _logger))
                {
                    try
                    {
                        await transaction.BeginAsync(overwriteMessage: false, cancellationToken);

                        await DeleteTransactionHelper.DeleteInstancesAsync(
                            _dicomBlobDataStore,
                            _dicomMetadataStore,
                            _dicomInstanceMetadataStore,
                            _dicomIndexDataStore,
                            transactionMessage.DicomInstances,
                            cancellationToken);

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
                _logger.LogError($"Error when trying to clean up failed transaction: {cloudBlob.Name}. {e}");
            }
        }
    }
}
