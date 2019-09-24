// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Delete;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Messages.Delete
{
    public class DeleteDicomResourcesRequestTests
    {
        [Fact]
        public void GivenDeleteDicomResourcesRequestForStudy_OnConstruction_StudyResourceTypeIsSet()
        {
            var request = new DeleteDicomResourcesRequest(DicomUID.Generate().UID);
            Assert.Equal(ResourceType.Study, request.ResourceType);
        }

        [Fact]
        public void GivenDeleteDicomResourcesRequestForSeries_OnConstruction_SeriesResourceTypeIsSet()
        {
            var request = new DeleteDicomResourcesRequest(DicomUID.Generate().UID, DicomUID.Generate().UID);
            Assert.Equal(ResourceType.Series, request.ResourceType);
        }

        [Fact]
        public void GivenDeleteDicomResourcesRequestForInstance_OnConstruction_InstanceResourceTypeIsSet()
        {
            var request = new DeleteDicomResourcesRequest(DicomUID.Generate().UID, DicomUID.Generate().UID, DicomUID.Generate().UID);
            Assert.Equal(ResourceType.Instance, request.ResourceType);
        }
    }
}
