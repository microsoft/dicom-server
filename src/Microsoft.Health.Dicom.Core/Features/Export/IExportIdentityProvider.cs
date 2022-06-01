// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Microsoft.Health.Dicom.Core.Features.Export;

/// <summary>
/// Represents a provider for identities used by the export operation.
/// </summary>
public interface IExportIdentityProvider
{
    /// <summary>
    /// Asynchronously creates a new instance of the <see cref="IExportSink"/> interface whose implementation
    /// is based on the value of the <see cref="Type"/> property.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="GetCredentialAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property is the credential for the identity.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<TokenCredential> GetCredentialAsync(CancellationToken cancellationToken = default);
}
