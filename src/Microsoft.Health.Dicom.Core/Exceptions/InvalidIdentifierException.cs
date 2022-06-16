// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Exceptions;

public class InvalidIdentifierException : ElementValidationException
{
    public InvalidIdentifierException(string name)
        : base(name, DicomVR.UI, ValidationErrorCode.UidIsInvalid, DicomCoreResource.ErrorMessageUidIsInvalid)
    {
    }
}
