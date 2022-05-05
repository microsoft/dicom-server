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

    public Task<ApplyCommandResult> ApplySchema(string connectionString, int version, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
