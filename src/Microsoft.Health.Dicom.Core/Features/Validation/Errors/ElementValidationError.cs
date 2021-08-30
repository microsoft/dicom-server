// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Validation.Errors
{
    public abstract class ElementValidationError
    {
        protected ElementValidationError(string name, DicomVR vr)
        {
            Name = EnsureArg.IsNotNull(name, nameof(name));
            VR = EnsureArg.IsNotNull(vr, nameof(vr));
        }

        protected ElementValidationError(string name, DicomVR vr, string value)
        {
            Name = EnsureArg.IsNotNull(name, nameof(name));
            VR = EnsureArg.IsNotNull(vr, nameof(vr));
            Value = EnsureArg.IsNotNull(value, nameof(value));
        }

        public string Name { get; }

        public DicomVR VR { get; }

        public string Value { get; }

        public abstract ValidationErrorCode ErrorCode { get; }

        protected abstract string GetInnerMessage();

        public string Message
        {
            get
            {
                return Value == null
                    ? string.Format(DicomCoreResource.DicomElementValidationFailed, Name, VR.Code, GetInnerMessage())
                    : string.Format(DicomCoreResource.DicomElementValidationFailedWithValue, Name, Value, VR.Code, GetInnerMessage());
            }
        }
    }
}
