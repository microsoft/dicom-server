// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Messages.Retrieve
{
    public class RetrieveDicomResourceRequestTests
    {
        [Fact]
        public void GivenRetrieveDicomResourcesRequestForStudy_OnConstruction_StudyResourceTypeIsSet()
        {
            var request = new RetrieveDicomResourceRequest(requestedTransferSyntax: string.Empty, DicomUID.Generate().UID);
            Assert.Equal(ResourceType.Study, request.ResourceType);
        }

        [Fact]
        public void GivenRetrieveDicomResourcesRequestForSeries_OnConstruction_SeriesResourceTypeIsSet()
        {
            var request = new RetrieveDicomResourceRequest(
                requestedTransferSyntax: string.Empty,
                DicomUID.Generate().UID,
                DicomUID.Generate().UID);
            Assert.Equal(ResourceType.Series, request.ResourceType);
        }

        [Fact]
        public void GivenRetrieveDicomResourcesRequestForInstance_OnConstruction_InstanceResourceTypeIsSet()
        {
            var request = new RetrieveDicomResourceRequest(
                requestedTransferSyntax: string.Empty,
                DicomUID.Generate().UID,
                DicomUID.Generate().UID,
                DicomUID.Generate().UID);
            Assert.Equal(ResourceType.Instance, request.ResourceType);
        }

        [Fact]
        public void GivenRetrieveDicomResourcesRequestForFrames_OnConstruction_FramesResourceTypeIsSet()
        {
            var request = new RetrieveDicomResourceRequest(
                requestedTransferSyntax: string.Empty,
                DicomUID.Generate().UID,
                DicomUID.Generate().UID,
                DicomUID.Generate().UID,
                new[] { 5 });
            Assert.Equal(ResourceType.Frames, request.ResourceType);
        }
    }
}
