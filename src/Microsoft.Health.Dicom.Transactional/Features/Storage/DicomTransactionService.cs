// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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
    public class DicomTransactionService : IDicomTransactionService
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

        public async Task<ITransaction> CreateTransactionAsync(DicomInstance dicomInstance, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomInstance, nameof(dicomInstance));

            var transactionMessage = new TransactionMessage(dicomInstance);
            var result = new DicomTransaction(CreateTransactionBlockBlob(), TransactionMessageLease, _logger);
            await result.BeginAsync(transactionMessage, cancellationToken);
            return result;
        }

        private CloudBlockBlob CreateTransactionBlockBlob() => _container.GetBlockBlobReference($"/transaction/{Guid.NewGuid()}");
    }
}
