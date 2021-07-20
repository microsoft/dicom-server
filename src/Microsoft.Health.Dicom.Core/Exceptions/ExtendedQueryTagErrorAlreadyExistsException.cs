// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when the extended query tag error already exists.
    /// </summary>
    public class ExtendedQueryTagErrorAlreadyExistsException : DicomServerException
    {
        public ExtendedQueryTagErrorAlreadyExistsException()
            : base(DicomCoreResource.ExtendedQueryTagErrorAlreadyExists)
        {
        }
    }
}
