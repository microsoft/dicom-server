// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Health.DicomCast.Core.Features.State;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage.Models.Entities
{
    public class SyncStateEntity : TableEntity
    {
        public SyncStateEntity()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncStateEntity"/> class.
        /// </summary>
        /// <param name="syncState">They SyncState </param>
        public SyncStateEntity(SyncState syncState)
        {
            EnsureArg.IsNotNull(syncState, nameof(syncState));

            PartitionKey = Constants.SyncStatePartitionKey;
            RowKey = Constants.SyncStateRowKey;

            SyncedSequence = syncState.SyncedSequence;
        }

        public long SyncedSequence { get; set; }
    }
}
