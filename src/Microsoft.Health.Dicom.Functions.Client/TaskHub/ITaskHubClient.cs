// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Functions.Client.TaskHub;

/// <summary>
/// Represents a client for retrieving a resolved <see cref="ITaskHub"/> based on possibly remote metadata.
/// </summary>
public interface ITaskHubClient
{
    /// <summary>
    /// Gets the name of the desired task hub.
    /// </summary>
    /// <value>The task hub name.</value>
    string TaskHubName { get; }

    /// <summary>
    /// Asynchronously retrieves an <see cref="ITaskHub"/> from its storage provider.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous get operation. The value of the <see cref="Task{TResult}.Result"/>
    /// property is the resolved task hub if found; otherwise <see langword="null"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// The <paramref name="cancellationToken"/> requested cancellation.
    /// </exception>
    ValueTask<ITaskHub> GetTaskHubAsync(CancellationToken cancellationToken = default);
}
