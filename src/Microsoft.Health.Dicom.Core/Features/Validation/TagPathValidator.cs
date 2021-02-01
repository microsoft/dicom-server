// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    internal static class TagPathValidator
    {
        private static readonly Regex _expectedTagPathFormat = new Regex(@"\A\d{8}(\.(\d){8})*$", RegexOptions.Compiled);

        public static void Validate(string tagPath)
        {
            MatchCollection matches = _expectedTagPathFormat.Matches(tagPath);
            if (matches.Count != 1)
            {
                throw new TagPathValidationException(string.Format(DicomCoreResource.TagPathValidation, tagPath));
            }
        }
    }
}
