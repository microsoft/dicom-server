// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Context;
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
[Collection("Auto-Validation Collection")]
public class StoreDatasetValidatorTestsV1
{
    private const ushort ValidationFailedFailureCode = 43264;
    private const ushort MismatchStudyInstanceUidFailureCode = 43265;
    private IStoreDatasetValidator _dicomDatasetValidator;
    private readonly DicomDataset _dicomDataset = Samples.CreateRandomInstanceDataset().NotValidated();
    private readonly IQueryTagService _queryTagService;
    private readonly List<QueryTag> _queryTags;
    private readonly StoreMeter _storeMeter;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();

    public StoreDatasetValidatorTestsV1()
    {
        var featureConfiguration = Options.Create(new FeatureConfiguration() { EnableFullDicomItemValidation = false });
        var minValidator = new ElementMinimumValidator();
        _queryTagService = Substitute.For<IQueryTagService>();
        _queryTags = new List<QueryTag>(QueryTagService.CoreQueryTags);
        _queryTagService.GetQueryTagsAsync(Arg.Any<CancellationToken>()).Returns(_queryTags);
        _storeMeter = new StoreMeter();
        _dicomDatasetValidator = new StoreDatasetValidator(featureConfiguration, minValidator, _queryTagService, _storeMeter, _dicomRequestContextAccessor, NullLogger<StoreDatasetValidator>.Instance);
    }

    [Fact]
    public async Task GivenFullValidation_WhenPatientIDInvalid_ExpectErrorProduced()
    {
        var featureConfigurationEnableFullValidation = Substitute.For<IOptions<FeatureConfiguration>>();
        featureConfigurationEnableFullValidation.Value.Returns(new FeatureConfiguration
        {
            EnableFullDicomItemValidation = true,
        });

        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(
            validateItems: false,
            patientId: "Before Null Character, \0");

        IElementMinimumValidator minimumValidator = Substitute.For<IElementMinimumValidator>();

        var dicomDatasetValidator = new StoreDatasetValidator(
            featureConfigurationEnableFullValidation,
            minimumValidator,
            _queryTagService,
            _storeMeter,
            _dicomRequestContextAccessor,
            NullLogger<StoreDatasetValidator>.Instance);

        var result = await dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.Contains(
            """does not validate VR LO: value contains invalid character""",
            result.InvalidTagErrors[DicomTag.PatientID].Error);
        minimumValidator.DidNotReceive().Validate(Arg.Any<DicomElement>());
    }

