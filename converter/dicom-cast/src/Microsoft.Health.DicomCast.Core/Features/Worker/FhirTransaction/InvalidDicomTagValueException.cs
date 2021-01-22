// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using EnsureThat;
using Microsoft.Health.DicomCast.Core.Exceptions;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Exception thrown when a DICOM tag value is invalid.
    /// </summary>
    public class InvalidDicomTagValueException : DicomTagException
    {
        public InvalidDicomTagValueException(string tagName, string value)
            : base(FormatMessage(tagName, value))
        {
        }

        private static string FormatMessage(string tagName, string value)
        {
            EnsureArg.IsNotNullOrWhiteSpace(tagName, nameof(tagName));

            return string.Format(CultureInfo.InvariantCulture, DicomCastCoreResource.InvalidDicomTagValue, value, tagName);
        }
    }
}
