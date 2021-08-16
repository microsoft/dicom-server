// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class DicomElementValidationException : ValidationException
    {
        public DicomElementValidationException(string name, DicomVR vr, string message)
            : base(message)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(vr, nameof(vr));
            Name = name;
            VR = vr;
        }

        public DicomElementValidationException(string name, DicomVR vr, string message, string value)
           : base(message)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(vr, nameof(vr));
            EnsureArg.IsNotNull(value, nameof(value));

            Name = name;
            VR = vr;
            Value = value;
        }

        public override string Message => Value == null
            ? string.Format(DicomCoreResource.DicomElementValidationFailed, Name, VR.Code, base.Message)
            : string.Format(DicomCoreResource.DicomElementValidationFailedWithValue, Name, Value, VR.Code, base.Message);

        public string Name { get; }

        public DicomVR VR { get; }

        public string Value { get; }
    }
}
