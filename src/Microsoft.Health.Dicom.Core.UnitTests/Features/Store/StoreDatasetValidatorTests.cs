// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store;

// Run these tests exclusively serial since they change the global autovalidation
[CollectionDefinition("Non-Parallel Collection", DisableParallelization = true)]
public class StoreDatasetValidatorTests
{
    private const ushort ValidationFailedFailureCode = 43264;
    private const ushort MismatchStudyInstanceUidFailureCode = 43265;

    private IStoreDatasetValidator _dicomDatasetValidator;

    private readonly DicomDataset _dicomDataset = Samples.CreateRandomInstanceDataset().NotValidated();
    private readonly IQueryTagService _queryTagService;
    private readonly List<QueryTag> _queryTags;
    private readonly IDicomTelemetryClient _telemetryClient = Substitute.For<IDicomTelemetryClient>();

    public StoreDatasetValidatorTests()
    {
        var featureConfiguration = Options.Create(new FeatureConfiguration() { EnableFullDicomItemValidation = false });
        var minValidator = new ElementMinimumValidator();
        _queryTagService = Substitute.For<IQueryTagService>();
        _queryTags = new List<QueryTag>(QueryTagService.CoreQueryTags);
        _queryTagService.GetQueryTagsAsync(Arg.Any<CancellationToken>()).Returns(_queryTags);
        _dicomDatasetValidator = new StoreDatasetValidator(featureConfiguration, minValidator, _queryTagService, _telemetryClient);
    }

    [Fact]
    public async Task GivenDicomTagWithDifferentVR_WhenValidated_ThenShouldThrowException()
    {
        var featureConfiguration = Options.Create(new FeatureConfiguration() { EnableFullDicomItemValidation = false });
        DicomTag tag = DicomTag.Date;
        DicomElement element = new DicomDateTime(tag, DateTime.Now);
        _dicomDataset.AddOrUpdate(element);

        _queryTags.Clear();
        _queryTags.Add(new QueryTag(tag.BuildExtendedQueryTagStoreEntry()));
        IElementMinimumValidator validator = Substitute.For<IElementMinimumValidator>();
        _dicomDatasetValidator = new StoreDatasetValidator(featureConfiguration, validator, _queryTagService, _telemetryClient);

        var result = await _dicomDatasetValidator.ValidateAsync(_dicomDataset, requiredStudyInstanceUid: null);

        Assert.IsType<ElementValidationException>(result.FirstException);

        validator.DidNotReceive().Validate(Arg.Any<DicomElement>());
    }

    [Fact]
    public async Task GivenAValidDicomDataset_WhenValidated_ThenItShouldSucceed()
    {
        var actual = await _dicomDatasetValidator.ValidateAsync(_dicomDataset, requiredStudyInstanceUid: null);

        Assert.Equal(ValidationWarnings.None, actual.WarningCodes);
    }

    [Fact]
    public async Task GivenDicomDatasetHavingDicomTagWithMultipleValues_WhenValidated_ThenItShouldReturnWarnings()
    {
        DicomElement element = new DicomLongString(DicomTag.StudyDescription, "Value1,", "Value2");
        var dicomDataset = Samples.CreateRandomInstanceDataset().NotValidated();
        dicomDataset.AddOrUpdate(element);

        var actual = await _dicomDatasetValidator.ValidateAsync(dicomDataset, requiredStudyInstanceUid: null);

        Assert.Equal(ValidationWarnings.IndexedDicomTagHasMultipleValues, actual.WarningCodes);
    }

    [Theory]
    [MemberData(nameof(GetNonExplicitVRTransferSyntax))]
    public async Task GivenAValidDicomDatasetWithImplicitVR_WhenValidated_ReturnsExpectedWarning(DicomTransferSyntax transferSyntax)
    {
        var dicomDataset = Samples
            .CreateRandomInstanceDataset(dicomTransferSyntax: transferSyntax)
            .NotValidated();

        var actual = await _dicomDatasetValidator.ValidateAsync(dicomDataset, requiredStudyInstanceUid: null);

        Assert.Equal(ValidationWarnings.DatasetDoesNotMatchSOPClass, actual.WarningCodes);
    }

    [Fact]
    public async Task GivenDicomDatasetWithImplicitVRAndHavingDicomTagWithMultipleValues_WhenValidated_ThenItShouldReturnWarnings()
    {
        DicomElement element = new DicomLongString(DicomTag.StudyDescription, "Value1,", "Value2");
        var dicomDataset = Samples.CreateRandomInstanceDataset(dicomTransferSyntax: DicomTransferSyntax.ImplicitVRBigEndian).NotValidated();
        dicomDataset.AddOrUpdate(element);

        var actual = await _dicomDatasetValidator.ValidateAsync(dicomDataset, requiredStudyInstanceUid: null);

        Assert.Equal(ValidationWarnings.IndexedDicomTagHasMultipleValues | ValidationWarnings.DatasetDoesNotMatchSOPClass,
            actual.WarningCodes);
    }

