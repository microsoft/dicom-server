// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;

namespace Microsoft.Health.Dicom.Functions.Update.Models;

/// <summary>
///  Represents input to <see cref="UpdateDurableFunction.CompleteUpdateInstanceAsync"/>
/// </summary>
public sealed class CompleteInstanceArguments : GetInstanceArguments
{
    public DicomDataset Dataset { get; }

    public CompleteInstanceArguments(int partitionKey, string studyInstanceUid, DicomDataset dicomDataset)
        : base(partitionKey, studyInstanceUid)
    {
        Dataset = dicomDataset;
    }
}
