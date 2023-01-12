// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using EnsureThat;

namespace Microsoft.Health.Dicom.Functions.Client.TaskHub;

internal sealed class ControlQueues
{
    private readonly QueueServiceClient _queueServiceClient;

    public ControlQueues(QueueServiceClient queueServiceClient)
        => _queueServiceClient = EnsureArg.IsNotNull(queueServiceClient, nameof(queueServiceClient));

    public async ValueTask<bool> ExistAsync(TaskHubInfo taskHubInfo, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(taskHubInfo, nameof(taskHubInfo));

        bool lastQueueExists = true;
        for (int i = 0; i < taskHubInfo.PartitionCount && lastQueueExists; i++)
        {
            QueueClient controlQueueClient = _queueServiceClient.GetQueueClient(GetName(taskHubInfo.TaskHubName, i));
            lastQueueExists = await controlQueueClient.ExistsAsync(cancellationToken);
        }

        return lastQueueExists;
    }

    // See: https://learn.microsoft.com/en-us/rest/api/storageservices/naming-queues-and-metadata#queue-names
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Queue names must be lowercase.")]
    internal static string GetName(string taskHub, int partition)
        => string.Format(CultureInfo.InvariantCulture, "{0}-control-{1:D2}", taskHub?.ToLowerInvariant(), partition);
}
