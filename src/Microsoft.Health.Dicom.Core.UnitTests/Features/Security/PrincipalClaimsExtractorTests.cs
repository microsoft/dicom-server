// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Security;
using NSubstitute;
using Xunit;
using Claim = System.Security.Claims.Claim;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Security
{
    public class PrincipalClaimsExtractorTests
    {
        private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
        private readonly IOptionsMonitor<SecurityConfiguration> _securityOptions = Substitute.For<IOptionsMonitor<SecurityConfiguration>>();
        private readonly SecurityConfiguration _securityConfiguration = Substitute.For<SecurityConfiguration>();
        private readonly ClaimsPrincipal _claimsPrincipal = Substitute.For<ClaimsPrincipal>();
        private readonly PrincipalClaimsExtractor _claimsIndexer;

        public PrincipalClaimsExtractorTests()
        {
            _securityOptions.CurrentValue.Returns(_securityConfiguration);
            _dicomRequestContextAccessor.DicomRequestContext.Principal.Returns(_claimsPrincipal);
            _claimsIndexer = new PrincipalClaimsExtractor(_dicomRequestContextAccessor, _securityOptions);
        }

        private static Claim Claim1 => new Claim("claim1", "value1");

        private static Claim Claim2 => new Claim("claim2", "value2");

        private static KeyValuePair<string, string> ExpectedValue1 => new KeyValuePair<string, string>("claim1", "value1");

        private static KeyValuePair<string, string> ExpectedValue2 => new KeyValuePair<string, string>("claim1", "value1");

        [Fact]
        public void GivenANullDicomContextAccessor_WhenInitializing_ThenExceptionShouldBeThrown()
        {
            Assert.Throws<ArgumentNullException>(
                "dicomRequestContextAccessor",
                () => new PrincipalClaimsExtractor(null, _securityOptions));
        }

        [Fact]
        public void GivenANullSecurityConfiguration_WhenInitializing_ThenExceptionShouldBeThrown()
        {
            Assert.Throws<ArgumentNullException>(
                "securityConfiguration",
                () => new PrincipalClaimsExtractor(new DicomRequestContextAccessor(), null));
        }

        [Fact]
        public void GivenANullPrincipal_WhenExtracting_ThenAnEmptyListShouldBeReturned()
        {
            _securityConfiguration.PrincipalClaims.Returns(new HashSet<string> { "claim1" });
            var result = _claimsIndexer.Extract();

            Assert.Empty(result);
        }

        [Fact]
        public void GivenAnEmptyListOfClaims_WhenExtracting_ThenAnEmptyListShouldBeReturned()
        {
            _securityConfiguration.PrincipalClaims.Returns(new HashSet<string> { "claim1" });
            _claimsPrincipal.Claims.Returns(new List<Claim>());

            var result = _claimsIndexer.Extract();

            Assert.Empty(result);
        }

        [Fact]
        public void GivenAnEmptyListOfLastModifiedClaims_WhenExtracting_ThenAnEmptyListShouldBeReturned()
        {
            _securityConfiguration.PrincipalClaims.Returns(new HashSet<string>());
            _claimsPrincipal.Claims.Returns(new List<Claim> { Claim1 });

            var result = _claimsIndexer.Extract();

            Assert.Empty(result);
        }

        [Fact]
        public void GivenAMismatchedListOfClaimsAndLastModifiedClaims_WhenExtracting_ThenAnEmptyListShouldBeReturned()
        {
            _securityConfiguration.PrincipalClaims.Returns(new HashSet<string> { "claim2" });
            _claimsPrincipal.Claims.Returns(new List<Claim> { Claim1 });

            var result = _claimsIndexer.Extract();

            Assert.Empty(result);
        }

        [Fact]
        public void GivenAMatchedListOfClaimsAndLastModifiedClaims_WhenExtracting_TheEntireSetShouldReturn()
        {
            _securityConfiguration.PrincipalClaims.Returns(new HashSet<string> { "claim1" });
            _claimsPrincipal.Claims.Returns(new List<Claim> { Claim1 });

            var result = _claimsIndexer.Extract();

            Assert.Contains(ExpectedValue1, result);
            Assert.Single(result);
        }

        [Fact]
        public void GivenAMatchedListOfClaimsAndLastModifiedClaimsWithMultipleDifferentClaims_WhenExtracting_TheEntireSetShouldReturn()
        {
            _securityConfiguration.PrincipalClaims.Returns(new HashSet<string> { "claim1", "claim2" });
            _claimsPrincipal.Claims.Returns(new List<Claim> { Claim1, Claim2 });

            var result = _claimsIndexer.Extract();

            Assert.Contains(ExpectedValue1, result);
            Assert.Contains(ExpectedValue2, result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GivenAMatchedListOfClaimsAndLastModifiedClaimsWithMultipleSimilar_WhenExtracting_TheEntireSetShouldReturn()
        {
            _securityConfiguration.PrincipalClaims.Returns(new HashSet<string> { "claim1" });
            _claimsPrincipal.Claims.Returns(new List<Claim> { Claim1, Claim1 });

            var result = _claimsIndexer.Extract();

            Assert.Contains(ExpectedValue1, result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GivenAPartiallyMatchedListOfClaimsAndLastModifiedClaims_WhenExtracting_ASubsetShouldReturn()
        {
            _securityConfiguration.PrincipalClaims.Returns(new HashSet<string> { "claim1", "claim3" });
            _claimsPrincipal.Claims.Returns(new List<Claim> { Claim1, Claim2 });

            var result = _claimsIndexer.Extract();

            Assert.Contains(ExpectedValue1, result);
            Assert.Single(result);
        }
    }
}
