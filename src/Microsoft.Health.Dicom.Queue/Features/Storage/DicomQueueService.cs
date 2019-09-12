// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Azure.Storage.Queue;

namespace Microsoft.Health.Dicom.Queue.Features.Storage
{
    public class DicomQueueService
    {
        private const int QueueMessageLeaseSeconds = 5;
        private readonly CloudQueueClient _queueClient;
        private static readonly TimeSpan MessageLease = TimeSpan.FromSeconds(QueueMessageLeaseSeconds);

        public DicomQueueService(CloudQueueClient queueClient)
        {
            EnsureArg.IsNotNull(queueClient, nameof(queueClient));

            _queueClient = queueClient;
        }

        public ITransaction CreateDicomQueueTransaction()
        {
            return new QueueTransaction(_queueClient, null, null, MessageLease);
        }
    }
}
