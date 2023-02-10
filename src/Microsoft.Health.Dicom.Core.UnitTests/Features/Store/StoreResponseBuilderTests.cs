// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Messages.Store;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store;

public class StoreResponseBuilderTests
{
    private readonly IUrlResolver _urlResolver = new MockUrlResolver();
    private readonly StoreResponseBuilder _storeResponseBuilder;
    private static readonly StoreValidationResult DefaultStoreValidationResult = new StoreValidationResultBuilder().Build();

    private readonly DicomDataset _dicomDataset1 = Samples.CreateRandomInstanceDataset(
        studyInstanceUid: "1",
        seriesInstanceUid: "2",
        sopInstanceUid: "3",
        sopClassUid: "4");

    private readonly DicomDataset _dicomDataset2 = Samples.CreateRandomInstanceDataset(
        studyInstanceUid: "10",
        seriesInstanceUid: "11",
        sopInstanceUid: "12",
        sopClassUid: "13");


    public StoreResponseBuilderTests()
    {
        _storeResponseBuilder = new StoreResponseBuilder(
            _urlResolver);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("1.2.3")]
    public void GivenNoEntries_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned(string studyInstanceUid)
    {
        StoreResponse response = _storeResponseBuilder.BuildResponse(studyInstanceUid);

        Assert.NotNull(response);
        Assert.Equal(StoreResponseStatus.None, response.Status);
        Assert.Null(response.Dataset);
    }

    [Fact]
    public void GivenOnlySuccessEntry_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
    {
        _storeResponseBuilder.AddSuccess(_dicomDataset1, DefaultStoreValidationResult);

        StoreResponse response = _storeResponseBuilder.BuildResponse(null);

        Assert.NotNull(response);
        Assert.Equal(StoreResponseStatus.Success, response.Status);
        Assert.NotNull(response.Dataset);
        Assert.Single(response.Dataset);

        ValidationHelpers.ValidateReferencedSopSequence(
            response.Dataset,
            ("3", "/1/2/3", "4"));
    }

    [Fact]
    public void GivenBuilderHadNoErrors_WhenDropMetadataEnabled_ThenResponseHasEmptyFailedSequence()
    {
        _storeResponseBuilder.AddSuccess(_dicomDataset1, DefaultStoreValidationResult, enableDropInvalidDicomJsonMetadata: true);

        StoreResponse response = _storeResponseBuilder.BuildResponse(null);

        Assert.Equal(StoreResponseStatus.Success, response.Status);
        Assert.Single(response.Dataset);

        DicomSequence refSopSequence = response.Dataset.GetSequence(DicomTag.ReferencedSOPSequence);
        Assert.Single(refSopSequence);
        DicomDataset ds = refSopSequence.Items[0];

        DicomSequence failedSequence = ds.GetSequence(DicomTag.FailedAttributesSequence);
        Assert.Empty(failedSequence);
    }

    [Fact]
    public void GivenOnlyFailedEntry_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
    {
        const ushort failureReasonCode = 100;

        _storeResponseBuilder.AddFailure(_dicomDataset2, failureReasonCode);

        StoreResponse response = _storeResponseBuilder.BuildResponse(null);

        Assert.NotNull(response);
        Assert.Equal(StoreResponseStatus.Failure, response.Status);
        Assert.NotNull(response.Dataset);
        Assert.Single(response.Dataset);

        ValidationHelpers.ValidateFailedSopSequence(
            response.Dataset,
            ("12", "13", failureReasonCode));
    }

    [Fact]
    public void GivenBuilderHasErrors_WhenDropMetadataEnabled_ThenResponseHasNonEmptyFailedSequence()
    {
        StoreValidationResultBuilder builder = new StoreValidationResultBuilder();
        builder.Add(new Exception("There was an issue with an attribute"), DicomTag.PatientAge);
        StoreValidationResult storeValidationResult = builder.Build();
        _storeResponseBuilder.AddSuccess(_dicomDataset1, storeValidationResult, enableDropInvalidDicomJsonMetadata: true);

        StoreResponse response = _storeResponseBuilder.BuildResponse(null);

        Assert.Equal(StoreResponseStatus.Success, response.Status);
        Assert.Single(response.Dataset);

        DicomSequence refSopSequence = response.Dataset.GetSequence(DicomTag.ReferencedSOPSequence);
        Assert.Single(refSopSequence);
        DicomDataset ds = refSopSequence.Items[0];

        DicomSequence failedSequence = ds.GetSequence(DicomTag.FailedAttributesSequence);
        Assert.Single(failedSequence);
        // expect comment sequence has single warning about single invalid attribute
        Assert.Equal(
            storeValidationResult.InvalidTagErrors.ToArray()[0].Value.Error,
            failedSequence.Items[0].GetString(DicomTag.ErrorComment)
        );
    }

