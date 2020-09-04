// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Exception thrown when required DICOM tag is missing.
    /// </summary>
    public class MissingRequiredDicomTagException : Exception
    {
        public MissingRequiredDicomTagException(string dicomTagName)
            : base(FormatMessage(dicomTagName))
        {
        }

        private static string FormatMessage(string dicomTagName)
        {
            EnsureArg.IsNotNullOrWhiteSpace(dicomTagName, nameof(dicomTagName));

            return string.Format(DicomCastCoreResource.MissingRequiredDicomTag, dicomTagName);
        }
    }
}
