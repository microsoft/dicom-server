// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Base class for all client input validation exceptions.
    /// </summary>
    public abstract class ValidationException : DicomServerException
    {
        protected ValidationException(string message)
            : base(message)
        {
        }

        protected ValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