    [Fact]
    public void GivenBuildWithAndWithoutErrors_WhenDropMetadataEnabled_ThenResponseHasNonEmptyFailedSequenceAndEmptyFailedSequence()
    {
        // This represents multiple instance being processed where one had a validation failure and the other did not

        // simulate validation failure
        StoreValidationResultBuilder builder = new StoreValidationResultBuilder();
        builder.Add(new Exception("There was an issue with an attribute"), DicomTag.PatientAge);
        StoreValidationResult storeValidationResult = builder.Build();
        _storeResponseBuilder.AddSuccess(_dicomDataset1, storeValidationResult, enableDropInvalidDicomJsonMetadata: true);

        //simulate validation pass
        _storeResponseBuilder.AddSuccess(_dicomDataset1, DefaultStoreValidationResult, enableDropInvalidDicomJsonMetadata: true);

        StoreResponse response = _storeResponseBuilder.BuildResponse(null);

        Assert.Equal(StoreResponseStatus.Success, response.Status);
        Assert.NotNull(response.Dataset);

        DicomSequence refSopSequence = response.Dataset.GetSequence(DicomTag.ReferencedSOPSequence);
        Assert.Equal(2, refSopSequence.Items.Count);

        // invalid instance section has error in FailedSOPSequence
        DicomDataset invalidInstanceResponse = refSopSequence.Items[0];
        DicomSequence failedSequence = invalidInstanceResponse.GetSequence(DicomTag.FailedAttributesSequence);
        Assert.Single(failedSequence);
        // expect comment sequence has single warning about single invalid attribute
        Assert.Equal(
            storeValidationResult.InvalidTagErrors.ToArray()[0].Value.Error,
            failedSequence.Items[0].GetString(DicomTag.ErrorComment)
        );

        // valid instance section has an empty FailedSOPSequence as there were no errors
        DicomDataset validInstanceResponse = refSopSequence.Items[1];
        Assert.Empty(validInstanceResponse.GetSequence(DicomTag.FailedAttributesSequence));
    }

    [Fact]
    public void GivenBothSuccessAndFailedEntires_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
    {
        _storeResponseBuilder.AddFailure(_dicomDataset1, TestConstants.ProcessingFailureReasonCode);
        _storeResponseBuilder.AddSuccess(_dicomDataset2, DefaultStoreValidationResult);

        StoreResponse response = _storeResponseBuilder.BuildResponse(null);

        Assert.NotNull(response);
        Assert.Equal(StoreResponseStatus.PartialSuccess, response.Status);
        Assert.NotNull(response.Dataset);
        Assert.Equal(2, response.Dataset.Count());

        ValidationHelpers.ValidateFailedSopSequence(
            response.Dataset,
            ("3", "4", TestConstants.ProcessingFailureReasonCode));

        ValidationHelpers.ValidateReferencedSopSequence(
            response.Dataset,
            ("12", "/10/11/12", "13"));
    }

    [Fact]
    public void GivenMultipleSuccessAndFailedEntries_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
    {
        ushort failureReasonCode1 = TestConstants.ProcessingFailureReasonCode;
        ushort failureReasonCode2 = 100;

        _storeResponseBuilder.AddFailure(_dicomDataset1, failureReasonCode1);
        _storeResponseBuilder.AddFailure(_dicomDataset2, failureReasonCode2);

        _storeResponseBuilder.AddSuccess(_dicomDataset2, DefaultStoreValidationResult);
        _storeResponseBuilder.AddSuccess(_dicomDataset1, DefaultStoreValidationResult);

        StoreResponse response = _storeResponseBuilder.BuildResponse(null);

        Assert.NotNull(response);
        Assert.Equal(StoreResponseStatus.PartialSuccess, response.Status);
        Assert.NotNull(response.Dataset);
        Assert.Equal(2, response.Dataset.Count());

        ValidationHelpers.ValidateFailedSopSequence(
            response.Dataset,
            ("3", "4", failureReasonCode1),
            ("12", "13", failureReasonCode2));

        ValidationHelpers.ValidateReferencedSopSequence(
            response.Dataset,
            ("12", "/10/11/12", "13"),
            ("3", "/1/2/3", "4"));
    }

