// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when preconditation failed.
    /// </summary>
    public class PreconditionFailedException : ValidationException
    {
        public PreconditionFailedException(string message)
            : base(message)
        {
        }
    }
}
