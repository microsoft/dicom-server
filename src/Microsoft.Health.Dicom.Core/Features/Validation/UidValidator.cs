// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public static class UidValidator
    {
        public static void Validate(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidIdentifierException(value, name);
            }

            // validate the value
            DicomElementMinimumValidation.ValidateUI(value, name);
        }
    }
}
