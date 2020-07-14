// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class DicomElementValidationException : ValidationException
    {
        public DicomElementValidationException(string name, string value, DicomVR vr, string message)
           : base(message)
        {
            Name = name;
            Value = value;
            VR = vr;
        }

        public override string Message => string.Format(DicomCoreResource.DicomElementValidationFailed, Name, Value, VR.Code, base.Message);

        public string Name { get; private set; }

        public string Value { get; private set; }

        public DicomVR VR { get; private set; }
    }
}
