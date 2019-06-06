// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Routing
{
    public class DicomRouteProviderTests
    {
        private static readonly Uri ValidBaseUri = new Uri("http://localhost:8080");
        private readonly IDicomRouteProvider _routeProvider;

        public DicomRouteProviderTests()
        {
            _routeProvider = new DicomRouteProvider();
        }

        [Fact]
        public void GivenARouteProvider_ValidArgumentExceptionsThrownCorrectly()
        {
            string validInstanceUID1 = Guid.NewGuid().ToString();
            string validInstanceUID2 = Guid.NewGuid().ToString();
            string validInstanceUID3 = Guid.NewGuid().ToString();

            Assert.Throws<ArgumentNullException>(() => _routeProvider.GetStudyUri(null, validInstanceUID1));
            Assert.Throws<ArgumentNullException>(() => _routeProvider.GetStudyUri(ValidBaseUri, null));
            Assert.Throws<ArgumentException>(() => _routeProvider.GetStudyUri(ValidBaseUri, string.Empty));

            Assert.Throws<ArgumentNullException>(() => _routeProvider.GetSeriesUri(null, validInstanceUID1, validInstanceUID2));
            Assert.Throws<ArgumentNullException>(() => _routeProvider.GetSeriesUri(ValidBaseUri, null, validInstanceUID2));
            Assert.Throws<ArgumentException>(() => _routeProvider.GetSeriesUri(ValidBaseUri, string.Empty, validInstanceUID2));
            Assert.Throws<ArgumentNullException>(() => _routeProvider.GetSeriesUri(ValidBaseUri, validInstanceUID1, null));
            Assert.Throws<ArgumentException>(() => _routeProvider.GetSeriesUri(ValidBaseUri, validInstanceUID1, string.Empty));
            Assert.Throws<ArgumentException>(() => _routeProvider.GetSeriesUri(ValidBaseUri, validInstanceUID1, validInstanceUID1));

            Assert.Throws<ArgumentNullException>(() => _routeProvider.GetInstanceUri(null, validInstanceUID1, validInstanceUID2, validInstanceUID3));
            Assert.Throws<ArgumentNullException>(() => _routeProvider.GetInstanceUri(ValidBaseUri, null, validInstanceUID2, validInstanceUID3));
            Assert.Throws<ArgumentException>(() => _routeProvider.GetInstanceUri(ValidBaseUri, string.Empty, validInstanceUID2, validInstanceUID3));
            Assert.Throws<ArgumentNullException>(() => _routeProvider.GetInstanceUri(ValidBaseUri, validInstanceUID1, null, validInstanceUID3));
            Assert.Throws<ArgumentException>(() => _routeProvider.GetInstanceUri(ValidBaseUri, validInstanceUID1, string.Empty, validInstanceUID3));
            Assert.Throws<ArgumentException>(() => _routeProvider.GetInstanceUri(ValidBaseUri, validInstanceUID1, validInstanceUID1, validInstanceUID3));
            Assert.Throws<ArgumentNullException>(() => _routeProvider.GetInstanceUri(ValidBaseUri, validInstanceUID1, validInstanceUID2, null));
            Assert.Throws<ArgumentException>(() => _routeProvider.GetInstanceUri(ValidBaseUri, validInstanceUID1, validInstanceUID2, string.Empty));
            Assert.Throws<ArgumentException>(() => _routeProvider.GetInstanceUri(ValidBaseUri, validInstanceUID1, validInstanceUID2, validInstanceUID1));
        }

        [Fact]
        public void GivenStudyUID_WhenCreatingTheRouteUri_ThenNoExceptionShouldBeThrown()
        {
            string studyInstanceUID = "test";
            var expectedResult = new Uri(ValidBaseUri, $"/studies/{studyInstanceUID}");
            Uri route = _routeProvider.GetStudyUri(ValidBaseUri, studyInstanceUID);
            Assert.Equal(expectedResult, route);
        }
    }
}
