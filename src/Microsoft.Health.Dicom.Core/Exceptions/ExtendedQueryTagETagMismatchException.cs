// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when max extended query tag version mismatch.
    /// </summary>
    public class MaxExtendedQueryTagVersionMismatchException
        : DicomServerException
    {
        public MaxExtendedQueryTagVersionMismatchException()
            : base(DicomCoreResource.MaxExtendedQueryTagVersionMismatch)
        {
        }
    }
}
