// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace DicomSchemaManager.Core;

public class DicomSchemaClient : IDicomSchemaClient
{
    public bool ApplySchema(string connectionString, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
