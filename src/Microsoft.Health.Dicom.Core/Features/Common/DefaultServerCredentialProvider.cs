// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;

namespace Microsoft.Health.Dicom.Core.Features.Common;

internal sealed class DefaultServerCredentialProvider : IServerCredentialProvider
{
    // TODO: Allow users to configure defaults
    public Task<TokenCredential> GetCredentialAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<TokenCredential>(new DefaultAzureCredential(includeInteractiveCredentials: false));
}
