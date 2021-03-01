// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Represents server generated resource identifier.
    /// </summary>
    public class ServerResourceId : IResourceId, IEquatable<ServerResourceId>
    {
        private ResourceReference _resourceReference;

        public ServerResourceId(ResourceType resourceType, string resourceId)
        {
            EnsureArg.EnumIsDefined(resourceType, nameof(resourceType));
            EnsureArg.IsNotNullOrWhiteSpace(resourceId, nameof(resourceId));

            ResourceType = resourceType;
            ResourceId = resourceId;
        }

        /// <summary>
        /// Gets the resource type.
        /// </summary>
        public ResourceType ResourceType { get; }

        /// <summary>
        /// Gets the server generated resource id.
        /// </summary>
        public string ResourceId { get; }

        /// <inheritdoc/>
        public ResourceReference ToResourceReference()
        {
            _resourceReference ??= new ResourceReference($"{ResourceType.GetLiteral()}/{ResourceId}");

            return _resourceReference;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ResourceType, ResourceId);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ServerResourceId);
        }

        public bool Equals(ServerResourceId other)
        {
            if (other == null)
            {
                return false;
            }
            else if (ReferenceEquals(this, other))
            {
                return true;
            }
            else
            {
                return ResourceType == other.ResourceType &&
                    string.Equals(ResourceId, other.ResourceId, StringComparison.Ordinal);
            }
        }

        public override string ToString()
            => ToResourceReference().Reference;
    }
}
