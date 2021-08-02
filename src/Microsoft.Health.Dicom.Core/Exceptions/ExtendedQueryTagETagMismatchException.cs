// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when ETag of extended query tag mismatch.
    /// </summary>
    public class ExtendedQueryTagETagMismatchException : DicomServerException
    {
        public ExtendedQueryTagETagMismatchException()
            : base(DicomCoreResource.ExtendedQueryTagETagMismatch)
        {
        }
    }
}
