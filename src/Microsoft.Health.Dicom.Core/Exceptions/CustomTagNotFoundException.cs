// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when custom tag is not found.
    /// </summary>
    public class CustomTagNotFoundException : ValidationException
    {
        public CustomTagNotFoundException()
            : base(DicomCoreResource.CustomTagNotFound)
        {
        }
    }
}
