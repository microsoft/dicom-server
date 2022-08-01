// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Core;

namespace Microsoft.Health.Dicom.Core.Features.Common;

/// <summary>
/// Represents a credential provider for the identity used by operations that interact with components external to the DICOM server.
/// </summary>
public interface IExternalOperationCredentialProvider
{
    /// <summary>
    /// Retrieves the token credential used for external operations.
    /// </summary>
    /// <returns>
    /// Tthe credential for the operation if found; otherwise <see langword="null"/>.
    /// </returns>
    TokenCredential GetTokenCredential();
}