    [Fact]
    public async Task GivenPartialValidation_WhenPatientIDInvalid_ExpectTagValidatedAndErrorProduced()
    {

        var featureConfigurationEnableFullValidation = Substitute.For<IOptions<FeatureConfiguration>>();
        featureConfigurationEnableFullValidation.Value.Returns(new FeatureConfiguration
        {
            EnableFullDicomItemValidation = false,
        });

        IElementMinimumValidator minimumValidator = new ElementMinimumValidator();

        var dicomDatasetValidator = new StoreDatasetValidator(
            featureConfigurationEnableFullValidation,
            minimumValidator,
            _queryTagService,
            _storeMeter,
            _dicomRequestContextAccessor,
            NullLogger<StoreDatasetValidator>.Instance);

        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(
            validateItems: false,
            patientId: "Before Null Character, \0");

        var result = await dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.True(result.InvalidTagErrors.Any());
        Assert.Single(result.InvalidTagErrors);
        Assert.Equal(
            """DICOM100: (0010,0020) - Dicom element 'PatientID' failed validation for VR 'LO': Value contains invalid character.""",
            result.InvalidTagErrors[DicomTag.PatientID].Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GivenPatientIdEmpty_WhenValidated_ExpectErrorProduced(string value)
    {
        var featureConfigurationEnableFullValidation = Substitute.For<IOptions<FeatureConfiguration>>();
        featureConfigurationEnableFullValidation.Value.Returns(new FeatureConfiguration { });

        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(
            validateItems: false,
            patientId: value);

        if (value == null)
            dicomDataset.AddOrUpdate(DicomTag.PatientID, new string[] { null });

        IElementMinimumValidator minimumValidator = Substitute.For<IElementMinimumValidator>();

        var dicomDatasetValidator = new StoreDatasetValidator(
            featureConfigurationEnableFullValidation,
            minimumValidator,
            _queryTagService,
            _storeMeter,
            _dicomRequestContextAccessor,
            NullLogger<StoreDatasetValidator>.Instance);

        var result = await dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.Contains(
            "DICOM100: (0010,0020) - The required tag '(0010,0020)' is missing.",
            result.InvalidTagErrors[DicomTag.PatientID].Error);
    }

    [Fact]
    public async Task GivenPatientIdTagNotPresent_WhenValidated_ExpectErrorProduced()
    {
        var featureConfigurationEnableFullValidation = Substitute.For<IOptions<FeatureConfiguration>>();
        featureConfigurationEnableFullValidation.Value.Returns(new FeatureConfiguration { });

        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(
            validateItems: false);
        dicomDataset.Remove(DicomTag.PatientID);

        IElementMinimumValidator minimumValidator = Substitute.For<IElementMinimumValidator>();

        var dicomDatasetValidator = new StoreDatasetValidator(
            featureConfigurationEnableFullValidation,
            minimumValidator,
            _queryTagService,
            _storeMeter,
            _dicomRequestContextAccessor,
            NullLogger<StoreDatasetValidator>.Instance);

        var result = await dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.Contains(
            "DICOM100: (0010,0020) - The required tag '(0010,0020)' is missing.",
            result.InvalidTagErrors[DicomTag.PatientID].Error);
    }

    [Fact]
    public async Task GivenDicomTagWithDifferentVR_WhenValidated_ThenShouldReturnInvalidEntries()
    {
        var featureConfiguration = Options.Create(new FeatureConfiguration() { EnableFullDicomItemValidation = false });
        DicomTag tag = DicomTag.Date;
        DicomElement element = new DicomDateTime(tag, DateTime.Now);
        _dicomDataset.AddOrUpdate(element);

        _queryTags.Clear();
        _queryTags.Add(new QueryTag(tag.BuildExtendedQueryTagStoreEntry()));
        IElementMinimumValidator validator = Substitute.For<IElementMinimumValidator>();
        _dicomDatasetValidator = new StoreDatasetValidator(featureConfiguration, validator, _queryTagService, _storeMeter, _dicomRequestContextAccessor, NullLogger<StoreDatasetValidator>.Instance);

        var result = await _dicomDatasetValidator.ValidateAsync(_dicomDataset, requiredStudyInstanceUid: null);

        Assert.True(result.InvalidTagErrors.Any());

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

        await ExecuteAndValidateInvalidTagEntries(dicomTag);
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

    [Fact]
    public async Task GivenNonRequiredTagNull_ExpectTagValidatedAndNoErrorProduced()
    {
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);
        dicomDataset.Add(DicomTag.ContentDate, new string[] { null });
        dicomDataset.AddOrUpdate(DicomTag.PatientName, new string[] { null });
        dicomDataset.Add(DicomTag.WindowCenterWidthExplanation, new string[] { null });

        var result = await _dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.Empty(result.InvalidTagErrors);
    }

    [Theory]
    [MemberData(nameof(GetDuplicatedDicomIdentifierValues))]
    public async Task GivenDuplicatedIdentifiers_WhenValidated_ThenValidationPasses(string firstDicomTagInString, string secondDicomTagInString)
    {
        DicomTag firstDicomTag = DicomTag.Parse(firstDicomTagInString);
        DicomTag secondDicomTag = DicomTag.Parse(secondDicomTagInString);

        string value = _dicomDataset.GetSingleValue<string>(firstDicomTag);
        _dicomDataset.AddOrUpdate(secondDicomTag, value);

        var result = await _dicomDatasetValidator.ValidateAsync(
            _dicomDataset,
            null,
            new CancellationToken());

        Assert.Empty(result.InvalidTagErrors);
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
        while (string.Equals(requiredStudyInstanceUid, studyInstanceUid, StringComparison.InvariantCultureIgnoreCase));

        await ExecuteAndValidateInvalidTagEntries(DicomTag.StudyInstanceUID, requiredStudyInstanceUid);
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

        _dicomDatasetValidator = new StoreDatasetValidator(featureConfiguration, minValidator, _queryTagService, _storeMeter, _dicomRequestContextAccessor, NullLogger<StoreDatasetValidator>.Instance);

        // LO VR, invalid characters
        _dicomDataset.Add(DicomTag.SeriesDescription, "CT1 abdomen\u0000");

        await ExecuteAndValidateInvalidTagEntries(DicomTag.SeriesDescription);
    }

    [Fact]
    public async Task GivenDatasetWithInvalidIndexedTagValue_WhenValidating_ThenValidationExceptionShouldBeThrown()
    {
        // CS VR, > 16 characters is not allowed
        _dicomDataset.Add(DicomTag.Modality, "01234567890123456789");

        await ExecuteAndValidateInvalidTagEntries(DicomTag.Modality);
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
        await ExecuteAndValidateInvalidTagEntries(standardTag);
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

        await ExecuteAndValidateInvalidTagEntries(tag);
    }

    private async Task ExecuteAndValidateInvalidTagEntries(DicomTag dicomTag, string requiredStudyInstanceUid = null)
    {
        var result = await _dicomDatasetValidator.ValidateAsync(_dicomDataset, requiredStudyInstanceUid);

        Assert.True(result.InvalidTagErrors.ContainsKey(dicomTag));
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
