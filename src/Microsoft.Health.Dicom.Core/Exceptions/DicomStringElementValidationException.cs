// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class DicomStringElementValidationException : DicomElementValidationException
    {
        public DicomStringElementValidationException(ValidationErrorCode errorCode, string name, string value, DicomVR vr, string message)
           : base(errorCode, name, vr, message)
        {
            EnsureArg.IsNotNull(vr, nameof(vr));
            Value = value;
        }


        public override string Message => string.Format(DicomCoreResource.DicomStringElementValidationFailed, Name, Value, VR.Code, base.Message);

        public string Value { get; }
    }
}
