// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public sealed class CancelWorkitemDatasetValidator : WorkitemDatasetValidator
    {
        protected override void OnValidate(DicomDataset dicomDataset, string workitemInstanceUid)
        {
            ValidateProcedureStepState(dicomDataset, workitemInstanceUid);

            if (dicomDataset.TryGetString(DicomTag.TransactionUID, out var _))
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.UnexpectedTag,
                        nameof(DicomTag.TransactionUID),
                        @"CancelWorkitem",
                        workitemInstanceUid));
            }
        }
    }
}
