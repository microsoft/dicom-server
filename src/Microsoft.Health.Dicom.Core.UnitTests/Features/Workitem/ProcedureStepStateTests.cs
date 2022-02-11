// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem
{
    public sealed class ProcedureStepStateTests
    {
        [Fact]
        public void GivenGetTransitionState_WhenCurrentStateIsNull_ReturnsScheduledAsFutureState()
        {
            var result = ProcedureStepState.GetTransitionState(WorkitemStateEvents.NCreate, null);
            Assert.False(result.IsError);
            Assert.Equal(ProcedureStepState.Scheduled, result.State);
        }

        [Fact]
        public void GivenGetTransitionState_WhenCurrentStateIsInvalid_ReturnsFalse()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                ProcedureStepState.GetTransitionState(WorkitemStateEvents.NCreate, "Foo");
            });
        }

        [Theory]
        [InlineData(ProcedureStepState.Scheduled)]
        [InlineData(ProcedureStepState.InProgress)]
        [InlineData(ProcedureStepState.Canceled)]
        [InlineData(ProcedureStepState.Completed)]
        public void GivenGetTransitionState_WhenCurrentStateAndFutureStateAreSame_ReturnsTrue(string state)
        {
            var result = ProcedureStepState.GetTransitionState(WorkitemStateEvents.NCreate, state);
            Assert.True(result.IsError);
            Assert.Equal("0111", result.Code);
        }
    }
}
