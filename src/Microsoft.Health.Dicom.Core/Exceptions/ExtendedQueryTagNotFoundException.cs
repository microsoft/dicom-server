// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when extended query tags don't exist or extended query tag with given tag path does not exist.
    /// </summary>
    public class ExtendedQueryTagNotFoundException : ResourceNotFoundException
    {
        public ExtendedQueryTagNotFoundException(string message)
            : base(message)
        {
        }
    }
}
