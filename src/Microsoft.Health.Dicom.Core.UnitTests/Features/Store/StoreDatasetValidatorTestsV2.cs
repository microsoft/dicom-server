// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store;

// Run these tests exclusively serial since they change the global autovalidation
[Collection("Auto-Validation Collection")]
public class StoreDatasetValidatorTestsV2
{
    private readonly IStoreDatasetValidator _dicomDatasetValidator;
    private readonly DicomDataset _dicomDataset = Samples.CreateRandomInstanceDataset().NotValidated();
    private readonly IQueryTagService _queryTagService;
    private readonly List<QueryTag> _queryTags;
    private readonly StoreMeter _storeMeter;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessorV2 = Substitute.For<IDicomRequestContextAccessor>();
    private readonly IDicomRequestContext _dicomRequestContextV2 = Substitute.For<IDicomRequestContext>();
    private readonly IElementMinimumValidator _minimumValidator = new ElementMinimumValidator();

    public StoreDatasetValidatorTestsV2()
    {
        var featureConfiguration = Options.Create(new FeatureConfiguration() { EnableFullDicomItemValidation = false });
        _queryTagService = Substitute.For<IQueryTagService>();
        _queryTags = new List<QueryTag>(QueryTagService.CoreQueryTags);
        _queryTagService.GetQueryTagsAsync(Arg.Any<CancellationToken>()).Returns(_queryTags);
        _storeMeter = new StoreMeter();
        _dicomRequestContextV2.Version.Returns(2);
        _dicomRequestContextAccessorV2.RequestContext.Returns(_dicomRequestContextV2);
        _dicomDatasetValidator = new StoreDatasetValidator(featureConfiguration, _minimumValidator, _queryTagService, _storeMeter, _dicomRequestContextAccessorV2, NullLogger<StoreDatasetValidator>.Instance);
    }

    [Fact(Skip = "Issue with minimum validation implementation, Fixing as part of https://github.com/microsoft/dicom-server/pull/3283")]
    public async Task GivenFullValidation_WhenPatientIDInvalid_ExpectErrorProduced()
    {
        // Even when V2 api is requested, if full validation is enabled, we will validate and generate warnings on invalid tags
        var featureConfigurationEnableFullValidation = Substitute.For<IOptions<FeatureConfiguration>>();
        featureConfigurationEnableFullValidation.Value.Returns(new FeatureConfiguration { EnableFullDicomItemValidation = true, });

        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(
            validateItems: false,
            patientId: "Before Null Character, \0");

        var validator = new StoreDatasetValidator(
            featureConfigurationEnableFullValidation,
            _minimumValidator,
            _queryTagService,
            _storeMeter,
            _dicomRequestContextAccessorV2,
            NullLogger<StoreDatasetValidator>.Instance);

        var result = await validator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.Contains(
            """does not validate VR LO: value contains invalid character""",
            result.InvalidTagErrors[DicomTag.PatientID].Error);

        _minimumValidator.DidNotReceive().Validate(Arg.Any<DicomElement>(), ValidationLevel.Default);
    }

    [Fact]
    public async Task GivenV2Enabled_WhenNonCoreTagInvalid_ExpectTagValidatedAndErrorProduced()
    {
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);
        dicomDataset.Add(DicomTag.ReviewDate, "NotAValidReviewDate");

