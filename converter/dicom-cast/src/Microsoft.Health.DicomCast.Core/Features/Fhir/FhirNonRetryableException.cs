// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.DicomCast.Core.Features.Fhir
{
    public class FhirNonRetryableException : Exception
    {
        protected FhirNonRetryableException(string message)
            : base(message)
        {
        }

        protected FhirNonRetryableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
