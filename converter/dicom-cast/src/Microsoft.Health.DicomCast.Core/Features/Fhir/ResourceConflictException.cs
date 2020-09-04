// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.DicomCast.Core.Features.Fhir
{
    /// <summary>
    /// Exception thrown when resource cannot be created or updated because the resource has been updated.
    /// </summary>
    public class ResourceConflictException : Exception
    {
        public ResourceConflictException()
        {
        }
    }
}
