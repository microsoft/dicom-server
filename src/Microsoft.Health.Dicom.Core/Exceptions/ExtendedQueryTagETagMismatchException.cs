// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when version of a set of extended query tags mismatch.
    /// </summary>
    public class ExtendedQueryTagVersionMismatchException
        : DicomServerException
    {
        public ExtendedQueryTagVersionMismatchException()
            : base(DicomCoreResource.ExtendedQueryTagVersionMismatch)
        {
        }
    }
}
