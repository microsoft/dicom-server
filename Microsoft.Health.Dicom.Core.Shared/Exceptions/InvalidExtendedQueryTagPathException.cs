// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when extended query tag path is invalid.
    /// </summary>
    public class InvalidExtendedQueryTagPathException : ValidationException
    {
        public InvalidExtendedQueryTagPathException(string message)
            : base(message)
        {
        }
    }
}
