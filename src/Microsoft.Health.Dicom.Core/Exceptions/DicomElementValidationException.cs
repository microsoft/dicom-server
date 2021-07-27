// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class DicomElementValidationException : ValidationException
    {
        public DicomElementValidationException(ValidationErrorCode errorCode, string name, DicomVR vr, string message)
           : base(message)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(vr, nameof(vr));
            EnsureArg.IsNotNull(message, nameof(message));

            ErrorCode = errorCode;
            Name = name;
            VR = vr;
        }

        public ValidationErrorCode ErrorCode { get; }

        public override string Message => string.Format(DicomCoreResource.DicomElementValidationFailed, Name, VR.Code, base.Message);

        public string Name { get; }

        public DicomVR VR { get; }
    }
}
