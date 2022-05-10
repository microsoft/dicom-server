// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Common;

/// <summary>
/// Represents something that may have sensitive information that has been secured
/// by an instance of the <see cref="ISecretStore"/> class.
/// </summary>
public interface ISensitive
{
    /// <summary>
    /// Asynchronously secures sensitive data in the given <paramref name="secretStore"/>.
    /// </summary>
    /// <param name="secretStore">A store for sensitive information.</param>
    /// <param name="secretName">The name to use for any uploaded secrets.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A task representing the <see cref="ClassifyAsync"/> operation.</returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task ClassifyAsync(ISecretStore secretStore, string secretName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously reveals sensitive data from the given <paramref name="secretStore"/>.
    /// </summary>
    /// <param name="secretStore">A store for sensitive information.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A task representing the <see cref="DeclassifyAsync"/> operation.</returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task DeclassifyAsync(ISecretStore secretStore, CancellationToken cancellationToken = default);
}
