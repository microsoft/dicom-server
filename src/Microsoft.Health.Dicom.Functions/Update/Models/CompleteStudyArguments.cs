// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Functions.Update.Models;

/// <summary>
/// Represents input to <see cref="UpdateDurableFunction.CompleteUpdateStudyAsync"/>
/// </summary>
public sealed class CompleteStudyArguments : UpdateInstanceWatermarkArguments
{
    public string ChangeDataset { get; set; }

    public CompleteStudyArguments(int partitionKey, string studyInstanceUid, string dicomDataset)
        : base(partitionKey, studyInstanceUid)
    {
        ChangeDataset = dicomDataset;
    }
}
