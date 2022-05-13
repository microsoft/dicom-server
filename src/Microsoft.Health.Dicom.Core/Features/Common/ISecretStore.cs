// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Common;

/// <summary>
/// Represents a data store that securely stores named secrets.
/// </summary>
public interface ISecretStore
{
    /// <summary>
    /// Asynchronously deletes the secret with the given name.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="DeleteSecretAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property is <see langword="true"/> if the
    /// secret was successfully deleted; otherwise, <see langword="false"/> if it does not exist.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<bool> DeleteSecretAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the secret with the given name.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <param name="version">The optional version of the secret. Defaults to the latest value.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="GetSecretAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property is value of the secret, if found;
    /// otherwise, <see langword="null"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<T> GetSecretAsync<T>(string name, string version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously lists all of the secrts in the store.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>The names of the secrets in the store.</returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    IAsyncEnumerable<string> ListSecretsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously uploads the value for a secret.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <param name="value">The value of the secret.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="GetSecretAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property is new version of the secret.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<string> SetSecretAsync<T>(string name, T value, CancellationToken cancellationToken = default);
}
