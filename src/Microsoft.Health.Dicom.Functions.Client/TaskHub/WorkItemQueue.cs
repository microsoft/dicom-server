// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using EnsureThat;

namespace Microsoft.Health.Dicom.Functions.Client.TaskHub;

internal sealed class WorkItemQueue
{
    private readonly QueueServiceClient _queueServiceClient;

    public WorkItemQueue(QueueServiceClient queueServiceClient)
        => _queueServiceClient = EnsureArg.IsNotNull(queueServiceClient, nameof(queueServiceClient));

    public async ValueTask<bool> ExistsAsync(TaskHubInfo taskHubInfo, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(taskHubInfo, nameof(taskHubInfo));

        QueueClient controlQueueClient = _queueServiceClient.GetQueueClient(GetName(taskHubInfo.TaskHubName));
        return await controlQueueClient.ExistsAsync(cancellationToken);
    }

    // See: https://learn.microsoft.com/en-us/rest/api/storageservices/naming-queues-and-metadata#queue-names
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Queue names must be lowercase.")]
    internal static string GetName(string taskHub)
        => taskHub?.ToLowerInvariant() + "-workitems";
}
