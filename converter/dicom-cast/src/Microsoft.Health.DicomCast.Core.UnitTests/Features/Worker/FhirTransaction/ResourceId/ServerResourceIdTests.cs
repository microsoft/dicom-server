// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction.ResourceId
{
    public class ServerResourceIdTests
    {
        private const ResourceType DefaultResourceType = ResourceType.Patient;
        private const string DefaultResourceId = "r123";
        private const string ResourceReference = "Patient/r123";

        private static readonly ServerResourceId ServerResourceId = new ServerResourceId(DefaultResourceType, DefaultResourceId);

        public static IEnumerable<object[]> GetDifferentServerResourceIdCombinations()
        {
            yield return new object[] { ResourceType.Practitioner, DefaultResourceId };
            yield return new object[] { DefaultResourceType, "r12" };
            yield return new object[] { DefaultResourceType, "R123" };
        }

        [Fact]
        public void GivenTheSystem_WhenNewServerResourceIdIsGenerated_ThenCorrectValuesShouldBeSet()
        {
            Assert.Equal(EnumUtility.GetLiteral(ResourceType.Patient), ServerResourceId.TypeName);
            Assert.Equal(DefaultResourceId, ServerResourceId.ResourceId);
        }

        [Fact]
        public void GivenAServerResourceId_WhenConvertedToResourceReference_ThenCorrectResourceReferenceShouldBeCreated()
        {
            ResourceReference resourceReference = ServerResourceId.ToResourceReference();

            Assert.NotNull(resourceReference);
            Assert.Equal(ResourceReference, resourceReference.Reference);
        }

        [Fact]
        public void GivenSameServerResourceId_WhenHashCodeIsComputed_ThenTheSameHashCodeShouldBeGenerated()
        {
            var newServerResourceId = new ServerResourceId(DefaultResourceType, DefaultResourceId);

            Assert.Equal(ServerResourceId.GetHashCode(), newServerResourceId.GetHashCode());
        }

        [Theory]
        [MemberData(nameof(GetDifferentServerResourceIdCombinations))]
        public void GivenDifferentServerResourceId_WhenHashCodeIsComputed_ThenHashCodeShouldBeDifferent(ResourceType resourceType, string resourceId)
        {
            var newServerResourceId = new ServerResourceId(resourceType, resourceId);

            Assert.NotEqual(ServerResourceId.GetHashCode(), newServerResourceId.GetHashCode());
        }

        [Fact]
        public void GivenAServerResourceId_WhenCheckingEqualToNullUsingObjectEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(ServerResourceId.Equals((object)null));
        }

        [Fact]
        public void GivenAServerResourceId_WhenCheckingEqualToSameServerResourceIdUsingObjectEquals_ThenFalseShouldBeReturned()
        {
            Assert.True(ServerResourceId.Equals((object)ServerResourceId));
        }

        [Fact]
        public void GivenAServerResourceId_WhenCheckingEqualToSameServerResourceIdUsingObjectEquals_ThenTrueShouldBeReturned()
        {
            Assert.True(ServerResourceId.Equals((object)new ServerResourceId(DefaultResourceType, DefaultResourceId)));
        }

        [Theory]
        [MemberData(nameof(GetDifferentServerResourceIdCombinations))]
        public void GivenAServerResourceId_WhenCheckingEqualToDifferentServerResourceIdUsingObjectEquals_ThenFalseShouldBeReturned(ResourceType resourceType, string resourceId)
        {
            Assert.False(ServerResourceId.Equals((object)new ServerResourceId(resourceType, resourceId)));
        }

        [Fact]
        public void GivenAServerResourceId_WhenCheckingEqualToNullUsingIEquatableEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(ServerResourceId.Equals(null));
        }

        [Fact]
        public void GivenAServerResourceId_WhenCheckingEqualToSameServerResourceIdUsingIEquatableEquals_ThenFalseShouldBeReturned()
        {
            Assert.True(ServerResourceId.Equals(ServerResourceId));
        }

        [Fact]
        public void GivenAServerResourceId_WhenCheckingEqualToSameServerResourceIdUsingIEquatableEquals_ThenTrueShouldBeReturned()
        {
            Assert.True(ServerResourceId.Equals(new ServerResourceId(DefaultResourceType, DefaultResourceId)));
        }

        [Theory]
        [MemberData(nameof(GetDifferentServerResourceIdCombinations))]
        public void GivenAServerResourceId_WhenCheckingEqualToDifferentServerResourceIdUsingIEquatableEquals_ThenFalseShouldBeReturned(ResourceType resourceType, string resourceId)
        {
            Assert.False(ServerResourceId.Equals(new ServerResourceId(resourceType, resourceId)));
        }

        [Fact]
        public void GivenAServerResourceId_WhenConvertedToString_ThenCorrectValueShouldBeReturned()
        {
            Assert.Equal(ResourceReference, ServerResourceId.ToString());
        }
    }
}
