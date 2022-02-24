// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public sealed class CancelWorkitemDatasetValidator : WorkitemDatasetValidator
    {
        protected override void OnValidate(DicomDataset dataset)
        {
            // all the attributes in the dataset are optional. Hence no validation is being done here.
        }

        public static void ValidateProcedureStepStateInStore(string workitemUid, WorkitemMetadataStoreEntry workitemMetadata, WorkitemStateTransitionResult stateTransitionResult)
        {
            EnsureArg.IsNotNull(stateTransitionResult, nameof(stateTransitionResult));

            if (workitemMetadata == null)
            {
                throw new WorkitemNotFoundException(workitemUid);
            }

            if (workitemMetadata.Status != WorkitemStoreStatus.ReadWrite)
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.WorkitemUpdateIsNotAllowed,
                        workitemUid,
                        workitemMetadata.ProcedureStepState.GetStringValue()));
            }

            if (stateTransitionResult.IsError)
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.InvalidProcedureStepStateTransition,
                        workitemUid,
                        ProcedureStepState.Canceled,
                        stateTransitionResult.Code));
            }
        }
    }
}
