// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    public static class ValidationErrorCodeExtensions
    {
        private static readonly ImmutableDictionary<ValidationErrorCode, string> MessageMap = ImmutableDictionary.CreateRange(
            new KeyValuePair<ValidationErrorCode, string>[]
            {
                KeyValuePair.Create(ValidationErrorCode.None, string.Empty),

                KeyValuePair.Create(ValidationErrorCode.MultipleValues, DicomCoreResource.ErrorMessageMultiValues),
                KeyValuePair.Create(ValidationErrorCode.ExceedMaxLength, DicomCoreResource.SimpleErrorMessageExceedMaxLength),
                KeyValuePair.Create(ValidationErrorCode.UnexpectedLength, DicomCoreResource.SimpleErrorMessageUnexpectedLength),
                KeyValuePair.Create(ValidationErrorCode.InvalidCharacters, DicomCoreResource.ErrorMessageInvalidCharacters),
                KeyValuePair.Create(ValidationErrorCode.UnexpectedVR, DicomCoreResource.SimpleErrorMessageUnexpectedVR),
                KeyValuePair.Create(ValidationErrorCode.ImplicitVRNotAllowed, DicomCoreResource.ImplicitVRNotAllowed),

                KeyValuePair.Create(ValidationErrorCode.PersonNameExceedMaxGroups, DicomCoreResource.ErrorMessagePersonNameExceedMaxComponents),
                KeyValuePair.Create(ValidationErrorCode.PersonNameGroupExceedMaxLength, DicomCoreResource.ErrorMessagePersonNameGroupExceedMaxLength),
                KeyValuePair.Create(ValidationErrorCode.PersonNameExceedMaxComponents, DicomCoreResource.ErrorMessagePersonNameExceedMaxComponents),

                KeyValuePair.Create(ValidationErrorCode.DateIsInvalid, DicomCoreResource.ErrorMessageDateIsInvalid),
                KeyValuePair.Create(ValidationErrorCode.UidIsInvalid, DicomCoreResource.ErrorMessageUidIsInvalid),
                KeyValuePair.Create(ValidationErrorCode.DateTimeIsInvalid, DicomCoreResource.ErrorMessageDateTimeIsInvalid),
                KeyValuePair.Create(ValidationErrorCode.TimeIsInvalid, DicomCoreResource.ErrorMessageTimeIsInvalid),
                KeyValuePair.Create(ValidationErrorCode.IntegerStringIsInvalid, DicomCoreResource.ErrorMessageIntegerStringIsInvalid),
                KeyValuePair.Create(ValidationErrorCode.DecimalStringIsInvalid, DicomCoreResource.ErrorMessageDecimalStringIsInvalid),
                KeyValuePair.Create(ValidationErrorCode.SequenceDisallowed, DicomCoreResource.SequentialDicomTagsNotSupported),
                KeyValuePair.Create(ValidationErrorCode.NestedSequence, DicomCoreResource.NestedSequencesNotSupported),
            });

        /// <summary>
        /// Get error message for error code.
        /// </summary>
        /// <param name="errorCode">The error code</param>
        /// <returns>The message</returns>
        public static string GetMessage(this ValidationErrorCode errorCode)
        {
            if (MessageMap.TryGetValue(errorCode, out string message))
            {
                return message;
            }
            else
            {
                Debug.Fail($"Missing message for error code {errorCode}");
                return string.Empty;
            }
        }
    }
}
