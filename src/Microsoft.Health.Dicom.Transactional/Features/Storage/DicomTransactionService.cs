// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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

namespace Microsoft.Health.Dicom.Transactional.Features.Storage
{
    internal class DicomTransactionService : IDicomTransactionService
    {
        private readonly CloudBlobContainer _container;
        private readonly ILogger<DicomTransactionService> _logger;
        private static readonly TimeSpan TransactionMessageLease = TimeSpan.FromSeconds(15);

        public DicomTransactionService(
            CloudBlobClient client,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            ILogger<DicomTransactionService> logger)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
            EnsureArg.IsNotNull(logger, nameof(logger));

            BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationName);

            _container = client.GetContainerReference(containerConfiguration.ContainerName);
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<ITransaction> BeginTransactionAsync(DicomSeries dicomSeries, DicomInstance[] dicomInstances, CancellationToken cancellationToken = default)
        {
            var transactionMessage = new TransactionMessage(dicomSeries, new HashSet<DicomInstance>(dicomInstances));

            foreach (DicomInstance instance in dicomInstances)
            {
                transactionMessage.AddInstance(instance);
            }

            var result = new DicomTransaction(CreateTransactionCloudBlob(dicomSeries), transactionMessage, TransactionMessageLease, _logger);
            await result.BeginAsync(overwriteMessage: true, _ => Task.CompletedTask, cancellationToken);
            return result;
        }

        /// <inheritdoc />
        public async Task<ITransaction> BeginTransactionAsync(DicomSeries dicomSeries, DicomInstance dicomInstance, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomInstance, nameof(dicomInstance));
            return await BeginTransactionAsync(dicomSeries, new[] { dicomInstance }, cancellationToken);
        }

        private CloudBlockBlob CreateTransactionCloudBlob(DicomSeries dicomSeries)
            => _container.GetBlockBlobReference($"/transaction/{dicomSeries.StudyInstanceUID}_{dicomSeries.SeriesInstanceUID}");
    }
}
