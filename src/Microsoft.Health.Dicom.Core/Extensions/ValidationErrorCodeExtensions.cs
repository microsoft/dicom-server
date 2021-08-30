// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Health.Dicom.Core.Features.Validation;
using static Microsoft.Health.Dicom.Core.Features.Validation.ValidationErrorCode;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    internal static class ValidationErrorCodeExtensions
    {
        private static readonly IReadOnlyDictionary<ValidationErrorCode, string> MessageMap = new Dictionary<ValidationErrorCode, string>()
        {
            { None, string.Empty },

            { MultiValues, DicomCoreResource.ErrorMessageMultiValues },
            { ExceedMaxLength, DicomCoreResource.SimpleErrorMessageExceedMaxLength },
            { UnexpectedLength, DicomCoreResource.SimpleErrorMessageUnexpectedLength },
            { InvalidCharacters, DicomCoreResource.ErrorMessageInvalidCharacters },
            { UnexpectedVR, DicomCoreResource.SimpleErrorMessageUnexpectedVR },

            { PersonNameExceedMaxGroups, DicomCoreResource.ErrorMessagePersonNameExceedMaxComponents},
            { PersonNameGroupExceedMaxLength, DicomCoreResource.ErrorMessagePersonNameGroupExceedMaxLength },
            { PersonNameExceedMaxComponents, DicomCoreResource.ErrorMessagePersonNameExceedMaxComponents},

            { DateIsInvalid, DicomCoreResource.ErrorMessageDateIsInvalid },

            { UidIsInvalid, DicomCoreResource.ErrorMessageUidIsInvalid},

        };
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
