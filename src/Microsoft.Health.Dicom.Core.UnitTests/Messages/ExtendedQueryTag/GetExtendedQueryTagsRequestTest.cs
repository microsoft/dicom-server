// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Messages.ExtendedQueryTag
{
    public class GetExtendedQueryTagsRequestTest
    {
        [Fact]
        public void GivenInvalidParameters_WhenCreateRequest_ThenThrowArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GetExtendedQueryTagsRequest(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GetExtendedQueryTagsRequest(201, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GetExtendedQueryTagsRequest(10, -12));
        }
    }
}
