// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Messages.Retrieve
{
    public class RetrieveDicomMetadataRequestTests
    {
        [Fact]
        public void GivenRetrieveDicomMetadataRequest_OnConstruction_CorrectResourceTypeIsSet()
        {
            var request = new RetrieveDicomMetadataRequest(Guid.NewGuid().ToString());
            Assert.Equal(ResourceType.Study, request.ResourceType);

            request = new RetrieveDicomMetadataRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            Assert.Equal(ResourceType.Series, request.ResourceType);

            request = new RetrieveDicomMetadataRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            Assert.Equal(ResourceType.Instance, request.ResourceType);
        }
    }
}
