// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.DicomCast.Core.Features.Fhir
{
    /// <summary>
    /// Exception thrown when multiple resources matching the criteria.
    /// </summary>
    public class MultipleMatchingResourcesException : FhirNonRetryableException
    {
        public MultipleMatchingResourcesException(string resourceType)
            : base(FormatMessage(resourceType))
        {
            ResourceType = resourceType;
        }

        public string ResourceType { get; }

        private static string FormatMessage(string resourceType)
        {
            EnsureArg.IsNotNullOrWhiteSpace(resourceType, nameof(resourceType));

            return string.Format(DicomCastCoreResource.MultipleMatchingResourcesFound, resourceType);
        }
    }
}
