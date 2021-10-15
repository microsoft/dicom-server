// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public static class PartitionIdValidator
    {
        private static readonly Regex ValidIdentifierCharactersFormat = new Regex("^[A-Za-z0-9_.-]*$", RegexOptions.Compiled);

        public static void Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidPartitionIdException(value);
            }

            if (value.Length > 64 || !ValidIdentifierCharactersFormat.IsMatch(value))
            {
                throw new InvalidPartitionIdException(value);
            }
        }
    }
}
