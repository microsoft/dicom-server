// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using EnsureThat;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace SqlSchemaRunner;

public class LibrarySchemaClient : ISchemaClient
{
    private readonly IScriptProvider _scriptProvider;
    private readonly ISchemaDataStore _schemaDataStore;
    private readonly SchemaInformation _schemaInformation;

    public LibrarySchemaClient(IScriptProvider scriptProvider, ISchemaDataStore schemaDataStore, SchemaInformation schemaInformation)
    {
        _scriptProvider = EnsureArg.IsNotNull(scriptProvider);
        _schemaDataStore = EnsureArg.IsNotNull(schemaDataStore);
        _schemaInformation = EnsureArg.IsNotNull(schemaInformation);
    }

    public void SetUri(Uri uri)
    {
    }

    public async Task<List<CurrentVersion>> GetCurrentVersionInformationAsync(CancellationToken cancellationToken)
    {
        List<CurrentVersionInformation>? currentVersions = await _schemaDataStore.GetCurrentVersionAsync(cancellationToken);

        IEnumerable<CurrentVersion> versions = currentVersions.Select(version => new CurrentVersion(version.Id, version.Status.ToString(), new ReadOnlyCollection<string>(version.Servers)));

        return versions.ToList();
    }

    public Task<string> GetScriptAsync(Uri scriptUri, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(scriptUri);
        int segment = int.Parse(scriptUri.Segments.Last().Replace(".sql", string.Empty, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(_scriptProvider.GetMigrationScript(segment, true));
    }

    public Task<string> GetDiffScriptAsync(Uri diffScriptUri, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(diffScriptUri);
        int segment = int.Parse(diffScriptUri.Segments.Last().Replace(".sql", string.Empty, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(_scriptProvider.GetMigrationScript(segment, false));
    }

    public async Task<CompatibleVersion> GetCompatibilityAsync(CancellationToken cancellationToken)
    {
        CompatibleVersions? compatibleVersions = await _schemaDataStore.GetLatestCompatibleVersionsAsync(cancellationToken);
        return new CompatibleVersion(compatibleVersions.Min, compatibleVersions.Max);
    }

    public Task<List<AvailableVersion>> GetAvailabilityAsync(CancellationToken cancellationToken)
    {
        var availableVersions = new List<AvailableVersion>();

        var currentVersion = _schemaInformation.Current ?? 1;
        for (var version = currentVersion; version <= _schemaInformation.MaximumSupportedVersion; version++)
        {
            var routeValues = new Dictionary<string, object> { { "id", version } };
            string scriptUri = $"{version}.sql";
            string diffScriptUri = string.Empty;
            if (version > 1)
            {
                diffScriptUri = $"{version}.diff.sql";
            }

            availableVersions.Add(new AvailableVersion(version, scriptUri, diffScriptUri));
        }

        return Task.FromResult(availableVersions);
    }
}
