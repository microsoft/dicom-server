// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Health.Dicom.AzureKeyVault;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Azure.UnitTests.KeyVault;

public class KeyVaultSecretStoreTests
{
    private readonly SecretClient _secretClient;
    private readonly KeyVaultSecretStore _secretStore;

    public KeyVaultSecretStoreTests()
    {
        _secretClient = Substitute.For<SecretClient>();
        _secretStore = new KeyVaultSecretStore(_secretClient);
    }

    [Fact]
    public async Task GivenKeyVault_WhenDeletingSecret_ThenRemove()
    {
        using var tokenSource = new CancellationTokenSource();
        const string secretName = "MySecret";

        DeleteSecretOperation operation = Substitute.For<DeleteSecretOperation>();
        _secretClient.StartDeleteSecretAsync(secretName, tokenSource.Token).Returns(operation);
        operation
            .WaitForCompletionAsync(tokenSource.Token)
            .Returns(Substitute.For<Response<DeletedSecret>>());

        await _secretStore.DeleteSecretAsync(secretName, tokenSource.Token);

        await _secretClient.Received(1).StartDeleteSecretAsync(secretName, tokenSource.Token);
        await operation.Received(1).WaitForCompletionAsync(tokenSource.Token);
    }

    [Theory]
    [InlineData("Secret1", "12345", "foo")]
    [InlineData("Secret2", null, "bar")]
    public async Task GivenKeyVault_WhenGettingSecret_ThenRemove(string name, string version, string value)
    {
        using var tokenSource = new CancellationTokenSource();

        Response<KeyVaultSecret> response = Substitute.For<Response<KeyVaultSecret>>();
        _secretClient.GetSecretAsync(name, version, tokenSource.Token).Returns(response);
        response.Value.Returns(new KeyVaultSecret(name, value));

        Assert.Equal(value, await _secretStore.GetSecretAsync(name, version, tokenSource.Token));

        await _secretClient.Received(1).GetSecretAsync(name, version, tokenSource.Token);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("one", "ii", "3")]
    public async Task GivenKeyVault_WhenListingSecrets_ThenRetrieveAllNames(params string[] names)
    {
        names ??= Array.Empty<string>();

        using var tokenSource = new CancellationTokenSource();

        AsyncPageable<SecretProperties> response = Substitute.For<AsyncPageable<SecretProperties>>();
        _secretClient.GetPropertiesOfSecretsAsync(tokenSource.Token).Returns(response);
        response
            .GetAsyncEnumerator(default) // The real implementation forwards the proper token
            .Returns(names
                .Select(x => new SecretProperties(x))
                .ToAsyncEnumerable()
                .GetAsyncEnumerator());

        string[] actual = await _secretStore.ListSecretsAsync(tokenSource.Token).ToArrayAsync();
        Assert.True(names.SequenceEqual(actual));

        _secretClient.Received(1).GetPropertiesOfSecretsAsync(tokenSource.Token);
    }

    [Theory]
    [InlineData("Secret1", "1", "foo")]
    [InlineData("Secret1", "2", "bar")]
    public async Task GivenKeyVault_WhenSettingSecret_ThenUpdateVersion(string name, string version, string value)
    {
        using var tokenSource = new CancellationTokenSource();

        Response<KeyVaultSecret> response = Substitute.For<Response<KeyVaultSecret>>();
        _secretClient.SetSecretAsync(name, value, tokenSource.Token).Returns(response);
        response.Value.Returns(CreateSecret(name, version, value));

        Assert.Equal(version, await _secretStore.SetSecretAsync(name, value, tokenSource.Token));

        await _secretClient.Received(1).SetSecretAsync(name, value, tokenSource.Token);
    }

    private static KeyVaultSecret CreateSecret(string name, string version, string value)
    {
        var secret = new KeyVaultSecret(name, value);

        // Version cannot be set via the existing API
        MethodInfo setter = typeof(SecretProperties)
            .GetProperty(nameof(SecretProperties.Version))
            .GetSetMethod(nonPublic: true);

        setter.Invoke(secret.Properties, new object[] { version });

        return secret;
    }
}
