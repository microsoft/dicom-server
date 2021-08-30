// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Validation.Errors;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    internal class UidValidation : ElementValidation
    {
        private static readonly Regex ValidIdentifierCharactersFormat = new Regex("^[0-9\\.]*[0-9]$", RegexOptions.Compiled);

        public override void Validate(DicomElement dicomElement)
        {
            base.Validate(dicomElement);

            string value = dicomElement.Get<string>();
            string name = dicomElement.Tag.GetFriendlyName();
            Validate(value, name, allowEmpty: true);
        }

        public static void Validate(string value, string name, bool allowEmpty = false)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (allowEmpty)
                {
                    return;
                }

                throw new InvalidIdentifierException(new UidIsInValidError(name, value));
            }

            // trailling spaces are allowed
            value = value.TrimEnd(' ');

            if (value.Length > 64)
            {
                // UI value is validated in other cases like params for WADO, DELETE. So keeping the exception specific.
                throw new InvalidIdentifierException(new UidIsInValidError(name, value));
            }

            if (!ValidIdentifierCharactersFormat.IsMatch(value))
            {
                throw new InvalidIdentifierException(new UidIsInValidError(name, value));
            }
        }

    }
}
