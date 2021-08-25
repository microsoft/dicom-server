// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;

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

        public DicomElementValidationException(string name, DicomVR vr, string value, int valueTruncationLength, string message)
           : base(message)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(vr, nameof(vr));
            EnsureArg.IsNotNull(value, nameof(value));
            EnsureArg.IsGte(valueTruncationLength, 0, nameof(valueTruncationLength));

            Name = name;
            VR = vr;
            Value = value;
            ValueTruncationLength = valueTruncationLength;
        }

        public override string Message
        {
            get
            {
                if (Value == null)
                {
                    return string.Format(CultureInfo.InvariantCulture, DicomCoreResource.DicomElementValidationFailed, Name, VR.Code, base.Message);
                }

                bool truncated = Value.TryTruncate(ValueTruncationLength, out string truncatedValue);
                return truncated ?
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.DicomElementValidationFailedWithTruncatedValue, Name, truncatedValue, VR.Code, base.Message) :
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.DicomElementValidationFailedWithValue, Name, truncatedValue, VR.Code, base.Message);
            }
        }

        public string Name { get; }

        public DicomVR VR { get; }

        public string Value { get; }

        public int ValueTruncationLength { get; }
    }
}
