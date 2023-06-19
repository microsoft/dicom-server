// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Partitioning;

internal class SqlPartitionStoreV4 : ISqlPartitionStore
{
    public virtual SchemaVersion Version => SchemaVersion.V4;

    public virtual Task<Partition> AddPartitionAsync(string partitionName, CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }

    public virtual Task<IEnumerable<Partition>> GetPartitionsAsync(CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }

    public virtual Task<Partition> GetPartitionAsync(string partitionName, CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }
}
