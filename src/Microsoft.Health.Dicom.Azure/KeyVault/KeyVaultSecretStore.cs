// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Azure.KeyVault;

internal sealed class KeyVaultSecretStore : ISecretStore
{
    private readonly SecretClient _secretClient;

    private const string SecretNotFoundErrorCode = "SecretNotFound";

    public KeyVaultSecretStore(SecretClient secretClient)
        => _secretClient = EnsureArg.IsNotNull(secretClient, nameof(secretClient));

    public async Task<bool> DeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        DeleteSecretOperation operation;
        try
        {
            operation = await _secretClient.StartDeleteSecretAsync(name, cancellationToken);
        }
        catch (RequestFailedException rfe) when (rfe.ErrorCode == SecretNotFoundErrorCode)
        {
            return false;
        }

        await operation.WaitForCompletionAsync(cancellationToken);
        return true;
    }

    public async Task<string> GetSecretAsync(string name, string version = null, CancellationToken cancellationToken = default)
    {
        try
        {
            Response<KeyVaultSecret> response = await _secretClient.GetSecretAsync(name, version, cancellationToken);
            return response.Value.Value;
        }
        catch (RequestFailedException rfe) when (rfe.ErrorCode == SecretNotFoundErrorCode)
        {
            throw new KeyNotFoundException(
                string.Format(CultureInfo.CurrentCulture, DicomAzureResource.SecretNotFound, name, version),
                rfe);
        }
    }

    public IAsyncEnumerable<string> ListSecretsAsync(CancellationToken cancellationToken = default)
        => _secretClient.GetPropertiesOfSecretsAsync(cancellationToken).Select(x => x.Name);

    public Task<string> SetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
        => SetSecretAsync(name, value, null, cancellationToken);

    public async Task<string> SetSecretAsync(string name, string value, string contentType, CancellationToken cancellationToken = default)
    {
        var secret = new KeyVaultSecret(name, value);

        if (contentType != null)
            secret.Properties.ContentType = contentType;

        Response<KeyVaultSecret> response = await _secretClient.SetSecretAsync(secret, cancellationToken);
        return response.Value.Properties.Version;
    }
}
