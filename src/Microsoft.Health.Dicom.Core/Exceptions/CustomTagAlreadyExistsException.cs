// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when the custom tag already exists.
    /// </summary>
    public class CustomTagAlreadyExistsException : DicomServerException
    {
        public CustomTagAlreadyExistsException()
            : base(DicomCoreResource.CustomTagAlreadyExists)
        {
        }
    }
}
