// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Represents a client generated resource identifier.
    /// </summary>
    public class ClientResourceId : IResourceId, IEquatable<ClientResourceId>
    {
        private ResourceReference _resourceReference;

        public ClientResourceId()
        {
            Id = $"urn:uuid:{Guid.NewGuid()}";
        }

        /// <summary>
        /// Gets the client generated resource identifier.
        /// </summary>
        public string Id { get; }

        /// <inheritdoc/>
        public ResourceReference ToResourceReference()
        {
            _resourceReference ??= new ResourceReference(Id);

            return _resourceReference;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ClientResourceId);
        }

        public bool Equals(ClientResourceId other)
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
                return string.Equals(Id, other.Id, StringComparison.Ordinal);
            }
        }

        public override string ToString()
            => Id;
    }
}
