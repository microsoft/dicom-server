// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using EnsureThat;

namespace Microsoft.Health.Dicom.Functions.Client.TaskHub;

internal sealed class ControlQueues
{
    private readonly QueueServiceClient _queueServiceClient;
    private readonly TaskHubInfo _taskHubInfo;

    public ControlQueues(QueueServiceClient queueServiceClient, TaskHubInfo taskHubInfo)
    {
        _queueServiceClient = EnsureArg.IsNotNull(queueServiceClient, nameof(queueServiceClient));
        _taskHubInfo = EnsureArg.IsNotNull(taskHubInfo, nameof(taskHubInfo));
    }

    public IEnumerable<string> Names => Enumerable
        .Range(0, _taskHubInfo.PartitionCount)
        .Select(i => GetName(_taskHubInfo.TaskHubName, i));

    public async ValueTask<bool> ExistAsync(CancellationToken cancellationToken = default)
    {
        foreach (string queue in Names)
        {
            QueueClient controlQueueClient = _queueServiceClient.GetQueueClient(queue);
            if (!await controlQueueClient.ExistsAsync(cancellationToken))
                return false;
        }

        return true;
    }

    // See: https://learn.microsoft.com/en-us/rest/api/storageservices/naming-queues-and-metadata#queue-names
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Queue names must be lowercase.")]
    internal static string GetName(string taskHub, int partition)
        => string.Format(CultureInfo.InvariantCulture, "{0}-control-{1:D2}", taskHub?.ToLowerInvariant(), partition);
}
