// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public static class TagPathValidator
    {
        public static void Validate(string tagPath)
        {
            Match match = Regex.Match(tagPath, @"\A\(\d{4},\d{4}\)(\.\((\d){4},(\d){4}\))*$");
            if (!match.Success)
            {
                throw new TagPathValidationException(string.Format(DicomCoreResource.TagPathValidation, tagPath));
            }
        }
    }
}
