// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.SchemaManager;

public interface IDicomSqlSchemaManager
{
    public Task<ApplyCommandResult> ApplySchema(string connectionString, int version, CancellationToken token = default);
}
