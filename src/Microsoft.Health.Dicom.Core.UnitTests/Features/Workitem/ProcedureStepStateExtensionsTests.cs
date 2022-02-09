// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Workitem;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem
{
    public sealed class ProcedureStepStateExtensionsTests
    {
        [Fact]
        public void GivenGetTransitionState_WhenCurrentStateIsNone_ReturnsScheduledAsFutureState()
        {
            var result = ProcedureStepState.None.GetTransitionState(WorkitemStateEvents.NCreate);
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
            var result = state.GetTransitionState(WorkitemStateEvents.NCreate);
            Assert.True(result.IsError);
            Assert.Equal("0111", result.Code);
        }

        [Theory]
        [InlineData(ProcedureStepState.None, @"")]
        [InlineData(ProcedureStepState.Scheduled, @"SCHEDULED")]
        [InlineData(ProcedureStepState.InProgress, @"IN PROGRESS")]
        [InlineData(ProcedureStepState.Completed, @"COMPLETED")]
        [InlineData(ProcedureStepState.Canceled, @"CANCELED")]
        public void GivenGetStringValue_WhenStateIsValid_ReturnsMatchingStringValue(ProcedureStepState state, string expectedValue)
        {
            var actual = state.GetStringValue();
            Assert.Equal(expectedValue, actual);
        }
    }
}
