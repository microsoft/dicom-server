// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Fhir
{
    /// <summary>
    /// Provides functionality to validate a resource.
    /// </summary>
    public interface IFhirResourceValidator
    {
        /// <summary>
        /// Validates the <paramref name="resource"/>.
        /// </summary>
        /// <param name="resource">The resource to be validated.</param>
        void Validate(Resource resource);
    }
}