    [Fact]
    public void GivenNullDicomDatasetWhenAddingFailure_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
    {
        const ushort failureReasonCode = 300;

        _storeResponseBuilder.AddFailure(dicomDataset: null, failureReasonCode: failureReasonCode);

        StoreResponse response = _storeResponseBuilder.BuildResponse(null);

        Assert.NotNull(response);
        Assert.Equal(StoreResponseStatus.Failure, response.Status);
        Assert.NotNull(response.Dataset);
        Assert.Single(response.Dataset);

        ValidationHelpers.ValidateFailedSopSequence(
            response.Dataset,
            (null, null, failureReasonCode));
    }

    [Fact]
    public void GivenStudyInstanceUidAndThereIsOnlySuccessEntries_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
    {
        _storeResponseBuilder.AddSuccess(_dicomDataset1, DefaultStoreValidationResult);

        StoreResponse response = _storeResponseBuilder.BuildResponse("1");

        Assert.NotNull(response);
        Assert.NotNull(response.Dataset);

        // We have 2 items: RetrieveURL and ReferencedSOPSequence.
        Assert.Equal(2, response.Dataset.Count());
        Assert.Equal("1", response.Dataset.GetFirstValueOrDefault<string>(DicomTag.RetrieveURL));
    }

    [Fact]
    public void GivenStudyInstanceUidAndThereIsOnlyFailedEntries_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
    {
        _storeResponseBuilder.AddFailure(dicomDataset: null, failureReasonCode: 500);

        StoreResponse response = _storeResponseBuilder.BuildResponse("1");

        Assert.NotNull(response);
        Assert.NotNull(response.Dataset);

        // We have 1 item: FailedSOPSequence.
        Assert.Single(response.Dataset);
    }

    [Fact]
    public void GivenStudyInstanceUidAndThereAreSuccessAndFailureEntries_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
    {
        _storeResponseBuilder.AddSuccess(_dicomDataset1, DefaultStoreValidationResult);
        _storeResponseBuilder.AddFailure(_dicomDataset2, failureReasonCode: 200);

        StoreResponse response = _storeResponseBuilder.BuildResponse("1");

        Assert.NotNull(response);
        Assert.NotNull(response.Dataset);

        // We have 3 items: RetrieveURL, FailedSOPSequence, and ReferencedSOPSequence.
        Assert.Equal(3, response.Dataset.Count());
        Assert.Equal("1", response.Dataset.GetFirstValueOrDefault<string>(DicomTag.RetrieveURL));
    }

    [Fact]
    public void GivenInvalidUidValue_WhenResponseIsBuilt_ThenItShouldNotThrowException()
    {
        // Create a DICOM dataset with invalid UID value.
        var dicomDataset = new DicomDataset().NotValidated();

        dicomDataset.Add(DicomTag.SOPClassUID, "invalid");

        _storeResponseBuilder.AddFailure(dicomDataset, failureReasonCode: 500);

        StoreResponse response = _storeResponseBuilder.BuildResponse(studyInstanceUid: null);

        Assert.NotNull(response);
        Assert.NotNull(response.Dataset);

        ValidationHelpers.ValidateFailedSopSequence(
            response.Dataset,
            (null, "invalid", 500));
    }

    [Fact]
    public void GivenWarning_WhenResponseIsBuilt_ThenItShouldHaveExpectedWarning()
    {
        string warning = "WarningMessage";
        _storeResponseBuilder.SetWarningMessage(warning);
        var response = _storeResponseBuilder.BuildResponse(studyInstanceUid: null);
        Assert.Equal(warning, response.Warning);
    }
}
