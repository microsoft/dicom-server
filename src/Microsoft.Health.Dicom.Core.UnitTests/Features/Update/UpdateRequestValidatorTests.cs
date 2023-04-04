// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Update;
using Microsoft.Health.Dicom.Core.Messages.Update;
using Microsoft.Health.Dicom.Core.Models.Update;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Update;

public class UpdateRequestValidatorTests
{
    [Fact]
    public void GivenNullRequestBody_WhenValidated_ThenBadRequestExceptionShouldBeThrown()
    {
        UpdateInstanceRequest request = new UpdateInstanceRequest(null);
        Assert.Throws<BadRequestException>(() => UpdateRequestValidator.ValidateRequest(request));
    }

    [Theory]
    [MemberData(nameof(GetValidStudyInstanceUids))]
    public void GivenValidStudyInstanceIds_WhenValidated_ThenItShouldSucceed(IReadOnlyList<string> studyInstanceUids)
    {
        UpdateInstanceRequest request = new UpdateInstanceRequest(new UpdateSpecification(studyInstanceUids, null));
        UpdateRequestValidator.ValidateRequest(request);
    }

    [Theory]
    [MemberData(nameof(GetInvalidStudyInstanceUids))]
    public void GivenInvalidStudyInstanceIds_WhenValidated_ThenInvalidIdentifierExceptionShouldBeThrown(IReadOnlyList<string> studyInstanceUids)
    {
        UpdateInstanceRequest request = new UpdateInstanceRequest(new UpdateSpecification(studyInstanceUids, null));
        Assert.Throws<InvalidIdentifierException>(() => UpdateRequestValidator.ValidateRequest(request));
    }

    [Fact]
    public void GivenNullDataset_WhenValidated_ThenArgumentNullExceptionShouldBeThrown()
    {
        Assert.Throws<ArgumentNullException>(() => UpdateRequestValidator.ValidateDicomDataset(null));
    }

    [Theory]
    [MemberData(nameof(GetValidDicomDataset))]
    public void GivenAValidDataset_WhenValidated_ThenItShouldSucceed(DicomDataset dataset)
    {
        UpdateRequestValidator.ValidateDicomDataset(dataset);
    }

    public static IEnumerable<object[]> GetValidStudyInstanceUids()
    {
        yield return new object[] { new List<string>() };
        yield return new object[] { new List<string>() { "1.2.3.4" } };
        yield return new object[] { new List<string>() { "1.2.3.4", "1.2.3.5" } };
    }

    public static IEnumerable<object[]> GetInvalidStudyInstanceUids()
    {
        yield return new object[] { new List<string>() { "1.a1.2" } };
        yield return new object[] { new List<string>() { "invalid" } };
    }

    public static IEnumerable<object[]> GetValidDicomDataset()
    {
        yield return new object[] { new DicomDataset(new DicomPersonName(DicomTag.PatientBirthName, "foo")) };
        yield return new object[] { new DicomDataset()
            {
                { DicomTag.PatientID, "123" },
                { DicomTag.PatientName, "Anonymous" }
            } };
    }
}
