// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem
{
    public sealed class WorkitemFinalStateValidatorExtensionTests
    {
        [Fact]
        public void GivenValidateFinalStateRequirement_WhenProcedureStepStateIsNotCanceledOrCompleted_ThenNoErrorsThrown()
        {
            var dataset = Samples.CreateRandomWorkitemInstanceDataset();

            dataset.AddOrUpdate(DicomTag.ProcedureStepState, ProcedureStepStateConstants.Scheduled);

            WorkitemFinalStateValidatorExtension.ValidateFinalStateRequirement(dataset);
        }


    }
}