    [Fact]
    public async Task GivenAValidDicomDatasetThatMatchesTheRequiredStudyInstanceUid_WhenValidated_ThenItShouldSucceed()
    {
        string studyInstanceUid = TestUidGenerator.Generate();

        _dicomDataset.AddOrUpdate(DicomTag.StudyInstanceUID, studyInstanceUid);

        await _dicomDatasetValidator.ValidateAsync(
             _dicomDataset,
             studyInstanceUid);
    }

    // Sometimes users will pass a whitespace padded UID. This is likely a misinterpretation of documentation
    // specifying "If ending on an odd byte boundary, except when used for network negotiation (see PS3.8),
    // one trailing NULL (00H), as a padding character, shall follow the last component in order to align the UID on an
    // even byte boundary.":
    // https://dicom.nema.org/dicom/2013/output/chtml/part05/chapter_9.html
    [Theory]
    [InlineData(" ", "")]
    [InlineData("  ", "")]
    [InlineData(" ", " ")]
    [InlineData("", " ")]
    public async Task GivenAValidDicomDatasetThatMatchesTheRequiredStudyInstanceUidWithUidWhitespacePadding_WhenValidated_ThenItShouldSucceed(
        string queryStudyInstanceUidPadding,
        string saveStudyInstanceUidPadding)
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string queryStudyInstanceUid = studyInstanceUid + queryStudyInstanceUidPadding;
        string saveStudyInstanceUid = studyInstanceUid + saveStudyInstanceUidPadding;

        _dicomDataset.AddOrUpdate(DicomTag.StudyInstanceUID, saveStudyInstanceUid);


