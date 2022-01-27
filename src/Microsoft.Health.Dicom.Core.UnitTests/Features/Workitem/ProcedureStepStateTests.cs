// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

//using Microsoft.Health.Dicom.Core.Features.Workitem;
//using Xunit;

//namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem
//{
//    public sealed class ProcedureStepStateTests
//    {
//        //[Fact]
//        //public void GivenCanTransition_WhenCurrentStateIsNullAndFutureStateIsScheduled_ReturnsTrue()
//        //{
//        //    Assert.True(ProcedureStepState.CanTransition(null, ProcedureStepState.Scheduled));
//        //}

//        //[Fact]
//        //public void GivenCanTransition_WhenCurrentStateIsInvalid_ReturnsFalse()
//        //{
//        //    Assert.False(ProcedureStepState.CanTransition(@"Something", ProcedureStepState.Scheduled));
//        //}

//        //[Fact]
//        //public void GivenCanTransition_WhenFutureStateIsInvalid_ReturnsFalse()
//        //{
//        //    Assert.False(ProcedureStepState.CanTransition(ProcedureStepState.Scheduled, @"Finished"));
//        //}

//        //[Fact]
//        //public void GivenCanTransition_WhenCurrentStateAndFutureStateAreSame_ReturnsTrue()
//        //{
//        //    Assert.True(ProcedureStepState.CanTransition(ProcedureStepState.Scheduled, ProcedureStepState.Scheduled));
//        //    Assert.True(ProcedureStepState.CanTransition(ProcedureStepState.InProgress, ProcedureStepState.InProgress));
//        //    Assert.True(ProcedureStepState.CanTransition(ProcedureStepState.Completed, ProcedureStepState.Completed));
//        //    Assert.True(ProcedureStepState.CanTransition(ProcedureStepState.Canceled, ProcedureStepState.Canceled));
//        //}

//        //[Fact]
//        //public void GivenCanTransition_WhenCurrentStateIsNotNullAndFutureStateIsScheduled_ReturnsFalse()
//        //{
//        //    Assert.False(ProcedureStepState.CanTransition(ProcedureStepState.InProgress, ProcedureStepState.Scheduled));
//        //    Assert.False(ProcedureStepState.CanTransition(ProcedureStepState.Canceled, ProcedureStepState.Scheduled));
//        //    Assert.False(ProcedureStepState.CanTransition(ProcedureStepState.Completed, ProcedureStepState.Scheduled));
//        //}

//        //[Fact]
//        //public void GivenCanTransition_WhenCurrentStateIsScheduledAndFutureStateIsInProgress_ReturnsTrue()
//        //{
//        //    Assert.True(ProcedureStepState.CanTransition(ProcedureStepState.Scheduled, ProcedureStepState.InProgress));
//        //}

//        //[Fact]
//        //public void GivenCanTransition_WhenCurrentStateIsScheduledAndFutureStateIsCompleted_ReturnsFalse()
//        //{
//        //    Assert.False(ProcedureStepState.CanTransition(ProcedureStepState.Scheduled, ProcedureStepState.Completed));
//        //}

//        //[Fact]
//        //public void GivenCanTransition_WhenCurrentStateIsScheduledAndFutureStateIsCanceled_ReturnsFalse()
//        //{
//        //    Assert.False(ProcedureStepState.CanTransition(ProcedureStepState.Scheduled, ProcedureStepState.Canceled));
//        //}

//        //[Fact]
//        //public void GivenCanTransition_WhenCurrentStateIsNotScheduledAndFutureStateIsInProgress_ReturnsFalse()
//        //{
//        //    Assert.False(ProcedureStepState.CanTransition(ProcedureStepState.Canceled, ProcedureStepState.InProgress));
//        //    Assert.False(ProcedureStepState.CanTransition(ProcedureStepState.Completed, ProcedureStepState.InProgress));
//        //}
//    }
//}
