// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Queue.Features.Messages;

namespace Microsoft.Health.Dicom.Queue.Features.Storage
{
    internal class QueueTransaction : ITransaction
    {
        private const int RenewLeaseDelayMilliseconds = 5000;
        private readonly CloudQueue _cloudQueue;
        private readonly CloudQueueMessage _cloudQueueMessage;
        private readonly DicomQueueMessage _dicomQueueMessage;
        private readonly Task _renewLeaseTask;
        private readonly TimeSpan _messageLease;
        private readonly ILogger<QueueTransaction> _logger;
        private readonly CancellationTokenSource _renewLeaseCancellationTokenSource;
        private bool _disposed;

        public QueueTransaction(
            CloudQueue cloudQueue, CloudQueueMessage cloudQueueMessage, DicomQueueMessage dicomQueueMessage, TimeSpan messageLease)
        {
            EnsureArg.IsNotNull(cloudQueue, nameof(cloudQueue));
            EnsureArg.IsNotNull(cloudQueueMessage, nameof(cloudQueueMessage));
            EnsureArg.IsNotNull(dicomQueueMessage, nameof(dicomQueueMessage));
            EnsureArg.IsTrue(messageLease.TotalMilliseconds < RenewLeaseDelayMilliseconds, nameof(messageLease));

            _cloudQueue = cloudQueue;
            _cloudQueueMessage = cloudQueueMessage;
            _dicomQueueMessage = dicomQueueMessage;
            _messageLease = messageLease;
            _renewLeaseCancellationTokenSource = new CancellationTokenSource();
            _renewLeaseTask = RenewLeaseAsync(_renewLeaseCancellationTokenSource);
        }

        /// <inheritdoc />
        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            // Delete message from the queue.
            await _cloudQueue.DeleteMessageAsync(_cloudQueueMessage, cancellationToken);

            // Cancel lease renew task after committing in case the message is released back to the queue before the commit.
            CancelRenewLeaseTask();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                CancelRenewLeaseTask();
            }

            _disposed = true;
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

        private void CancelRenewLeaseTask()
        {
            _renewLeaseCancellationTokenSource.Cancel();
            _renewLeaseTask.Wait();
        }

        private async Task RenewLeaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            // The maximum time we will wait for the lease to renew is the time of the lease with 'some' buffer.
            var maximumRenewLeaseWaitSeconds = _messageLease.TotalSeconds - 1;
            DateTime renewLeaseLastUpdate = DateTime.UtcNow;

            await TryDelay(RenewLeaseDelayMilliseconds, cancellationTokenSource.Token);

            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await _cloudQueue.UpdateMessageAsync(_cloudQueueMessage, _messageLease, MessageUpdateFields.Visibility, cancellationTokenSource.Token);
                    renewLeaseLastUpdate = DateTime.UtcNow;

                    await TryDelay(RenewLeaseDelayMilliseconds, cancellationTokenSource.Token);
                }
                catch (Exception e)
                {
                    // If we fail to renew the lease for a queue message in the time it takes for the lease to expire
                    // we need to cancel and let the owner of the message know that the lease has expired.
                    if ((DateTime.UtcNow - renewLeaseLastUpdate).TotalSeconds > maximumRenewLeaseWaitSeconds && !cancellationTokenSource.IsCancellationRequested)
                    {
                        _logger.LogError($"Failed to renew the lease failed within {maximumRenewLeaseWaitSeconds} seconds for message: {_dicomQueueMessage.MessageId}. Exception: {e}.");
                        cancellationTokenSource.Cancel();
                    }
                    else
                    {
                        _logger.LogError($"Trying to renew for the lease failed for message: {_dicomQueueMessage.MessageId}. Exception: {e}.");
                    }
                }
            }
        }
    }
}
