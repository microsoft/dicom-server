// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when custom tags don't exist or custom tag with given tag path does not exist.
    /// </summary>
    public class CustomTagNotFoundException : ResourceNotFoundException
    {
        public CustomTagNotFoundException(string message)
            : base(message)
        {
        }
    }
}
