// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;

namespace DicomSchemaManager.Core;

public class DicomSchemaClient : IDicomSchemaClient
{
    private readonly ISchemaDataStore _schemaDataStore;
    private readonly IScriptProvider _scriptProvider;

    public DicomSchemaClient(ISchemaDataStore schemaDataStore, IScriptProvider scriptProvider)
    {
        _schemaDataStore = schemaDataStore;
        _scriptProvider = scriptProvider;
    }

    public Task<List<CurrentVersion>> GetCurrentVersionInformationAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetScriptAsync(int version, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetDiffScriptAsync(int version, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<CompatibleVersion> GetCompatibilityAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

}
