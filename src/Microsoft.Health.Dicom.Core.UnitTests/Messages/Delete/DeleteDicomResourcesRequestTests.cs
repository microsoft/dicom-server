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
    public class DeleteDicomResourcesRequestTests
    {
        [Fact]
        public void GivenDeleteDicomResourcesRequestForStudy_OnConstruction_StudyResourceTypeIsSet()
        {
            var request = new DeleteDicomResourcesRequest(TestUidGenerator.Generate());
            Assert.Equal(ResourceType.Study, request.ResourceType);
        }

        [Fact]
        public void GivenDeleteDicomResourcesRequestForSeries_OnConstruction_SeriesResourceTypeIsSet()
        {
            var request = new DeleteDicomResourcesRequest(TestUidGenerator.Generate(), TestUidGenerator.Generate());
            Assert.Equal(ResourceType.Series, request.ResourceType);
        }

        [Fact]
        public void GivenDeleteDicomResourcesRequestForInstance_OnConstruction_InstanceResourceTypeIsSet()
        {
            var request = new DeleteDicomResourcesRequest(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate());
            Assert.Equal(ResourceType.Instance, request.ResourceType);
        }
    }
}
