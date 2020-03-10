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
    public class RetrieveDicomResourceRequestTests
    {
        [Fact]
        public void GivenRetrieveDicomResourcesRequestForStudy_OnConstruction_StudyResourceTypeIsSet()
        {
            var request = new RetrieveDicomResourceRequest(requestedTransferSyntax: string.Empty, TestUidGenerator.Generate());
            Assert.Equal(ResourceType.Study, request.ResourceType);
        }

        [Fact]
        public void GivenRetrieveDicomResourcesRequestForSeries_OnConstruction_SeriesResourceTypeIsSet()
        {
            var request = new RetrieveDicomResourceRequest(
                requestedTransferSyntax: string.Empty,
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate());
            Assert.Equal(ResourceType.Series, request.ResourceType);
        }

        [Fact]
        public void GivenRetrieveDicomResourcesRequestForInstance_OnConstruction_InstanceResourceTypeIsSet()
        {
            var request = new RetrieveDicomResourceRequest(
                requestedTransferSyntax: string.Empty,
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate());
            Assert.Equal(ResourceType.Instance, request.ResourceType);
        }

        [Fact]
        public void GivenRetrieveDicomResourcesRequestForFrames_OnConstruction_FramesResourceTypeIsSet()
        {
            var request = new RetrieveDicomResourceRequest(
                requestedTransferSyntax: string.Empty,
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate(),
                new[] { 5 });
            Assert.Equal(ResourceType.Frames, request.ResourceType);
        }
    }
}
