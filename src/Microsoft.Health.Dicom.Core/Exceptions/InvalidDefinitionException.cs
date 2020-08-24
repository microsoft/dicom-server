// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// The exception that is thrown when provided definition is invalid.
    /// </summary>
    public class InvalidDefinitionException : DicomServerException
    {
        public InvalidDefinitionException(string message)
            : base(message)
        {
        }
    }
}
