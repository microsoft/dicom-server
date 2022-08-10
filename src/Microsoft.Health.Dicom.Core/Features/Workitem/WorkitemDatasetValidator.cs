// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

public abstract class WorkitemDatasetValidator : IWorkitemDatasetValidator
{
    public string Name => GetType().Name;

    public void Validate(DicomDataset dataset)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));

        OnValidate(dataset);
    }

    protected abstract void OnValidate(DicomDataset dataset);
}
