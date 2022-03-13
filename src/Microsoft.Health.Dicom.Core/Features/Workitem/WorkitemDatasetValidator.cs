// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Store;

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


    protected static void ValidateEmptyValue(DicomDataset dataset, DicomTag tag)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));

        if (dataset.GetValueCount(tag) > 0)
        {
            throw new DatasetValidationException(
                FailureReasonCodes.ValidationFailure,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.AttributeMustBeEmpty,
                    tag));
        }
    }

    protected static void ValidateNotPresent(DicomDataset dataset, DicomTag tag)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));

        if (dataset.Contains(tag))
        {
            throw new DatasetValidationException(
                FailureReasonCodes.ValidationFailure,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.AttributeNotAllowed,
                    tag));
        }
    }

}
