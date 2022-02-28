// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem
{
    public class CancelWorkitemDatasetValidatorTests
    {
        [Fact]
        public void GivenMissingRequiredTag_Throws()
        {
            var dataset = Samples.CreateCanceledWorkitemDataset(@"Unit Test Reason", ProcedureStepState.Canceled);
            dataset.Remove(DicomTag.SOPInstanceUID);

            var target = new CancelWorkitemDatasetValidator();

            Assert.Throws<DatasetValidationException>(() => target.Validate(dataset));
        }

        [Fact]
        public void GivenMissingConditionalRequiredTag_Throws()
        {
            var dataset = Samples.CreateCanceledWorkitemDataset(@"Unit Test Reason", ProcedureStepState.Canceled);
            dataset.Remove(DicomTag.SpecificCharacterSet);

            var target = new CancelWorkitemDatasetValidator();

            Assert.Throws<DatasetValidationException>(() => target.Validate(dataset));
        }

        [Fact]
        public void GivenMissingPartiallyRequiredTagWithCanceledProcedureStepState_DoesNotThrow()
        {
            var dataset = Samples.CreateCanceledWorkitemDataset(@"Unit Test Reason", ProcedureStepState.Canceled);
            dataset.Remove(DicomTag.UnifiedProcedureStepPerformedProcedureSequence);

            var target = new CancelWorkitemDatasetValidator();

            target.Validate(dataset);
        }

        [Fact]
        public void GivenMissingPartiallyRequiredTagWithCompletedProcedureStepState_Throws()
        {
            var dataset = Samples.CreateCanceledWorkitemDataset(@"Unit Test Reason", ProcedureStepState.Completed);
            dataset.Remove(DicomTag.UnifiedProcedureStepPerformedProcedureSequence);

            var target = new CancelWorkitemDatasetValidator();

            Assert.Throws<DatasetValidationException>(() => target.Validate(dataset));
        }

        [Fact]
        public void GivenEmptyValueForOptionalTag_DoesNotThrow()
        {
            var dataset = Samples.CreateCanceledWorkitemDataset(@"Unit Test Reason", ProcedureStepState.Canceled);
            dataset.AddOrUpdate(DicomTag.ProcedureStepLabel, string.Empty);

            var target = new CancelWorkitemDatasetValidator();

            target.Validate(dataset);
        }
    }
}