        var result = await _dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.True(result.InvalidTagErrors.Any());
        Assert.Single(result.InvalidTagErrors);
        Assert.Equal("""DICOM100: (300e,0004) - Content "NotAValidReviewDate" does not validate VR DA: one of the date values does not match the pattern YYYYMMDD""", result.InvalidTagErrors[DicomTag.ReviewDate].Error);
    }

    [Fact]
    public async Task GivenV2Enabled_WhenPrivateTagInvalid_ExpectTagValidatedAndWarningProduced()
    {
        DicomItem item = new DicomAgeString(new DicomTag(0007, 0008), "Invalid Private Age Tag");
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);
        dicomDataset.Add(item);

        var result = await _dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null);

        Assert.True(result.InvalidTagErrors.Any());
        Assert.Single(result.InvalidTagErrors);
        Assert.Equal("""DICOM100: (0007,0008) - Content "Invalid Private Age Tag" does not validate VR AS: value does not have pattern 000[DWMY]""", result.InvalidTagErrors[item.Tag].Error);
    }

    [Fact]
    public async Task GivenV2Enabled_WhenItemNotADicomElement_ExpectTagValidationSkippedAndErrorNotProduced()
    {
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);
        dicomDataset.Add(new DicomOtherByteFragment(DicomTag.ReviewDate));

        var result = await _dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.Empty(result.InvalidTagErrors);
    }

    [Fact]
    public async Task GivenV2Enabled_WhenItemAnEmptyNotStringType_ExpectTagValidationNotSkippedAndErrorNotProduced()
    {
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);
        dicomDataset.Add(new DicomSignedLong(DicomTag.PregnancyStatus, new int[] { }));

        var result = await _dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.Empty(result.InvalidTagErrors);
    }

    [Theory]
    [InlineData("X\0\0\0\0")]
    [InlineData("\0")]
    [InlineData("X")]
    public async Task GivenV2Enabled_WhenNonCoreTagPaddedWithNNulls_ExpectTagValidatedAndNoErrorProduced(string value)
    {
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);
        dicomDataset.Add(DicomTag.Modality, value);

        var result = await _dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.Empty(result.InvalidTagErrors);
    }

    [Theory]
    [InlineData("Before Null Character, \0\0\0\0")]
    [InlineData("Before Null Character, \0")]
    [InlineData("Before Null Character")]
    public async Task GivenV2Enabled_WhenPatientIDPAddedWithNNulls_ExpectTagValidatedAndNoErrorProduced(string value)
    {
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(
            validateItems: false,
            patientId: value);

        var result = await _dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.Empty(result.InvalidTagErrors);
    }

    [Theory]
    [InlineData("123,345")]
    public async Task GivenV2Enabled_WhenPatientIDPAddedWithComma_ExpectTagValidatedAndNoErrorProduced(string value)
    {
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(
            validateItems: false,
            patientId: value);

        var result = await _dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.Empty(result.InvalidTagErrors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("     ")]
    [InlineData(null)]
    public async Task GivenV2Enabled_WhenPatientIDTagPresentAndValueEmpty_ExpectTagValidatedAndWarningsProduced(string value)
    {
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(
            validateItems: false,
            patientId: value);

        if (value == null)
            dicomDataset.AddOrUpdate(DicomTag.PatientID, new string[] { null });

        var result = await _dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.True(result.InvalidTagErrors.Any());
        Assert.Single(result.InvalidTagErrors);
        Assert.False(result.HasCoreTagError);
        Assert.False(result.InvalidTagErrors[DicomTag.PatientID].IsRequiredCoreTag);
        Assert.Equal("DICOM100: (0010,0020) - The required tag '(0010,0020)' is missing.", result.InvalidTagErrors[DicomTag.PatientID].Error);
    }

    [Fact]
    public async Task GivenV2Enabled_WhenPatientIDTagNotPresent_ExpectErrorProduced()
    {
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(
            validateItems: false);
        dicomDataset.Remove(DicomTag.PatientID);

        var result = await _dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.True(result.InvalidTagErrors.Any());
        Assert.Single(result.InvalidTagErrors);
        Assert.True(result.HasCoreTagError);
        Assert.True(result.InvalidTagErrors[DicomTag.PatientID].IsRequiredCoreTag);
        Assert.Equal("DICOM100: (0010,0020) - The required tag '(0010,0020)' is missing.", result.InvalidTagErrors[DicomTag.PatientID].Error);
    }

    [Fact]
    public async Task GivenV2Enabled_WhenNonRequiredTagNull_ExpectTagValidatedAndNoErrorProduced()
    {
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);
        dicomDataset.Add(DicomTag.AcquisitionDateTime, new string[] { null });
        dicomDataset.AddOrUpdate(DicomTag.PatientName, new string[] { null });
        dicomDataset.Add(DicomTag.WindowCenterWidthExplanation, new string[] { null });

        var result = await _dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.Empty(result.InvalidTagErrors);
    }

    [Fact(Skip = "Issue with minimum validation implementation, Fixing as part of https://github.com/microsoft/dicom-server/pull/3283")]
    public async Task GivenV2Enabled_WhenCoreTagUidWithLeadingZeroes_ExpectTagValidatedAndNoOnlyWarningProduced()
    {
        // For Core Tag validation like studyInstanceUid, we expect to use minimum validator which is more lenient
        // than fo-dicom's validator and allows things like leading zeroes in the UID
        // We want the validation to *not* produce any errors and therefore not cause any failures
        // However, we do want to still produce a warning for the end user so they are aware their instance may have issues
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(
            validateItems: false,
            studyInstanceUid: "1.3.6.1.4.1.55648.014924617884283217793330176991551322645.2.1");

        var result = await _dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.Single(result.InvalidTagErrors);
        Assert.False(result.InvalidTagErrors.Values.First().IsRequiredCoreTag); // we only fail when invalid core tags are present
        Assert.Contains("does not validate VR UI: components must not have leading zeros", result.InvalidTagErrors.Values.First().Error);
    }

    [Fact]
    public async Task GivenV2Enabled_WhenValidSequenceTag_ExpectTagValidatedAndNoErrorProduced()
    {
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);
        dicomDataset.Add(new DicomSequence(
            DicomTag.RegistrationSequence,
            new DicomDataset
            {
                { DicomTag.PatientName, "Test^Patient" },
                { DicomTag.PixelData, new byte[] { 1, 2, 3 } },
                {DicomTag.AcquisitionDateTime, new string[] { null }}
            }));

        var result = await _dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.Empty(result.InvalidTagErrors);
    }

    [Fact]
    public async Task GivenV2Enabled_WhenValidSequenceTagWithInnerSequences_ExpectTagValidatedAndNoErrorProduced()
    {
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);
        dicomDataset.Add(
            new DicomSequence(
                DicomTag.RegistrationSequence,
                new DicomDataset
                {
                    { DicomTag.PatientName, "Test^Patient" },
                    new DicomSequence(
                        DicomTag.RegistrationSequence,
                        new DicomDataset
                        {
                            { DicomTag.PatientName, "Test^Patient" },
                            new DicomSequence(
                                DicomTag.RegistrationSequence,
                                new DicomDataset
                                {
                                    { DicomTag.PatientName, "Test^Patient" },
                                })
                        })
                }));

        var result = await _dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.Empty(result.InvalidTagErrors);
    }

    [Fact]
    public async Task GivenV2Enabled_WhenValidSequenceTagInvalidInnerNullPaddedValues_ExpectTagValidatedAndNoErrorProduced()
    {
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);
        var sq = new DicomDataset();
        sq.NotValidated();
        sq.AddOrUpdate(DicomTag.ReviewDate, "NotAValidReviewDate");

        dicomDataset.Add(new DicomSequence(DicomTag.RegistrationSequence, sq));


        var result = await _dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.Single(result.InvalidTagErrors);
        Assert.Equal("""DICOM100: (300e,0004) - Content "NotAValidReviewDate" does not validate VR DA: one of the date values does not match the pattern YYYYMMDD""", result.InvalidTagErrors[DicomTag.ReviewDate].Error);
    }

    [Fact]
    public async Task GivenV2Enabled_WhenValidSequenceTagWithInvalidNestedValue_ExpectTagValidatedAndErrorProduced()
    {
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);

        var sq = new DicomDataset();
        sq.NotValidated();
        sq.AddOrUpdate(DicomTag.ReviewDate, "NotAValidReviewDate");

        var ds = new DicomDataset();
        ds.NotValidated();
        ds.Add(DicomTag.PatientName, "Test^Patient");
        ds.Add(DicomTag.RegistrationSequence, sq);

        dicomDataset.Add(new DicomSequence(
                DicomTag.RegistrationSequence,
                ds));

        var result = await _dicomDatasetValidator.ValidateAsync(
            dicomDataset,
            null,
            new CancellationToken());

        Assert.Single(result.InvalidTagErrors);
        Assert.Equal("""DICOM100: (300e,0004) - Content "NotAValidReviewDate" does not validate VR DA: one of the date values does not match the pattern YYYYMMDD""", result.InvalidTagErrors[DicomTag.ReviewDate].Error);
    }

    [Fact]
    public async Task GivenAValidDicomDataset_WhenValidated_ThenItShouldSucceed()
    {
        var result = await _dicomDatasetValidator.ValidateAsync(_dicomDataset, requiredStudyInstanceUid: null);

        Assert.Empty(result.InvalidTagErrors);
        Assert.Equal(ValidationWarnings.None, result.WarningCodes);
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

    public static IEnumerable<object[]> GetDuplicatedDicomIdentifierValues()
    {
        return new List<object[]>
        {
            new[] { DicomTag.StudyInstanceUID.ToString(), DicomTag.SeriesInstanceUID.ToString() },
            new[] { DicomTag.StudyInstanceUID.ToString(), DicomTag.SOPInstanceUID.ToString() },
            new[] { DicomTag.SeriesInstanceUID.ToString(), DicomTag.SOPInstanceUID.ToString() },
        };
    }
}
