// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Fhir
{
    /// <summary>
    /// Provides functionality to validate a resource.
    /// </summary>
    public class FhirResourceValidator : IFhirResourceValidator
    {
        /// <inheritdoc/>
        public void Validate(Resource resource)
        {
            EnsureArg.IsNotNull(resource, nameof(resource));

            if (string.IsNullOrWhiteSpace(resource.Id))
            {
                throw new FhirResourceValidationException(DicomCastCoreResource.InvalidFhirResourceMissingId);
            }

            if (string.IsNullOrWhiteSpace(resource.Meta?.VersionId))
            {
                throw new FhirResourceValidationException(DicomCastCoreResource.InvalidFhirResourceMissingVersionId);
            }
        }
    }
}
