// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Core.Messages.Store;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Extensions
{
    public class StoreResponseStatusExtensionsTests
    {
        public static IEnumerable<object[]> GetStatusMapping()
        {
            yield return new object[] { StoreResponseStatus.None, HttpStatusCode.NoContent };
            yield return new object[] { StoreResponseStatus.Success, HttpStatusCode.OK };
            yield return new object[] { StoreResponseStatus.PartialSuccess, HttpStatusCode.Accepted };
            yield return new object[] { StoreResponseStatus.Failure, HttpStatusCode.Conflict };
        }

        [Theory]
        [MemberData(nameof(GetStatusMapping))]
        public void GivenStatus_WhenConvertedToHttpStatusCode_ThenCorrectStatusCodeShouldBeReturned(StoreResponseStatus status, HttpStatusCode expectedStatusCode)
        {
            Assert.Equal(expectedStatusCode, status.ToHttpStatusCode());
        }

        [Fact]
        public void GivenStatus_WhenConvertingToHttpStatusCode_ThenMappingShouldExistForEachStatus()
        {
            // This test makes sure all of the mapping exists to prevent new status being added without new mapping.
            Assert.Equal(
                GetStatusMapping().Select(mapping => (StoreResponseStatus)mapping[0]),
                Enum.GetValues(typeof(StoreResponseStatus)).Cast<StoreResponseStatus>());
        }
    }
}
