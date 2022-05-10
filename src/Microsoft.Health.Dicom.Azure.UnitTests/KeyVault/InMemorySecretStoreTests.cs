// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Azure.KeyVault;
using Xunit;

namespace Microsoft.Health.Dicom.Azure.UnitTests.KeyVault;

public class InMemorySecretStoreTests
{
    private readonly InMemorySecretStore _secretStore = new InMemorySecretStore();

    [Fact]
    public async Task GivenMissingSecret_WhenDeletingSecret_ThenReturnFalse()
    {
        Assert.False(await _secretStore.DeleteSecretAsync("foo"));
    }

    [Fact]
    public async Task GivenValidSecret_WhenDeletingSecret_ThenReturnTrue()
    {
        const string secretName = "MySecret";
        await _secretStore.SetSecretAsync(secretName, "foo");
        await _secretStore.SetSecretAsync(secretName, "bar");
        await _secretStore.SetSecretAsync(secretName, "baz");

        Assert.True(await _secretStore.DeleteSecretAsync(secretName));
    }

    [Fact]
    public async Task GivenMissingSecret_WhenGettingSecret_ThenThrowException()
    {
        const string secretName = "MySecret";
        await _secretStore.SetSecretAsync(secretName, "foo");
        await _secretStore.SetSecretAsync(secretName, "bar");
        await _secretStore.SetSecretAsync(secretName, "baz");

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _secretStore.GetSecretAsync("MyOtherSecret", null));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _secretStore.GetSecretAsync(secretName, "4"));
    }

    [Fact]
    public async Task GivenStore_WhenGettingAndSettingSecret_ThenUpdateVersion()
    {
        const string secretName = "MySecret";
        string previous = await _secretStore.SetSecretAsync(secretName, "foo");
        string latest = await _secretStore.SetSecretAsync(secretName, "bar");

        Assert.Equal("bar", await _secretStore.GetSecretAsync(secretName, null));
        Assert.Equal("bar", await _secretStore.GetSecretAsync(secretName, latest));
        Assert.Equal("foo", await _secretStore.GetSecretAsync(secretName, previous));
    }

    [Fact]
    public async Task GivenStore_WhenListingSecrets_ThenRetrieveAllNames()
    {
        // Empty
        Assert.Empty(await _secretStore.ListSecretsAsync().ToArrayAsync());

        // Non-Empty
        await _secretStore.SetSecretAsync("Secret1", "foo");
        await _secretStore.SetSecretAsync("Secret1", "foobar");
        await _secretStore.SetSecretAsync("Secret2", "bar");
        await _secretStore.SetSecretAsync("Secret3", "baz");

        string[] actual = await _secretStore.ListSecretsAsync().ToArrayAsync();
        Assert.Equal(3, actual.Length);
        Assert.Contains("Secret1", actual);
        Assert.Contains("Secret2", actual);
        Assert.Contains("Secret3", actual);
    }
}
