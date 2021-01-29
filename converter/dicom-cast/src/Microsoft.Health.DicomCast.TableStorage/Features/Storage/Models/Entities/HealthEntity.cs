// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Azure.Cosmos.Table;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage.Entities
{
    /// <summary>
    /// Entity used to check health of table storage
    /// </summary>
    public class HealthEntity : TableEntity
    {
        public HealthEntity()
        {
        }

        public HealthEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public string Data { get; set; }
    }
}
