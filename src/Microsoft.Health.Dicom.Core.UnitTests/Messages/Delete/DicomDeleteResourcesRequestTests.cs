// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Delete;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Messages.Delete
{
    public class DicomDeleteResourcesRequestTests
    {
        [Fact]
        public void GivenDicomDeleteResourcesRequestForStudy_OnConstruction_StudyResourceTypeIsSet()
        {
            var request = new DicomDeleteResourcesRequest(TestUidGenerator.Generate());
            Assert.Equal(ResourceType.Study, request.ResourceType);
        }

        [Fact]
        public void GivenDicomDeleteResourcesRequestForSeries_OnConstruction_SeriesResourceTypeIsSet()
        {
            var request = new DicomDeleteResourcesRequest(TestUidGenerator.Generate(), TestUidGenerator.Generate());
            Assert.Equal(ResourceType.Series, request.ResourceType);
        }

        [Fact]
        public void GivenDicomDeleteResourcesRequestForInstance_OnConstruction_InstanceResourceTypeIsSet()
        {
            var request = new DicomDeleteResourcesRequest(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate());
            Assert.Equal(ResourceType.Instance, request.ResourceType);
        }
    }
}
