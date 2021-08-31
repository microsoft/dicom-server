// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Exceptions.Validation
{
    public abstract class ElementValidationException : ValidationException
    {
        protected ElementValidationException(string name, DicomVR vr, ValidationErrorCode errorCode, string message) : base(message)
        {
            Name = EnsureArg.IsNotNull(name, nameof(name));
            VR = EnsureArg.IsNotNull(vr, nameof(vr));
            ErrorCode = errorCode;
        }

        protected ElementValidationException(string name, DicomVR vr, string value, ValidationErrorCode errorCode, string message) : base(message)
        {
            Name = EnsureArg.IsNotNull(name, nameof(name));
            VR = EnsureArg.IsNotNull(vr, nameof(vr));
            Value = EnsureArg.IsNotNull(value, nameof(value));
            ErrorCode = errorCode;
        }

        public string Name { get; }

        public DicomVR VR { get; }

        public string Value { get; }

        public ValidationErrorCode ErrorCode { get; }

        public override string Message
        {
            get
            {
                return Value == null
                    ? string.Format(DicomCoreResource.DicomElementValidationFailed, Name, VR.Code, base.Message)
                    : string.Format(DicomCoreResource.DicomElementValidationFailedWithValue, Name, Value, VR.Code, base.Message);
            }
        }
    }
}
