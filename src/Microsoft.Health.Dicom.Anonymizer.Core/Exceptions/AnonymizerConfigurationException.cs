// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Exceptions
{
    public class AnonymizerConfigurationException : DicomAnonymizationException
    {
        public AnonymizerConfigurationException(DicomAnonymizationErrorCode errorCode, string message)
            : base(errorCode, message)
        {
        }

        public AnonymizerConfigurationException(DicomAnonymizationErrorCode errorCode, string message, Exception innerException)
            : base(errorCode, message, innerException)
        {
        }
    }
}
