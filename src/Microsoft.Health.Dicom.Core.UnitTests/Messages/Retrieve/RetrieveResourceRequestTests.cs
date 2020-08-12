// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Messages.Retrieve
{
    public class RetrieveResourceRequestTests
    {
        [Fact]
        public void GivenRetrieveResourcesRequestForStudy_WhenConstructed_ThenStudyResourceTypeIsSet()
        {
            var request = new RetrieveResourceRequest(TestUidGenerator.Generate(), AcceptHeaderHelpers.CreateAcceptHeadersForGetInstance(transferSyntax: string.Empty));
            Assert.Equal(ResourceType.Study, request.ResourceType);
        }

        [Fact]
        public void GivenRetrieveResourcesRequestForSeries_WhenConstructed_ThenSeriesResourceTypeIsSet()
        {
            var request = new RetrieveResourceRequest(
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate(),
                AcceptHeaderHelpers.CreateAcceptHeadersForGetSeries(transferSyntax: string.Empty));
            Assert.Equal(ResourceType.Series, request.ResourceType);
        }

        [Fact]
        public void GivenRetrieveResourcesRequestForInstance_WhenConstructed_ThenInstanceResourceTypeIsSet()
        {
            var request = new RetrieveResourceRequest(
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate(),
                AcceptHeaderHelpers.CreateAcceptHeadersForGetInstance(transferSyntax: string.Empty));
            Assert.Equal(ResourceType.Instance, request.ResourceType);
        }

        [Fact]
        public void GivenRetrieveResourcesRequestForFrames_WhenConstructed_ThenFramesResourceTypeIsSet()
        {
            var request = new RetrieveResourceRequest(
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate(),
                new[] { 5 },
                AcceptHeaderHelpers.CreateAcceptHeadersForGetFrame(transferSyntax: string.Empty));
            Assert.Equal(ResourceType.Frames, request.ResourceType);
        }
    }
}
