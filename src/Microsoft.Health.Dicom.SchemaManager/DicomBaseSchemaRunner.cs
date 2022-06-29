// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.SqlServer.Features.Schema.Manager;

namespace Microsoft.Health.Dicom.SchemaManager;

public class DicomBaseSchemaRunner : IBaseSchemaRunner
{
    public Task EnsureBaseSchemaExistsAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task EnsureInstanceSchemaRecordExistsAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
