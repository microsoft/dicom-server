// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Partition
{
    internal class SqlPartitionStoreV4 : ISqlPartitionStore
    {
        public virtual SchemaVersion Version => SchemaVersion.V4;

        public virtual Task<PartitionEntry> AddPartition(string partitionName, CancellationToken cancellationToken = default)
        {
            throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
        }

        public virtual Task<IEnumerable<PartitionEntry>> GetPartitions(CancellationToken cancellationToken = default)
        {
            throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
        }

        public virtual Task<PartitionEntry> GetPartition(string partitionName, CancellationToken cancellationToken = default)
        {
            throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
        }
    }
}
