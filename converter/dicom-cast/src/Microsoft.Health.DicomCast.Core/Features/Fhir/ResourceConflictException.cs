// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DicomCast.Core.Exceptions;

namespace Microsoft.Health.DicomCast.Core.Features.Fhir
{
    /// <summary>
    /// Exception thrown when resource cannot be created or updated because the resource has been updated.
    /// Currently do not have this extending fhir exception yet because fhir exceptions are not retryable where as this one is
    /// </summary>
    public class ResourceConflictException : RetryableException
    {
        public ResourceConflictException()
        {
        }
    }
}
