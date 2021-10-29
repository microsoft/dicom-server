// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    internal static class ElementValidationExceptionFactory
    {
        public static ElementValidationException CreateDateIsInvalidException(string name, string value)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(value, nameof(value));

            return new ElementValidationException(name, DicomVR.DA, value, ValidationErrorCode.DateIsInvalid, DicomCoreResource.ErrorMessageDateIsInvalid);
        }

        public static ElementValidationException CreateDateTimeIsInvalidException(string name, string value)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(value, nameof(value));

            return new ElementValidationException(name, DicomVR.DT, value, ValidationErrorCode.DateTimeIsInvalid, DicomCoreResource.ErrorMessageDateTimeIsInvalid);
        }

        public static ElementValidationException CreateTimeIsInvalidException(string name, string value)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(value, nameof(value));

            return new ElementValidationException(name, DicomVR.TM, value, ValidationErrorCode.TimeIsInvalid, DicomCoreResource.ErrorMessageTimeIsInvalid);
        }

        public static ElementValidationException CreateExceedMaxLengthException(string name, DicomVR vr, string value, int maxlength)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(vr, nameof(vr));
            EnsureArg.IsNotNull(value, nameof(value));

            var message = string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageExceedMaxLength, maxlength);
            return new ElementValidationException(name, vr, value, ValidationErrorCode.ExceedMaxLength, message);
        }

        public static ElementValidationException CreateInvalidCharactersException(string name, DicomVR vr, string value)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(vr, nameof(vr));
            EnsureArg.IsNotNull(value, nameof(value));

            return new ElementValidationException(name, vr, value, ValidationErrorCode.InvalidCharacters, DicomCoreResource.ErrorMessageInvalidCharacters);
        }

        public static ElementValidationException CreateMultiValuesException(string name, DicomVR vr)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(vr, nameof(vr));

            return new ElementValidationException(name, vr, ValidationErrorCode.MultiValues, DicomCoreResource.ErrorMessageMultiValues);
        }
        public static ElementValidationException CreatePersonNameExceedMaxComponentsException(string name, string value)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(value, nameof(value));

            return new ElementValidationException(name, DicomVR.PN, value, ValidationErrorCode.PersonNameExceedMaxComponents, DicomCoreResource.ErrorMessagePersonNameExceedMaxComponents);
        }
        public static ElementValidationException CreatePersonNameExceedMaxGroupsException(string name, string value)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(value, nameof(value));

            return new ElementValidationException(name, DicomVR.PN, value, ValidationErrorCode.PersonNameExceedMaxGroups, DicomCoreResource.ErrorMessagePersonNameExceedMaxGroups);
        }
        public static ElementValidationException CreatePersonNameGroupExceedMaxLengthException(string name, string value)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(value, nameof(value));

            return new ElementValidationException(name, DicomVR.PN, value, ValidationErrorCode.PersonNameGroupExceedMaxLength, DicomCoreResource.ErrorMessagePersonNameGroupExceedMaxLength);
        }

        public static ElementValidationException CreateUnexpectedLengthException(string name, DicomVR vr, string value, int expectedLength)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(vr, nameof(vr));
            EnsureArg.IsNotNull(value, nameof(value));

            return new ElementValidationException(name, vr, value, ValidationErrorCode.UnexpectedLength, string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageUnexpectedLength, expectedLength));
        }

        public static ElementValidationException CreateUnexpectedLengthException(string name, DicomVR vr, int expectedLength)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(vr, nameof(vr));

            return new ElementValidationException(name, vr, ValidationErrorCode.UnexpectedLength, string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageUnexpectedLength, expectedLength));
        }

        public static ElementValidationException CreateUnexpectedVRException(string name, DicomVR vr, DicomVR expectedVR)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(vr, nameof(vr));
            EnsureArg.IsNotNull(expectedVR, nameof(expectedVR));

            var message = string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageUnexpectedVR, name, expectedVR, vr);
            return new ElementValidationException(name, vr, ValidationErrorCode.UnexpectedVR, message);
        }

    }
}
