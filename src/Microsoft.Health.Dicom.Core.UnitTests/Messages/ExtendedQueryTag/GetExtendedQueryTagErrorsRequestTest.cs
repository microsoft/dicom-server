// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Messages.ExtendedQueryTag
{
    public class GetExtendedQueryTagErrorsRequestTest
    {
        [Fact]
        public void GivenInvalidParameters_WhenCreateRequest_ThenThrowArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GetExtendedQueryTagErrorsRequest("foo", 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GetExtendedQueryTagErrorsRequest("bar", 201, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GetExtendedQueryTagErrorsRequest("baz", 10, -12));
        }
    }
}
