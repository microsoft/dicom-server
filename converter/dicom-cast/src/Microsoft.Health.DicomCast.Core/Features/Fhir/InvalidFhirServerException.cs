// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DicomCast.Core.Features.Fhir
{
    /// <summary>
    /// Exception thrown when the FHIR server configuration is invalid.
    /// </summary>
    public class InvalidFhirServerException : FhirNonRetryableException
    {
        public InvalidFhirServerException(string message)
            : base(message)
        {
        }
    }
}
