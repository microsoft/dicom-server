// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public static class ImplicitValueRepresentationValidator
    {
        public static void Validate(DicomDataset dicomDataset)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(dicomDataset.InternalTransferSyntax, nameof(dicomDataset.InternalTransferSyntax));

            if (!dicomDataset.InternalTransferSyntax.IsExplicitVR)
            {
                throw new NotAcceptableException(ValidationErrorCode.ImplicitVRNotAllowed.GetMessage());
            }
        }
    }
}
