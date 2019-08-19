// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Messages.Delete;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Messages.Delete
{
    public class DeleteDicomResourcesRequestTests
    {
        [Fact]
        public void GivenRetrieveDicomResourcesRequest_OnConstruction_CorrectResourceTypeIsSet()
        {
            var request = new DeleteDicomResourcesRequest(Guid.NewGuid().ToString());
            Assert.Equal(ResourceType.Study, request.ResourceType);

            request = new DeleteDicomResourcesRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            Assert.Equal(ResourceType.Series, request.ResourceType);

            request = new DeleteDicomResourcesRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            Assert.Equal(ResourceType.Instance, request.ResourceType);
        }
    }
}
