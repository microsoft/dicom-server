// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;

namespace DicomSchemaManager.Core;

public interface IDicomSchemaClient
{
    Task<List<CurrentVersion>> GetCurrentVersionInformationAsync(CancellationToken cancellationToken);

    Task<string> GetScriptAsync(int version, CancellationToken cancellationToken);

    Task<string> GetDiffScriptAsync(int version, CancellationToken cancellationToken);

    Task<CompatibleVersion> GetCompatibilityAsync(CancellationToken cancellationToken);
}
