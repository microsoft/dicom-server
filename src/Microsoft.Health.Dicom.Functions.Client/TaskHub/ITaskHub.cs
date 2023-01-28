// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Functions.Client.TaskHub;

/// <summary>
/// Represents a Durable Task Framework (DTFx) task hub that manages the state of long-running orchestrations.
/// </summary>
public interface ITaskHub
{
    /// <summary>
    /// Asynchronously checks whether the task hub is up-and-running.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The value of the <see cref="Task{TResult}.Result"/>
    /// property is <see langword="true"/> if the task hub is healthy; otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// The <paramref name="cancellationToken"/> requested cancellation.
    /// </exception>
    ValueTask<bool> IsReadyAsync(CancellationToken cancellationToken = default);
}
