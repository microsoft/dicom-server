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
        private readonly ITransactionResolver _transactionResolver;
        private readonly ILogger<DicomTransactionService> _logger;
        private static readonly TimeSpan TransactionMessageLease = TimeSpan.FromSeconds(15);

        public DicomTransactionService(
            CloudBlobClient client,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            ITransactionResolver transactionResolver,
            ILogger<DicomTransactionService> logger)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
            EnsureArg.IsNotNull(transactionResolver, nameof(transactionResolver));
            EnsureArg.IsNotNull(logger, nameof(logger));

            BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationName);

            _container = client.GetContainerReference(containerConfiguration.ContainerName);
            _transactionResolver = transactionResolver;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<ITransaction> BeginTransactionAsync(DicomSeries dicomSeries, DicomInstance[] dicomInstances, CancellationToken cancellationToken = default)
        {
            var transactionMessage = new TransactionMessage(dicomSeries, new HashSet<DicomInstance>(dicomInstances));

            CloudBlockBlob cloudBlob = CreateTransactionCloudBlob(dicomSeries);
            var result = new DicomTransaction(_transactionResolver, cloudBlob, transactionMessage, TransactionMessageLease, _logger);
            await result.BeginAsync(cancellationToken);
            return result;
        }

        /// <inheritdoc />
        public async Task<ITransaction> BeginTransactionAsync(DicomSeries dicomSeries, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomSeries, nameof(dicomSeries));
            return await BeginTransactionAsync(dicomSeries, Array.Empty<DicomInstance>(), cancellationToken);
        }

        private CloudBlockBlob CreateTransactionCloudBlob(DicomSeries dicomSeries)
            => _container.GetBlockBlobReference($"/transaction/{dicomSeries.StudyInstanceUID}_{dicomSeries.SeriesInstanceUID}");
    }
}
