// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when the Workitem instance already exists.
    /// </summary>
    public class WorkitemAlreadyExistsException : DicomServerException
    {
        public WorkitemAlreadyExistsException(string uid)
            : base(string.Format(DicomCoreResource.WorkitemInstanceAlreadyExists, uid))
        {
        }
    }
}
