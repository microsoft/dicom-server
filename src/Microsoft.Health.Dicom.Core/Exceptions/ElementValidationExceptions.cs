// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public static class ElementValidationExceptions
    {
        public static ElementValidationException DateIsInvalidException(string name, string value)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(value, nameof(value));

            return new ElementValidationException(name, DicomVR.DA, value, ValidationErrorCode.DateIsInvalid, DicomCoreResource.ErrorMessageDateIsInvalid);
        }

        public static ElementValidationException ExceedMaxLengthException(string name, DicomVR vr, string value, int maxlength)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(vr, nameof(vr));
            EnsureArg.IsNotNull(value, nameof(value));

            var message = string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageExceedMaxLength, maxlength);
            return new ElementValidationException(name, vr, value, ValidationErrorCode.ExceedMaxLength, message);
        }

        public static ElementValidationException InvalidCharactersException(string name, DicomVR vr, string value)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(vr, nameof(vr));
            EnsureArg.IsNotNull(value, nameof(value));

            return new ElementValidationException(name, vr, value, ValidationErrorCode.InvalidCharacters, DicomCoreResource.ErrorMessageInvalidCharacters);
        }

        public static ElementValidationException MultiValuesException(string name, DicomVR vr)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(vr, nameof(vr));

            return new ElementValidationException(name, vr, ValidationErrorCode.MultiValues, DicomCoreResource.ErrorMessageMultiValues);
        }
        public static ElementValidationException PersonNameExceedMaxComponentsException(string name, string value)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(value, nameof(value));

            return new ElementValidationException(name, DicomVR.PN, value, ValidationErrorCode.PersonNameExceedMaxComponents, DicomCoreResource.ErrorMessagePersonNameExceedMaxComponents);
        }
        public static ElementValidationException PersonNameExceedMaxGroupsException(string name, string value)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(value, nameof(value));

            return new ElementValidationException(name, DicomVR.PN, value, ValidationErrorCode.PersonNameExceedMaxGroups, DicomCoreResource.ErrorMessagePersonNameExceedMaxGroups);
        }
        public static ElementValidationException PersonNameGroupExceedMaxLengthException(string name, string value)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(value, nameof(value));

            return new ElementValidationException(name, DicomVR.PN, value, ValidationErrorCode.PersonNameGroupExceedMaxLength, DicomCoreResource.ErrorMessagePersonNameGroupExceedMaxLength);
        }

        public static ElementValidationException UnexpectedLengthException(string name, DicomVR vr, string value, int expectedLength)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(vr, nameof(vr));
            EnsureArg.IsNotNull(value, nameof(value));

            return new ElementValidationException(name, vr, value, ValidationErrorCode.UnexpectedLength, string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageUnexpectedLength, expectedLength));
        }

        public static ElementValidationException UnexpectedLengthException(string name, DicomVR vr, int expectedLength)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(vr, nameof(vr));

            return new ElementValidationException(name, vr, ValidationErrorCode.UnexpectedLength, string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageUnexpectedLength, expectedLength));
        }

        public static ElementValidationException UnexpectedVRException(string name, DicomVR vr, DicomVR expectedVR)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(vr, nameof(vr));
            EnsureArg.IsNotNull(expectedVR, nameof(expectedVR));

            var message = string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageUnexpectedVR, name, expectedVR, vr);
            return new ElementValidationException(name, vr, ValidationErrorCode.UnexpectedVR, message);
        }


    }
}
