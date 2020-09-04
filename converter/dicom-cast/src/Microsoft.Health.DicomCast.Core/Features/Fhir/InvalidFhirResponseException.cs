// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.DicomCast.Core.Features.Fhir
{
    /// <summary>
    /// Exception thrown when the FHIR response is invalid.
    /// </summary>
    public class InvalidFhirResponseException : Exception
    {
        public InvalidFhirResponseException(string message)
            : base(message)
        {
        }
    }
}
