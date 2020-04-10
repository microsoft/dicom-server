// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Exceptions
{
    public class DicomInstanceMetadataNotFoundExceptionTests
    {
        [Fact]
        public void GivenDicomInstanceMetadataNotFoundException_WhenCreated_ThenNotFoundStatusCodeIsSet()
        {
            var exception = new DicomInstanceNotFoundException();
            Assert.Equal(HttpStatusCode.NotFound, exception.ResponseStatusCode);
        }
    }
}
