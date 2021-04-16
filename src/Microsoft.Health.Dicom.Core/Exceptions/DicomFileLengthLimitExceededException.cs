// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class DicomFileLengthLimitExceededException : ValidationException
    {
        public DicomFileLengthLimitExceededException(long maxAllowedLength)
           : base(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.DicomFileLengthLimitExceeded, maxAllowedLength))
        {
        }
    }
}
