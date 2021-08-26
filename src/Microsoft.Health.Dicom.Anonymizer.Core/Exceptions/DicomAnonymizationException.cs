// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Exceptions
{
    public class DicomAnonymizationException : Exception
    {
        public DicomAnonymizationException(DicomAnonymizationErrorCode errorCode, string message)
            : base(message)
        {
            DicomAnonymizerErrorCode = errorCode;
        }

        public DicomAnonymizationException(DicomAnonymizationErrorCode errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            DicomAnonymizerErrorCode = errorCode;
        }

        public DicomAnonymizationErrorCode DicomAnonymizerErrorCode { get; }
    }
}
