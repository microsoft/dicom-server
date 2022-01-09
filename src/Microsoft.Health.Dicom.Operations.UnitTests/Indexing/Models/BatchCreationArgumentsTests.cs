// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Operations.Indexing.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Operations.UnitTests.Indexing.Models
{
    public class BatchCreationArgumentsTests
    {
        [Fact]
        public void GivenBadValues_WhenContructing_ThenThrowExceptions()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BatchCreationArguments(-1, 2, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BatchCreationArguments(1, -2, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BatchCreationArguments(1, 2, -3));
        }
    }
}
