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
using Azure;
using Azure.Storage.Queues;
using EnsureThat;

namespace Microsoft.Health.Dicom.Functions.Client.TaskHub;

internal class ControlQueueCollection
{
    private readonly QueueServiceClient _queueServiceClient;
    private readonly TaskHubInfo _taskHubInfo;

    public ControlQueueCollection(QueueServiceClient queueServiceClient, TaskHubInfo taskHubInfo)
    {
        _queueServiceClient = EnsureArg.IsNotNull(queueServiceClient, nameof(queueServiceClient));
        _taskHubInfo = EnsureArg.IsNotNull(taskHubInfo, nameof(taskHubInfo));
    }

    public IEnumerable<string> Names => Enumerable
        .Range(0, _taskHubInfo.PartitionCount)
        .Select(i => GetName(_taskHubInfo.TaskHubName, i));

    public virtual async ValueTask<bool> ExistAsync(CancellationToken cancellationToken = default)
    {
        // Note: The maximum number of partitions is 16
        Response<bool>[] responses = await Task
            .WhenAll(Names
                .Select(n => _queueServiceClient.GetQueueClient(n).ExistsAsync(cancellationToken)));

        return responses.All(x => x.Value);
    }

    // See: https://learn.microsoft.com/en-us/rest/api/storageservices/naming-queues-and-metadata#queue-names
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Queue names must be lowercase.")]
    internal static string GetName(string taskHub, int partition)
        => string.Format(CultureInfo.InvariantCulture, "{0}-control-{1:D2}", taskHub?.ToLowerInvariant(), partition);
}
