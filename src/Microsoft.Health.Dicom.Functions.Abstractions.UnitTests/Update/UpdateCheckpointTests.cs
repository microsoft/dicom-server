// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Dicom.Functions.Update;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Update;

public class UpdateCheckpointTests
{
    [Fact]
    public void GivenEmptyInput_WhenGettingPercentComplete_ThenReturnZero()
        => Assert.Equal(0, new UpdateCheckpoint().PercentComplete);

    [Theory]
    [InlineData(4, 4, 100)]
    [InlineData(4, 3, 75)]
    [InlineData(4, 2, 50)]
    [InlineData(4, 0, 0)]
    public void GivenUpdateInput_WhenGettingPercentComplete_ThenReturnComputedProgress(int total, int processed, int expected)
        => Assert.Equal(expected, new UpdateCheckpoint { StudyInstanceUids = Enumerable.Repeat<string>(".", total).ToList(), NumberOfStudyProcessed = processed }.PercentComplete);

    [Fact]
    public void GivenCheckpoint_WhenRetrievingAdditionalProperties_ThenGetOperationSpecificValues()
    {
        var checkpoint = new UpdateCheckpoint
        {
            NumberOfStudyProcessed = 5,
            NumberOfStudyCompleted = 4,
            TotalNumberOfInstanceUpdated = 20,
            Errors = new List<string>()
        };

        UpdateResult results = checkpoint.GetResults(null) as UpdateResult;
        Assert.NotNull(results);
        Assert.Equal(20, results.InstanceUpdated);
        Assert.Equal(5, results.StudyProcessed);
        Assert.Equal(4, results.StudyUpdated);
        Assert.Equal(new List<string>(), results.Errors);
    }
}