        await _dicomDatasetValidator.ValidateAsync(
            _dicomDataset,
            queryStudyInstanceUid);
    }

    public static IEnumerable<object[]> GetDicomTagsToRemove()
    {
        return new List<object[]>
        {
            new[] { DicomTag.PatientID.ToString() },
            new[] { DicomTag.StudyInstanceUID.ToString() },
            new[] { DicomTag.SeriesInstanceUID.ToString() },
            new[] { DicomTag.SOPInstanceUID.ToString() },
            new[] { DicomTag.SOPClassUID.ToString() },
        };
    }

    [Theory]
    [MemberData(nameof(GetDicomTagsToRemove))]
    public async Task GivenAMissingTag_WhenValidated_ThenDatasetValidationExceptionShouldBeThrown(string dicomTagInString)
    {
        DicomTag dicomTag = DicomTag.Parse(dicomTagInString);

        _dicomDataset.Remove(dicomTag);

        await ExecuteAndValidateException<DatasetValidationException>(ValidationFailedFailureCode);
    }

    public static IEnumerable<object[]> GetDuplicatedDicomIdentifierValues()
    {
        return new List<object[]>
        {
            new[] { DicomTag.StudyInstanceUID.ToString(), DicomTag.SeriesInstanceUID.ToString() },
            new[] { DicomTag.StudyInstanceUID.ToString(), DicomTag.SOPInstanceUID.ToString() },
            new[] { DicomTag.SeriesInstanceUID.ToString(), DicomTag.SOPInstanceUID.ToString() },
        };
    }

    [Theory]
    [MemberData(nameof(GetDuplicatedDicomIdentifierValues))]
    public async Task GivenDuplicatedIdentifiers_WhenValidated_ThenDatasetValidationExceptionShouldBeThrown(string firstDicomTagInString, string secondDicomTagInString)
    {
        DicomTag firstDicomTag = DicomTag.Parse(firstDicomTagInString);
        DicomTag secondDicomTag = DicomTag.Parse(secondDicomTagInString);

        string value = _dicomDataset.GetSingleValue<string>(firstDicomTag);
        _dicomDataset.AddOrUpdate(secondDicomTag, value);

        await ExecuteAndValidateException<DatasetValidationException>(ValidationFailedFailureCode);
    }

    [Fact]
    public async Task GivenStudyInstanceUidDoesNotMatchWithRequiredStudyInstanceUid_WhenValidated_ThenDatasetValidationExceptionShouldBeThrown()
    {
        string requiredStudyInstanceUid = null;
        string studyInstanceUid = _dicomDataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);

        do
        {
            requiredStudyInstanceUid = TestUidGenerator.Generate();
        }
        while (string.Equals(requiredStudyInstanceUid, studyInstanceUid, System.StringComparison.InvariantCultureIgnoreCase));

        await ExecuteAndValidateException<DatasetValidationException>(MismatchStudyInstanceUidFailureCode, requiredStudyInstanceUid);
    }

    [Fact]
    public async Task GivenDatasetWithInvalidVrValue_WhenValidatingWithFullValidation_ThenDatasetValidationExceptionShouldBeThrown()
    {
        var featureConfiguration = Substitute.For<IOptions<FeatureConfiguration>>();
        featureConfiguration.Value.Returns(new FeatureConfiguration
        {
            EnableFullDicomItemValidation = true,
        });
        var minValidator = new ElementMinimumValidator();

        _dicomDatasetValidator = new StoreDatasetValidator(featureConfiguration, minValidator, _queryTagService, _telemetryClient);

        // LO VR, invalid characters
        _dicomDataset.Add(DicomTag.SeriesDescription, "CT1 abdomen\u0000");

        await ExecuteAndValidateException<DatasetValidationException>(ValidationFailedFailureCode);
    }

    [Fact]
    public async Task GivenDatasetWithInvalidIndexedTagValue_WhenValidating_ThenValidationExceptionShouldBeThrown()
    {
        // CS VR, > 16 characters is not allowed
        _dicomDataset.Add(DicomTag.Modality, "01234567890123456789");

        await ExecuteAndValidateTagEntriesException<ElementValidationException>(ValidationFailedFailureCode);
    }


    [Fact]
    public async Task GivenDatasetWithInvalidIndexedTagValue_WhenValidating_ThenValidationExceptionMetricsShouldBeTracked()
    {
        // CS VR, > 16 characters is not allowed
        _dicomDataset.Add(DicomTag.Modality, "01234567890123456789");

        await ExecuteAndValidateTagEntriesException<ElementValidationException>(ValidationFailedFailureCode);
        _telemetryClient.Received(1).TrackMetric(
            "IndexingTagValidationErrorByVrCode",
            "ExceedMaxLength",
            "Modality",
            "CS");
    }

    [Fact]
    public async Task GivenDatasetWithEmptyIndexedTagValue_WhenValidating_ThenValidationPasses()
    {
        _dicomDataset.AddOrUpdate(DicomTag.ReferringPhysicianName, string.Empty);
        await _dicomDatasetValidator.ValidateAsync(_dicomDataset, null);
    }

    [Fact]
    public async Task GivenExtendedQueryTags_WhenValidating_ThenExtendedQueryTagsShouldBeValidated()
    {
        DicomTag standardTag = DicomTag.DestinationAE;

        // AE > 16 characters is not allowed
        _dicomDataset.Add(standardTag, "01234567890123456");

        QueryTag indextag = new QueryTag(standardTag.BuildExtendedQueryTagStoreEntry());
        _queryTags.Add(indextag);
        await ExecuteAndValidateTagEntriesException<ElementValidationException>(ValidationFailedFailureCode);
    }

    [Fact]
    public async Task GivenPrivateExtendedQueryTags_WhenValidating_ThenExtendedQueryTagsShouldBeValidated()
    {
        DicomTag tag = DicomTag.Parse("04050001");

        DicomIntegerString element = new DicomIntegerString(tag, "0123456789123"); // exceed max length 12

        // AE > 16 characters is not allowed
        _dicomDataset.Add(element);

        QueryTag indextag = new QueryTag(tag.BuildExtendedQueryTagStoreEntry(vr: element.ValueRepresentation.Code));
        _queryTags.Clear();
        _queryTags.Add(indextag);

        await ExecuteAndValidateTagEntriesException<ElementValidationException>(ValidationFailedFailureCode);
    }

    private async Task ExecuteAndValidateException<T>(ushort failureCode, string requiredStudyInstanceUid = null)
        where T : Exception
    {
        var exception = await Assert.ThrowsAsync<T>(() => _dicomDatasetValidator.ValidateAsync(_dicomDataset, requiredStudyInstanceUid));

        if (exception is DatasetValidationException)
        {
            var datasetValidationException = exception as DatasetValidationException;
            Assert.Equal(failureCode, datasetValidationException.FailureCode);
        }
    }

    private async Task ExecuteAndValidateTagEntriesException<T>(ushort failureCode, string requiredStudyInstanceUid = null)
        where T : Exception
    {
        var result = await _dicomDatasetValidator.ValidateAsync(_dicomDataset, requiredStudyInstanceUid);

        if (result.FirstException is DatasetValidationException)
        {
            var datasetValidationException = result.FirstException as DatasetValidationException;
            Assert.Equal(failureCode, datasetValidationException.FailureCode);
        }
    }

    public static IEnumerable<object[]> GetNonExplicitVRTransferSyntax()
    {
        foreach (var ts in Samples.GetAllDicomTransferSyntax())
        {
            if (ts.IsExplicitVR)
                continue;

            yield return new object[] { ts };
        }
    }
}
