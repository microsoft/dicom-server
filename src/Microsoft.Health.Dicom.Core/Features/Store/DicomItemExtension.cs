// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    public static class DicomItemExtension
    {
        public static void ValidateDicomItem(this DicomItem dicomItem)
        {
            EnsureArg.IsNotNull(dicomItem, nameof(dicomItem));
            try
            {
                dicomItem.Validate();
            }
            catch (DicomValidationException ex)
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    ex.Message,
                    ex);
            }
        }
    }
}
