// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

/// <summary>
/// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the minimum requirement when Adding.
/// <see href="https://dicom.nema.org/medical/dicom/current/output/html/part04.html#sect_5.4.2.1">Dicom 3.4.5.4.2.1</see>
/// </summary>
public class AddWorkitemDatasetValidator : WorkitemDatasetValidator
{
    /// <summary>
    /// Validate requirement codes for dicom tags based on the spec.
    /// Reference: <see href="https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.5-3"/>
    /// </summary>
    /// <param name="dataset">Dataset to be validated.</param>
    protected override void OnValidate(DicomDataset dataset)
    {
        // TODO: return all validation exceptions together
        dataset.ValidateAllRequirements(WorkitemRequestType.Add);
    }
}
