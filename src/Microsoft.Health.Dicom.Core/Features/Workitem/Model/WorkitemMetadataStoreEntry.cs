// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Workitem.Model;

public sealed class WorkitemMetadataStoreEntry : WorkitemInstanceIdentifier
{
    public WorkitemMetadataStoreEntry(string workitemUid, long workitemKey, long watermark, int partitionKey = default)
        : base(workitemUid, workitemKey, partitionKey, watermark)
    {
    }

    public WorkitemStoreStatus Status { get; set; }

    public string TransactionUid { get; set; }

    public string ProcedureStepStateStringValue => ProcedureStepState.GetStringValue();

    public ProcedureStepState ProcedureStepState { get; set; }
}
