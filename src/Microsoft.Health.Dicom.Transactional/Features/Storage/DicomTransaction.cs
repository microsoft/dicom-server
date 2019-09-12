// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Transaction;
using Microsoft.Health.Dicom.Transactional.Features.Storage.Models;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Transactional.Features.Storage
{
    internal class DicomTransaction : ITransaction
    {
        private const int RenewLeaseDelaySeconds = 5;
        private const int MinimumLeaseTimeSeconds = 15;
        private readonly TimeSpan _messageLease;
        private readonly ILogger _logger;
        private readonly CloudBlockBlob _cloudBlockBlob;
        private readonly CancellationTokenSource _renewLeaseCancellationTokenSource;
        private readonly JsonSerializer _jsonSerializer;
        private readonly Encoding _messageEncoding;
        private readonly TransactionMessage _transactionMessage;
        private Task _renewLeaseTask;
        private AccessCondition _cloudBlockBlobAccessCondition;
        private bool _disposed;

        public DicomTransaction(CloudBlockBlob cloudBlockBlob, TimeSpan messageLease, ILogger logger)
        {
            EnsureArg.IsNotNull(cloudBlockBlob, nameof(cloudBlockBlob));
            EnsureArg.IsTrue(messageLease.TotalSeconds >= MinimumLeaseTimeSeconds, nameof(messageLease));
            EnsureArg.IsTrue(messageLease.TotalSeconds > RenewLeaseDelaySeconds, nameof(messageLease));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _cloudBlockBlob = cloudBlockBlob;
            _messageLease = messageLease;
            _logger = logger;
            _renewLeaseCancellationTokenSource = new CancellationTokenSource();
            _jsonSerializer = new JsonSerializer();
            _messageEncoding = Encoding.UTF8;
        }

        /// <inheritdoc />
        public async Task AppendInstanceAsync(DicomInstance dicomInstance, CancellationToken cancellationToken)
        {
            _transactionMessage.AddInstance(dicomInstance);
            await UpdateTransactionMessageAsync(_transactionMessage, cancellationToken);
        }

        /// <inheritdoc />
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            // Delete message from the queue.
            await _cloudBlockBlob.DeleteIfExistsAsync(
                DeleteSnapshotsOption.None, _cloudBlockBlobAccessCondition, new BlobRequestOptions(), new OperationContext(), cancellationToken);

            // Cancel lease renew task after committing in case the message is released back to the queue before the commit.
            await CancelRenewLeaseTaskAsync();
        }

        /// <inheritdoc />
        public async Task AbortAsync(CancellationToken cancellationToken = default)
        {
            // Cancel renew task and attempt to release the lease.
            await CancelRenewLeaseTaskAsync();

            try
            {
                await _cloudBlockBlob.ReleaseLeaseAsync(_cloudBlockBlobAccessCondition, cancellationToken);
            }
            catch (StorageException e) when (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
            {
                // Ignore error when the blob does not exist.
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal async Task BeginAsync(TransactionMessage transactionMessage, CancellationToken cancellationToken = default)
        {
            // We first create an empty blob, then acquire lease.
            await _cloudBlockBlob.UploadFromByteArrayAsync(Array.Empty<byte>(), index: 0, count: 0, cancellationToken);
            var leaseId = await _cloudBlockBlob.AcquireLeaseAsync(_messageLease, proposedLeaseId: string.Empty, cancellationToken);

            _cloudBlockBlobAccessCondition = new AccessCondition() { LeaseId = leaseId };

            // Once lease has been acquired, start the renew task and upload the transaction message.
            _renewLeaseTask = RenewLeaseAsync(_cloudBlockBlob, _cloudBlockBlobAccessCondition, _messageLease, _logger, _renewLeaseCancellationTokenSource);
            await UpdateTransactionMessageAsync(transactionMessage, cancellationToken: cancellationToken);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                Task.WhenAll(AbortAsync());
                _renewLeaseCancellationTokenSource.Dispose();
            }

            _disposed = true;
        }

        private async Task UpdateTransactionMessageAsync(TransactionMessage transactionMessage, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(transactionMessage, nameof(transactionMessage));

            using (CloudBlobStream stream = await _cloudBlockBlob.OpenWriteAsync(
                                                    _cloudBlockBlobAccessCondition, new BlobRequestOptions(), new OperationContext(), cancellationToken))
            using (var streamWriter = new StreamWriter(stream, _messageEncoding))
            using (var jsonTextWriter = new JsonTextWriter(streamWriter))
            {
                _jsonSerializer.Serialize(jsonTextWriter, transactionMessage);
                jsonTextWriter.Flush();
            }
        }

        private static async Task TryDelay(int millisecondsDelay, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(millisecondsDelay, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // No need to trace here - the task is stopping as cancellation was requested.
            }
        }

        private static async Task RenewLeaseAsync(
            CloudBlockBlob cloudBlockBlob, AccessCondition accessCondition, TimeSpan messageLease, ILogger logger, CancellationTokenSource cancellationTokenSource)
        {
            EnsureArg.IsNotNull(cloudBlockBlob, nameof(cloudBlockBlob));
            EnsureArg.IsNotNull(accessCondition, nameof(accessCondition));
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(cancellationTokenSource, nameof(cancellationTokenSource));

            // The maximum time we will wait for the lease to renew is the time of the lease with 'some' buffer.
            var maximumRenewLeaseWaitSeconds = messageLease.TotalSeconds - 1;
            DateTime renewLeaseLastUpdate = DateTime.UtcNow;

            await TryDelay(RenewLeaseDelaySeconds, cancellationTokenSource.Token);

            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await cloudBlockBlob.RenewLeaseAsync(accessCondition, cancellationTokenSource.Token);
                    renewLeaseLastUpdate = DateTime.UtcNow;

                    await TryDelay(RenewLeaseDelaySeconds, cancellationTokenSource.Token);
                }
                catch (Exception e)
                {
                    // If we fail to renew the lease for a queue message in the time it takes for the lease to expire
                    // we need to cancel and let the owner of the message know that the lease has expired.
                    if ((DateTime.UtcNow - renewLeaseLastUpdate).TotalSeconds > maximumRenewLeaseWaitSeconds && !cancellationTokenSource.IsCancellationRequested)
                    {
                        logger.LogError($"Failed to renew the lease failed within {maximumRenewLeaseWaitSeconds} seconds with ID: {accessCondition.LeaseId}. Cloud Block Blob: {cloudBlockBlob.Name}. Exception: {e}.");
                        cancellationTokenSource.Cancel();
                    }
                    else
                    {
                        logger.LogError($"Trying to renew for the lease failed with ID: {accessCondition.LeaseId}. Cloud Block Blob: {cloudBlockBlob.Name}. Exception: {e}.");
                    }
                }
            }
        }

        private async Task CancelRenewLeaseTaskAsync()
        {
            _renewLeaseCancellationTokenSource.Cancel();
            await _renewLeaseTask.ConfigureAwait(false);
        }
    }
}
