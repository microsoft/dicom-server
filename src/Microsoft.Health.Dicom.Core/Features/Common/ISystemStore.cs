// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Common;

/// <summary>
/// Provides functionalities managing the inputs to Azure Durable function.
/// </summary>
public interface ISystemStore
{
    /// <summary>
    /// Asynchronously stores a Azure durable function input
    /// </summary>
    /// <param name="input">The input to azure durable function</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous add operation.</returns>
    Task<string> StoreInputAsync<T>(T input, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Asynchronously gets a Azure durable function input
    /// </summary>
    /// <param name="name">Blob name</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task that represents the asynchronous get operation.</returns>
    Task<TResult> GetInputAsync<TResult>(string name, CancellationToken cancellationToken = default);
}
