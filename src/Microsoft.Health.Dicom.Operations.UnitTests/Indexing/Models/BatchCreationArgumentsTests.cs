// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Operations.Indexing;
using Microsoft.Health.Dicom.Operations.Indexing.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Operations.UnitTests.Indexing.Models
{
    public class BatchCreationArgumentsTests
    {
        [Fact]
        public void GivenBadValues_WhenContructing_ThenThrowExceptions()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BatchCreationArguments(1, -2, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BatchCreationArguments(1, 2, -3));
        }

        [Fact]
        public void GivenValues_WhenConstructing_ThenAssignProperties()
        {
            var actual = new BatchCreationArguments(1, 2, 3);
            Assert.Equal(1, actual.MaxWatermark);
            Assert.Equal(2, actual.BatchSize);
            Assert.Equal(3, actual.MaxParallelBatches);
        }

        [Fact]
        public void GivenOptions_WhenCreatingFromOptions_ThenAssignProperties()
        {
            var actual = BatchCreationArguments.FromOptions(
                1,
                new QueryTagIndexingOptions
                {
                    BatchSize = 2,
                    MaxParallelBatches = 3,
                });

            Assert.Equal(1, actual.MaxWatermark);
            Assert.Equal(2, actual.BatchSize);
            Assert.Equal(3, actual.MaxParallelBatches);
        }
    }
}
