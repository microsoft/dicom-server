// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
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
            : this(EnumUtility.GetLiteral(resourceType), resourceId)
        {
        }

        public ServerResourceId(string typeName, string resourceId)
        {
            EnsureArg.IsNotNullOrWhiteSpace(typeName, nameof(typeName));
            EnsureArg.IsNotNullOrWhiteSpace(resourceId, nameof(resourceId));

            TypeName = typeName;
            ResourceId = resourceId;
        }

        /// <summary>
        /// Gets the type name.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Gets the resource type.
        /// </summary>
        [Obsolete("Please use TypeName instead.")]
        public ResourceType ResourceType
        {
            get
            {
                ResourceType? resourceType = ModelInfo.FhirTypeNameToResourceType(TypeName);
                if (resourceType == null)
                {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.InvariantCulture, DicomCastCoreResource.UnknownResourceType, TypeName));
                }

                return resourceType.GetValueOrDefault();
            }
        }

        /// <summary>
        /// Gets the server generated resource id.
        /// </summary>
        public string ResourceId { get; }

        /// <inheritdoc/>
        public ResourceReference ToResourceReference()
        {
            _resourceReference ??= new ResourceReference(TypeName + "/" + ResourceId);

            return _resourceReference;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TypeName, ResourceId);
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
                return string.Equals(TypeName, other.TypeName, StringComparison.Ordinal) &&
                    string.Equals(ResourceId, other.ResourceId, StringComparison.Ordinal);
            }
        }

        public override string ToString()
            => ToResourceReference().Reference;
    }
}
