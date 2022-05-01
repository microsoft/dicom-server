// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.AzureKeyVault;

[SuppressMessage("Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "This class is instantiated via dependency injection.")]
internal sealed class KeyVaultSecretStore : ISecretStore
{
    private readonly SecretClient _secretClient;

    public KeyVaultSecretStore(SecretClient secretClient)
        => _secretClient = EnsureArg.IsNotNull(secretClient, nameof(secretClient));

    public async Task DeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        DeleteSecretOperation operation = await _secretClient.StartDeleteSecretAsync(name, cancellationToken);
        await operation.WaitForCompletionAsync(cancellationToken);
    }

    public async Task<string> GetSecretAsync(string name, string version = null, CancellationToken cancellationToken = default)
    {
        Response<KeyVaultSecret> secret = await _secretClient.GetSecretAsync(name, version, cancellationToken);
        return secret.Value.Value;
    }

    public IAsyncEnumerable<string> ListSecretsAsync(CancellationToken cancellationToken = default)
        => _secretClient.GetPropertiesOfSecretsAsync(cancellationToken).Select(x => x.Name);

    public async Task<string> SetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        Response<KeyVaultSecret> response = await _secretClient.SetSecretAsync(name, value, cancellationToken);
        return response.Value.Properties.Version;
    }
}
