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

internal class WorkItemQueue
{
    private readonly QueueClient _queueClient;

    public WorkItemQueue(QueueServiceClient queueServiceClient, string taskHubName)
        => _queueClient = EnsureArg
            .IsNotNull(queueServiceClient, nameof(queueServiceClient))
            .GetQueueClient(GetName(EnsureArg.IsNotNullOrWhiteSpace(taskHubName, nameof(taskHubName))));

    public string Name => _queueClient.Name;

    public virtual async ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default)
        => await _queueClient.ExistsAsync(cancellationToken);

    // See: https://learn.microsoft.com/en-us/rest/api/storageservices/naming-queues-and-metadata#queue-names
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Queue names must be lowercase.")]
    internal static string GetName(string taskHub)
        => taskHub?.ToLowerInvariant() + "-workitems";
}
