// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Microsoft.Health.Dicom.Core.Features.Common;

/// <summary>
/// Represents a credential provider for the identity used by operations that
/// interact with components external to the DICOM server.
/// </summary>
public interface IExternalOperationCredentialProvider
{
    /// <summary>
    /// Asynchronously retrieves the token credential used for external operations.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="GetCredentialAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property is the credential for the operation;
    /// otherwise <see langword="null"/> if no credentials could be found.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<TokenCredential> GetCredentialAsync(CancellationToken cancellationToken = default);
}
