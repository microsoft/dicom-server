// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Azure.KeyVault;

internal sealed class KeyVaultSecretStore : ISecretStore
{
    private readonly SecretClient _secretClient;
    private readonly JsonSerializerOptions _serializerOptions;

    private const string SecretNotFoundErrorCode = "SecretNotFound";

    public KeyVaultSecretStore(SecretClient secretClient, IOptions<JsonSerializerOptions> serializerOptions)
    {
        _secretClient = EnsureArg.IsNotNull(secretClient, nameof(secretClient));
        _serializerOptions = EnsureArg.IsNotNull(serializerOptions?.Value, nameof(serializerOptions));
    }

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

    public async Task<T> GetSecretAsync<T>(string name, string version = null, CancellationToken cancellationToken = default)
    {
        try
        {
            Response<KeyVaultSecret> response = await _secretClient.GetSecretAsync(name, version, cancellationToken);
            return JsonSerializer.Deserialize<T>(response.Value.Value, _serializerOptions);
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

    public async Task<string> SetSecretAsync<T>(string name, T value, CancellationToken cancellationToken = default)
    {
        Response<KeyVaultSecret> response = await _secretClient.SetSecretAsync(
            name,
            JsonSerializer.Serialize(value, _serializerOptions),
            cancellationToken);

        return response.Value.Properties.Version;
    }
}
