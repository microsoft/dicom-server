// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem
{
    public class AddWorkitemDatasetValidatorTests
    {
        [Fact]
        public void GivenValidateForDuplicateTagValuesInSequence_WhenSequenceHasNoDuplicateTag_NoExceptionsThrown()
        {
            var sqDataset = new DicomDataset();
            sqDataset.Add(DicomTag.ExpectedCompletionDateTime, DateTime.Now);

            var dataset = Samples.CreateRandomWorkitemInstanceDataset();
            dataset.AddOrUpdate(DicomTag.ScheduledWorkitemCodeSequence, sqDataset);

            AddWorkitemDatasetValidator.ValidateForDuplicateTagValuesInSequence(dataset);
        }
    }
}
