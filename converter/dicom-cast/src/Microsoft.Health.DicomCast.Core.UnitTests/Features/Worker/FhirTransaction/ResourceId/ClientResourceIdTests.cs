// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction.ResourceId
{
    public class ClientResourceIdTests
    {
        private const string Prefix = "urn:uuid:";

        private readonly ClientResourceId _clientResourceId = new ClientResourceId();

        [Fact]
        public void GivenTheSystem_WhenClientIdIsGenerated_ThenIdShouldHaveCorrectFormat()
        {
            Assert.StartsWith(Prefix, _clientResourceId.Id);

            string guid = _clientResourceId.Id.Substring(Prefix.Length);

            Assert.True(Guid.TryParse(guid, out Guid _));
        }

        [Fact]
        public void GivenAClientResourceId_WhenConvertedToResourceReference_ThenCorrectResourceReferenceShouldBeCreated()
        {
            ResourceReference resourceReference = _clientResourceId.ToResourceReference();

            Assert.NotNull(resourceReference);
            Assert.Equal(_clientResourceId.Id, resourceReference.Reference);
        }

        [Fact]
        public void GivenDifferentClientResourceId_WhenHashCodeIsComputed_ThenHashCodeShouldBeDifferent()
        {
            var newClientResourceId = new ClientResourceId();

            Assert.NotEqual(_clientResourceId.GetHashCode(), newClientResourceId.GetHashCode());
        }

        [Fact]
        public void GivenAClientResourceId_WhenCheckingEqualToNullUsingObjectEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(_clientResourceId.Equals((object)null));
        }

        [Fact]
        public void GivenAClientResourceId_WhenCheckingEqualToSameClientResourceIdUsingObjectEquals_ThenTrueShouldBeReturned()
        {
            Assert.True(_clientResourceId.Equals((object)_clientResourceId));
        }

        [Fact]
        public void GivenAClientResourceId_WhenCheckingEqualToDifferentClientResourceIdUsingObjectEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(_clientResourceId.Equals((object)new ClientResourceId()));
        }

        [Fact]
        public void GivenAClientResourceId_WhenCheckingEqualToNullUsingIEquatableEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(_clientResourceId.Equals(null));
        }

        [Fact]
        public void GivenAClientResourceId_WhenCheckingEqualToSameClientResourceIdUsingIEquatableEquals_ThenTrueShouldBeReturned()
        {
            Assert.True(_clientResourceId.Equals(_clientResourceId));
        }

        [Fact]
        public void GivenAClientResourceId_WhenCheckingEqualToDifferentClientResourceIdUsingIEquatableEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(_clientResourceId.Equals(new ClientResourceId()));
        }

        [Fact]
        public void GivenAClientResourceId_WhenConvertedToString_ThenCorrectValueShouldBeReturned()
        {
            Assert.Equal(_clientResourceId.Id, _clientResourceId.ToString());
        }
    }
}
