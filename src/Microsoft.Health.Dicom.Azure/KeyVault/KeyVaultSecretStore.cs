// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Security.KeyVault.Secrets;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.AzureKeyVault;

public class KeyVaultSecretStore : ISecretStore
{
    private readonly SecretClient _secretClient;

    public KeyVaultSecretStore(SecretClient secretClient)
    {
        _secretClient = secretClient;
    }

    public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken)
    {
        await _secretClient.SetSecretAsync(secretName, secretValue, cancellationToken);
    }

    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken)
    {
        var secret = await _secretClient.GetSecretAsync(secretName, version: null, cancellationToken);
        return secret.Value.Value;
    }

    public async Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken)
    {
        var operation = await _secretClient.StartDeleteSecretAsync(secretName, cancellationToken);
        await operation.WaitForCompletionAsync(cancellationToken);

        await _secretClient.PurgeDeletedSecretAsync(secretName, cancellationToken);
    }

}
