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
        public DicomElementValidationException(string name, string value, DicomVR vr, string message)
           : base(message)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(value, nameof(value));
            EnsureArg.IsNotNull(vr, nameof(vr));
            EnsureArg.IsNotNull(message, nameof(message));

            Name = name;
            Value = value;
            VR = vr;
        }

        public override string Message => string.Format(DicomCoreResource.DicomElementValidationFailed, Name, Value, VR.Code, base.Message);

        public string Name { get; }

        public string Value { get; }

        public DicomVR VR { get; }
    }
}
