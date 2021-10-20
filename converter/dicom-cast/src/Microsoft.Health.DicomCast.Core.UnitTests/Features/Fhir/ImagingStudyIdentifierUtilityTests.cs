// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Fhir
{
    public class ImagingStudyIdentifierUtilityTests
    {
        [Fact]
        public void GivenStudyInstanceUid_WhenCreated_ThenCorrectIdentifierShouldBeCreated()
        {
            var identifier = IdentifierUtility.CreateIdentifier("123");

            Assert.NotNull(identifier);
            Assert.Equal("urn:dicom:uid", identifier.System);
            Assert.Equal("urn:oid:123", identifier.Value);
        }
    }
}
