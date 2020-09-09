// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Represents a resource identifier.
    /// </summary>
    public interface IResourceId
    {
        /// <summary>
        /// Converts to resource identifier to <see cref="ResourceReference"/>.
        /// </summary>
        /// <returns>An instance of <see cref="ResourceReference"/>.</returns>
        ResourceReference ToResourceReference();
    }
}
