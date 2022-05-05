// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace DicomSchemaManager.Core;

public interface IDicomSqlSchemaManager
{
    public Task<ApplyCommandResult> ApplySchema(string connectionString, int version, CancellationToken token = default);

    //public Task<IList<CompatibleVersion>> GetCompatibleVersions(string connectionString, CancellationToken cancellationToken = default);

    //public Task<IList<CurrentVersion>> GetCurrentSchema(string connectionString, CancellationToken cancellationToken = default);
}
