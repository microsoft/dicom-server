// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Exception thrown when the validation fails.
    /// </summary>
    public class DatasetValidationException : ValidationException
    {
        public DatasetValidationException(ushort failureCode, string message)
            : this(failureCode, message, null)
        {
        }

        public DatasetValidationException(ushort failureCode, string message, Exception innerException)
            : base(message, innerException)
        {
            FailureCode = failureCode;
        }

        public ushort FailureCode { get; }
    }
}
