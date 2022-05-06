// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.SchemaManager;

public class DicomSqlSchemaManager : IDicomSqlSchemaManager
{
    private readonly IScriptProvider _scriptProvider;
    private readonly ISchemaDataStore _schemaDataStore;
    private readonly IBaseSchemaRunner _baseSchemaRunner;
    private readonly ISchemaManagerDataStore _schemaManagerDataStore;
    private readonly ILogger<DicomSqlSchemaManager> _logger;

    public DicomSqlSchemaManager(
        IScriptProvider scriptProvider,
        ISchemaDataStore schemaDataStore,
        IBaseSchemaRunner baseSchemaRunner,
        ISchemaManagerDataStore schemaManagerDataStore,
        ILogger<DicomSqlSchemaManager> logger)
    {
        _scriptProvider = scriptProvider;
        _schemaDataStore = schemaDataStore;
        _baseSchemaRunner = baseSchemaRunner;
        _schemaManagerDataStore = schemaManagerDataStore;
        _logger = logger;
    }

    public async Task<ApplyCommandResult> ApplySchema(string connectionString, int version, CancellationToken token = default)
    {
        int currentSchemaVersion = await _schemaManagerDataStore.GetCurrentSchemaVersionAsync(token);

        if (currentSchemaVersion >= version)
        {
            return ApplyCommandResult.Unnecessary;
        }

        CompatibleVersions compatibleVersions = await _schemaDataStore.GetLatestCompatibleVersionsAsync(token);

        if (compatibleVersions.Max < version)
        {
            return ApplyCommandResult.Incompatible;
        }

        return ApplyCommandResult.Unsuccessful;
    }
}
