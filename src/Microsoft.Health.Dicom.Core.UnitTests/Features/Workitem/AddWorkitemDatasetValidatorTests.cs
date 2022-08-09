// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem;

public class AddWorkitemDatasetValidatorTests
{
    [Fact]
    public void GivenMissingRequiredTag_Throws()
    {
        var dataset = Samples.CreateRandomWorkitemInstanceDataset();
        var validator = new AddWorkitemDatasetValidator();

        dataset = dataset.Remove(DicomTag.TransactionUID);

        Assert.Throws<DatasetValidationException>(() => validator.Validate(dataset));
    }

    [Fact]
    public void GivenNotAllowedTag_WhenPresent_Throws()
    {
        var dataset = Samples.CreateRandomWorkitemInstanceDataset();

        dataset = dataset.AddOrUpdate(
            new DicomSequence(
                DicomTag.ProcedureStepProgressInformationSequence,
                new DicomDataset
                {
                    { DicomTag.ProcedureStepProgress, "1.0" },
                }));

        var validator = new AddWorkitemDatasetValidator();

        Assert.Throws<DatasetValidationException>(() => validator.Validate(dataset));
    }

    [Fact]
    public void GivenTagShouldBeEmpty_WhenHasValue_Throws()
    {
        var dataset = Samples.CreateRandomWorkitemInstanceDataset();

        dataset = dataset.AddOrUpdate(new DicomUniqueIdentifier(DicomTag.TransactionUID, "123"));

        var validator = new AddWorkitemDatasetValidator();

        Assert.Throws<DatasetValidationException>(() => validator.Validate(dataset));
    }
}
