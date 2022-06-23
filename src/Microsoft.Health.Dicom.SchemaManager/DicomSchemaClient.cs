// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.SchemaManager;

public class DicomSchemaClient : ISchemaClient
{
    private readonly IScriptProvider _scriptProvider;
    private readonly ISchemaDataStore _schemaDataStore;
    private readonly ISchemaManagerDataStore _schemaManagerDataStore;

    public DicomSchemaClient(
        IScriptProvider scriptProvider,
        ISchemaDataStore schemaDataStore,
        ISchemaManagerDataStore schemaManagerDataStore)
    {
        _scriptProvider = scriptProvider;
        _schemaDataStore = schemaDataStore;
        _schemaManagerDataStore = schemaManagerDataStore;
    }

    public async Task<List<AvailableVersion>> GetAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        int currentVersion = await _schemaManagerDataStore.GetCurrentSchemaVersionAsync(cancellationToken);
        var availableVersions = new List<AvailableVersion>();

        for (int version = currentVersion; version <= SchemaVersionConstants.Max; version++)
        {
            string scriptUri = $"{version}.sql";
            string diffScriptUri = version > 1 ? $"{version}.diff.sql" : string.Empty;
            availableVersions.Add(new AvailableVersion(version, scriptUri, diffScriptUri));
        }

        return availableVersions;
    }

    public async Task<CompatibleVersion> GetCompatibilityAsync(CancellationToken cancellationToken = default)
    {
        CompatibleVersions compatibleVersions = await _schemaDataStore.GetLatestCompatibleVersionsAsync(cancellationToken);
        return new CompatibleVersion(compatibleVersions.Min, compatibleVersions.Max);
    }

    public async Task<List<CurrentVersion>> GetCurrentVersionInformationAsync(CancellationToken cancellationToken = default)
    {
        List<CurrentVersionInformation>? currentVersions = await _schemaDataStore.GetCurrentVersionAsync(cancellationToken);

        IEnumerable<CurrentVersion> versions = currentVersions.Select(version => new CurrentVersion(version.Id, version.Status.ToString(), new ReadOnlyCollection<string>(version.Servers)));

        return versions.ToList();
    }

    public Task<string> GetDiffScriptAsync(int version, CancellationToken cancellationToken = default)
    {
        string diffScript = _scriptProvider.GetMigrationScript(version, false);
        return Task.FromResult(diffScript);
    }

    public Task<string> GetScriptAsync(int version, CancellationToken cancellationToken = default)
    {
        string script = _scriptProvider.GetMigrationScript(version, true);
        return Task.FromResult(script);
    }
}
