// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Azure.KeyVault;

internal sealed class InMemorySecretStore : ISecretStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, string>> _secrets =
        new ConcurrentDictionary<string, ConcurrentDictionary<int, string>>(StringComparer.Ordinal);

    public Task<bool> DeleteSecretAsync(string name, CancellationToken cancellationToken = default)
        => _secrets.TryRemove(name, out _) ? Task.FromResult(true) : Task.FromResult(false);

    public Task<string> GetSecretAsync(string name, string version = null, CancellationToken cancellationToken = default)
    {
        if (_secrets.TryGetValue(name, out ConcurrentDictionary<int, string> versions))
        {
            if (version == null)
            {
                return Task.FromResult(versions.OrderByDescending(x => x.Key).First().Value);
            }
            else if (int.TryParse(version, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)
                && versions.TryGetValue(n, out string value))
            {
                return Task.FromResult(value);
            }
        }

        return Task.FromException<string>(
            new KeyNotFoundException(
                string.Format(CultureInfo.CurrentCulture, DicomAzureResource.SecretNotFound, name, version)));
    }

    public IAsyncEnumerable<string> ListSecretsAsync(CancellationToken cancellationToken = default)
        => _secrets.Keys.ToAsyncEnumerable();

    public Task<string> SetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        int nextVersion;
        ConcurrentDictionary<int, string> versions;
        do
        {
            versions = _secrets.GetOrAdd(name, _ => new ConcurrentDictionary<int, string>());
            nextVersion = versions
                .OrderByDescending(x => x.Key)
                .FirstOrDefault(KeyValuePair.Create(0, string.Empty)).Key + 1;
        } while (!versions.TryAdd(nextVersion, value)
            || !_secrets.TryGetValue(name, out ConcurrentDictionary<int, string> currentVersions)
            || !ReferenceEquals(currentVersions, versions));

        return Task.FromResult(nextVersion.ToString(CultureInfo.InvariantCulture));
    }
}
