// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Api.Web
{
    public class DicomFileLengthLimitExceededException : ValidationException
    {
        public DicomFileLengthLimitExceededException(long maxAllowedLength)
           : base(string.Format(DicomApiResource.DicomFileLengthLimitExceeded, maxAllowedLength))
        {
        }
    }
}
