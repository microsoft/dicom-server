// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Workitem;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem;

public sealed class ProcedureStepStateExtensionsTests
{
    [Fact]
    public void GivenGetTransitionState_WhenCurrentStateIsNone_ReturnsScheduledAsFutureState()
    {
        var result = ProcedureStepState.None.GetTransitionState(WorkitemActionEvent.NCreate);
        Assert.False(result.IsError);
        Assert.Equal(ProcedureStepState.Scheduled, result.State);
    }

    [Theory]
    [InlineData(ProcedureStepState.Scheduled)]
    [InlineData(ProcedureStepState.InProgress)]
    [InlineData(ProcedureStepState.Canceled)]
    [InlineData(ProcedureStepState.Completed)]
    public void GivenGetTransitionState_WhenCurrentStateAndFutureStateAreSame_ReturnsErrorTrue(ProcedureStepState state)
    {
        var result = state.GetTransitionState(WorkitemActionEvent.NCreate);
        Assert.True(result.IsError);
        Assert.Equal("0111", result.Code);
    }

    [Theory]
    [InlineData(ProcedureStepState.None, ProcedureStepStateConstants.None)]
    [InlineData(ProcedureStepState.Scheduled, ProcedureStepStateConstants.Scheduled)]
    [InlineData(ProcedureStepState.InProgress, ProcedureStepStateConstants.InProgress)]
    [InlineData(ProcedureStepState.Completed, ProcedureStepStateConstants.Completed)]
    [InlineData(ProcedureStepState.Canceled, ProcedureStepStateConstants.Canceled)]
    public void GivenGetStringValue_WhenStateIsValid_ReturnsMatchingStringValue(ProcedureStepState state, string expectedValue)
    {
        var actual = state.GetStringValue();
        Assert.Equal(expectedValue, actual);
    }

    [Theory]
    [InlineData(ProcedureStepStateConstants.None, ProcedureStepState.None)]
    [InlineData(ProcedureStepStateConstants.Scheduled, ProcedureStepState.Scheduled)]
    [InlineData(ProcedureStepStateConstants.InProgress, ProcedureStepState.InProgress)]
    [InlineData(ProcedureStepStateConstants.Completed, ProcedureStepState.Completed)]
    [InlineData(ProcedureStepStateConstants.Canceled, ProcedureStepState.Canceled)]
    public void GivenGetProcedureStepState_WhenValidStringValueIsPassed_ReturnsMatchingProcedureStepState(string stringValue, ProcedureStepState expectedState)
    {
        var actual = ProcedureStepStateExtensions.GetProcedureStepState(stringValue);
        Assert.Equal(expectedState, actual);
    }
}
