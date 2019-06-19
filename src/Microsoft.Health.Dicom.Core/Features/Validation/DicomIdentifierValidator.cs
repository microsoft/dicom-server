// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;
using FluentValidation.Validators;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    /// <summary>
    /// Validates a unique identifer conforms to the rules from http://dicom.nema.org/dicom/2013/output/chtml/part05/chapter_9.html
    /// </summary>
    /// <seealso cref="RegularExpressionValidator" />
    public class DicomIdentifierValidator : RegularExpressionValidator
    {
        public const string IdentifierRegex = "^[A-Za-z0-9\\-\\.]{1,64}$";

        public DicomIdentifierValidator()
            : base(IdentifierRegex, RegexOptions.Singleline | RegexOptions.Compiled)
        {
        }
    }
}
