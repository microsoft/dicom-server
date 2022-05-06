// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;

namespace DicomSchemaManager.Core;

public class DicomSqlSchemaManager : IDicomSqlSchemaManager
{
    private readonly IScriptProvider _scriptProvider;
    private readonly ISchemaDataStore _schemaDataStore;
    private readonly IBaseSchemaRunner _baseSchemaRunner;
    private readonly ISchemaManagerDataStore _schemaManagerDataStore;

    public DicomSqlSchemaManager(
        IScriptProvider scriptProvider,
        ISchemaDataStore schemaDataStore,
        IBaseSchemaRunner baseSchemaRunner,
        ISchemaManagerDataStore schemaManagerDataStore)
    {
        _scriptProvider = scriptProvider;
        _schemaDataStore = schemaDataStore;
        _baseSchemaRunner = baseSchemaRunner;
        _schemaManagerDataStore = schemaManagerDataStore;
    }

    public async Task<ApplyCommandResult> ApplySchema(string connectionString, int version, CancellationToken token = default)
    {
        ApplyCommandResult result = ApplyCommandResult.Unsuccessful;

        int currentSchemaVersion = await _schemaManagerDataStore.GetCurrentSchemaVersionAsync(token);

        if (currentSchemaVersion >= version)
        {
            return ApplyCommandResult.Unnecessary;
        }

        return result;
    }
}
