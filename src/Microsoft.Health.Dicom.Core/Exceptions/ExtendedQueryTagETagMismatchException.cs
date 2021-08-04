// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when version of a set of extended query tags mismatch.
    /// </summary>
    public class ExtendedQueryTagsVersionMismatchException
        : DicomServerException
    {
        public ExtendedQueryTagsVersionMismatchException()
            : base(DicomCoreResource.ExtendedQueryTagsVersionMismatch)
        {
        }
    }
}
