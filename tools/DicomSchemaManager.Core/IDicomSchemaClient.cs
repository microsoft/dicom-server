// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace DicomSchemaManager.Core;

public interface IDicomSchemaClient
{
    bool ApplySchema(string connectionString, CancellationToken token = default);
}
