// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Workitem.Model
{
    public sealed class WorkitemMetadataStoreEntry
    {
        public string WorkitemUid { get; set; }

        public long WorkitemKey { get; set; }

        public int PartitionKey { get; set; }

        public WorkitemStoreStatus Status { get; set; }

        public string TransactionUid { get; set; }

        public string ProcedureStepState { get; set; }
    }
}
