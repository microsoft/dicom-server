// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Update;
using Microsoft.Health.Dicom.Core.Models.Update;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Update;

public class UpdateRequestValidatorTests
{
    [Theory]
    [MemberData(nameof(GetInvalidStudyInstanceUidsCount))]
    public void GivenInvalidStudyInstanceUids_WhenValidated_ThenBadRequestExceptionShouldBeThrown(IReadOnlyList<string> studyInstanceUids)
    {
        UpdateSpecification updateSpecification = new UpdateSpecification(studyInstanceUids, null);
        Assert.Throws<BadRequestException>(() => UpdateRequestValidator.ValidateRequest(updateSpecification));
    }

    [Theory]
    [MemberData(nameof(GetInvalidStudyInstanceUids))]
    public void GivenInvalidStudyInstanceIds_WhenValidated_ThenInvalidIdentifierExceptionShouldBeThrown(IReadOnlyList<string> studyInstanceUids)
    {
        UpdateSpecification updateSpecification = new UpdateSpecification(studyInstanceUids, null);
        Assert.Throws<InvalidIdentifierException>(() => UpdateRequestValidator.ValidateRequest(updateSpecification));
    }

    [Theory]
    [MemberData(nameof(GetValidStudyInstanceUids))]
    public void GivenValidStudyInstanceIds_WhenValidated_ThenItShouldSucceed(IReadOnlyList<string> studyInstanceUids)
    {
        UpdateSpecification updateSpecification = new UpdateSpecification(studyInstanceUids, null);
        UpdateRequestValidator.ValidateRequest(updateSpecification);
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

    [Theory]
    [MemberData(nameof(GetInvalidDicomDataset))]
    public void GivenAnInvalidDataset_WhenValidated_ThenSucceedsWithErrorsInFailedAttributesSequence(DicomDataset dataset)
    {
        string errorComment = "DICOM100: (0010,0021) - Updating the tag is not supported";
        DicomDataset failedSop = UpdateRequestValidator.ValidateDicomDataset(dataset);
        DicomSequence failedAttributeSequence = failedSop.GetSequence(DicomTag.FailedAttributesSequence);
        Assert.Single(failedAttributeSequence);
        Assert.Equal(errorComment, failedAttributeSequence.Items[0].GetString(DicomTag.ErrorComment));
    }

    public static IEnumerable<object[]> GetValidStudyInstanceUids()
    {
        yield return new object[] { new List<string>() { "1.2.3.4" } };
        yield return new object[] { new List<string>() { "1.2.3.4", "1.2.3.5" } };
    }

    public static IEnumerable<object[]> GetInvalidStudyInstanceUids()
    {
        yield return new object[] { new List<string>() { "1.a1.2" } };
        yield return new object[] { new List<string>() { "invalid" } };
    }

    public static IEnumerable<object[]> GetInvalidStudyInstanceUidsCount()
    {
        yield return new object[] { null };
        yield return new object[] { new List<string>() };
        yield return new object[] { new List<string>() {
            "1.1.1.1", "1.1.1.2", "1.1.1.3", "1.1.1.4", "1.1.1.5", "1.1.1.6", "1.1.1.7", "1.1.1.8", "1.1.1.9", "1.1.1.10",
            "1.1.2.1", "1.1.2.2", "1.1.2.3", "1.1.2.4", "1.1.2.5", "1.1.2.6", "1.1.2.7", "1.1.2.8", "1.1.2.9", "1.1.2.10",
            "1.1.3.1", "1.1.3.2", "1.1.3.3", "1.1.3.4", "1.1.3.5", "1.1.3.6", "1.1.3.7", "1.1.3.8", "1.1.3.9", "1.1.3.10",
            "1.1.4.1", "1.1.4.2", "1.1.4.3", "1.1.4.4", "1.1.4.5", "1.1.4.6", "1.1.4.7", "1.1.4.8", "1.1.4.9", "1.1.4.10",
            "1.1.5.1", "1.1.5.2", "1.1.5.3", "1.1.5.4", "1.1.5.5", "1.1.5.6", "1.1.5.7", "1.1.5.8", "1.1.5.9", "1.1.5.10",
            "1.1.6.1", "1.1.6.2", "1.1.6.3", "1.1.6.4", "1.1.6.5", "1.1.6.6", "1.1.6.7", "1.1.6.8", "1.1.6.9", "1.1.6.10" } };
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

    public static IEnumerable<object[]> GetInvalidDicomDataset()
    {
        yield return new object[] { new DicomDataset(new DicomPersonName(DicomTag.IssuerOfPatientID, "Issuer")) };
    }
}
