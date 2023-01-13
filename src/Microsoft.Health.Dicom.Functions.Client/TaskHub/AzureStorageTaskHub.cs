// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Dicom.Functions.Client.TaskHub;

internal class AzureStorageTaskHub : ITaskHub
{
    private readonly ControlQueues _controlQueues;
    private readonly WorkItemQueue _workItemQueue;
    private readonly InstanceTable _instanceTable;
    private readonly HistoryTable _historyTable;
    private readonly ILogger<AzureStorageTaskHub> _logger;

    public AzureStorageTaskHub(
        ControlQueues controlQueues,
        WorkItemQueue workItemQueue,
        InstanceTable instanceTable,
        HistoryTable historyTable,
        ILogger<AzureStorageTaskHub> logger)
    {
        _controlQueues = EnsureArg.IsNotNull(controlQueues, nameof(controlQueues));
        _workItemQueue = EnsureArg.IsNotNull(workItemQueue, nameof(workItemQueue));
        _instanceTable = EnsureArg.IsNotNull(instanceTable, nameof(instanceTable));
        _historyTable = EnsureArg.IsNotNull(historyTable, nameof(historyTable));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async ValueTask<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        // Check that each of the components found in the Task Hub are available
        if (!await _controlQueues.ExistAsync(cancellationToken))
        {
            _logger.LogWarning("Cannot find one or more of the control queues: [{ControlQueues}].", string.Join(", ", _controlQueues.Names));
            return false;
        }

        if (!await _workItemQueue.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("Cannot find work item queue '{WorkItemQueue}.'", _workItemQueue.Name);
            return false;
        }

        if (!await _instanceTable.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("Cannot find instance table '{InstanceTable}.'", _instanceTable.Name);
            return false;
        }

        if (!await _historyTable.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("Cannot find history table '{HistoryTable}.'", _historyTable.Name);
            return false;
        }

        return true;
    }
}
