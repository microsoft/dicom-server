// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.Store;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using DicomValidationException = FellowOakDicom.DicomValidationException;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store;

public class DicomStoreServiceTests
{
    private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;
    private static readonly StoreResponse DefaultResponse = new StoreResponse(StoreResponseStatus.Success, new DicomDataset(), null);
    private static readonly StoreValidationResult DefaultStoreValidationResult = new StoreValidationResultBuilder().Build();
    private const string ExpectedAttributeError =
        """DICOM100: (0008,0020) - Content "NotAValidStudyDate" does not validate VR DA: one of the date values does not match the pattern YYYYMMDD""";
    private const string ExpectedIssuerOfAccessionNumberSequenceError =
        """DICOM100: (0008,0051) - Content "NotAValidStudyDate" does not validate VR DA: one of the date values does not match the pattern YYYYMMDD""";

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

    private readonly IStoreResponseBuilder _storeResponseBuilder = Substitute.For<IStoreResponseBuilder>();
    private readonly IStoreDatasetValidator _dicomDatasetValidator = Substitute.For<IStoreDatasetValidator>();
    private readonly IStoreOrchestrator _storeOrchestrator = Substitute.For<IStoreOrchestrator>();
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessorLatestApi = Substitute.For<IDicomRequestContextAccessor>();
    private readonly IDicomRequestContext _dicomRequestContext = Substitute.For<IDicomRequestContext>();
    private readonly IDicomRequestContext _dicomRequestContextLatestApi = Substitute.For<IDicomRequestContext>();
    private readonly StoreMeter _storeMeter = new StoreMeter();
    private readonly TelemetryClient _telemetryClient = new TelemetryClient(new TelemetryConfiguration()
    {
        TelemetryChannel = Substitute.For<ITelemetryChannel>(),
    });

    private readonly IDicomTelemetryClient _dicomTelemetryClient = Substitute.For<IDicomTelemetryClient>();
    private readonly StoreService _storeService;
    private readonly StoreService _storeServiceDropData;

    public DicomStoreServiceTests()
    {
        _dicomRequestContext.DataPartition = Partition.Default;
        _dicomRequestContextLatestApi.DataPartition = Partition.Default;
        _storeResponseBuilder.BuildResponse(Arg.Any<string>()).Returns(DefaultResponse);
        _dicomRequestContextAccessor.RequestContext.Returns(_dicomRequestContext);
        _dicomRequestContextAccessorLatestApi.RequestContext.Returns(_dicomRequestContextLatestApi);
        _dicomRequestContextLatestApi.Version.Returns(Convert.ToInt32(DicomApiVersions.Latest.TrimStart('v'), CultureInfo
        .CurrentCulture));

        _dicomDatasetValidator
            .ValidateAsync(Arg.Any<DicomDataset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(DefaultStoreValidationResult));

        _storeService = new StoreService(
            _storeResponseBuilder,
            _dicomDatasetValidator,
            _storeOrchestrator,
            _dicomRequestContextAccessor,
            _storeMeter,
            NullLogger<StoreService>.Instance,
            Options.Create(new FeatureConfiguration { }),
            _dicomTelemetryClient,
            _telemetryClient);

        _storeServiceDropData = new StoreService(
            new StoreResponseBuilder(new MockUrlResolver(), Options.Create(new FeatureConfiguration { })),
            CreateStoreDatasetValidatorWithDropDataEnabled(_dicomRequestContextAccessorLatestApi),
            _storeOrchestrator,
            _dicomRequestContextAccessorLatestApi,
            _storeMeter,
            NullLogger<StoreService>.Instance,
            Options.Create(new FeatureConfiguration { }),
            _dicomTelemetryClient,
            _telemetryClient);

        DicomValidationBuilderExtension.SkipValidation(null);
    }

    private static IStoreDatasetValidator CreateStoreDatasetValidatorWithDropDataEnabled(IDicomRequestContextAccessor contextAccessor)
    {
        IQueryTagService queryTagService = Substitute.For<IQueryTagService>();
        List<QueryTag> queryTags = new List<QueryTag>(QueryTagService.CoreQueryTags);
        queryTagService
            .GetQueryTagsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<QueryTag>(QueryTagService.CoreQueryTags));
        StoreMeter storeMeter = new StoreMeter();

        IStoreDatasetValidator validator = new StoreDatasetValidator(
            Options.Create(new FeatureConfiguration()
            {
                EnableFullDicomItemValidation = true
            }),
            new ElementMinimumValidator(),
            queryTagService,
            storeMeter,
            contextAccessor,
            NullLogger<StoreDatasetValidator>.Instance);
        return validator;
    }

    [Fact]
    public async Task GivenNullDicomInstanceEntries_WhenProcessed_ThenNoContentShouldBeReturned()
    {
        await ExecuteAndValidateAsync(dicomInstanceEntries: null);

        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default, DefaultStoreValidationResult, Partition.Default);
        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddFailure(default);
    }

    [Fact]
    public async Task GivenEmptyDicomInstanceEntries_WhenProcessed_ThenNoContentShouldBeReturned()
    {
        await ExecuteAndValidateAsync(new IDicomInstanceEntry[0]);

        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default, DefaultStoreValidationResult, _dicomRequestContext.DataPartition);
        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddFailure(default);
    }

    [Fact]
    public async Task GivenAValidDicomInstanceEntry_WhenProcessed_ThenSuccessfulEntryShouldBeAdded()
    {
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset1);
        long bytesStored = 100L;
        _storeOrchestrator.StoreDicomInstanceEntryAsync(dicomInstanceEntry, DefaultCancellationToken).Returns(bytesStored);

        await ExecuteAndValidateAsync(dicomInstanceEntry);

        _storeResponseBuilder.Received(1).AddSuccess(_dicomDataset1, DefaultStoreValidationResult, _dicomRequestContext.DataPartition);
        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddFailure(default);
        Assert.Equal(1, _dicomRequestContextAccessor.RequestContext.PartCount);
        Assert.Equal(bytesStored, _dicomRequestContextAccessor.RequestContext.TotalDicomEgressToStorageBytes);
    }

    [Fact]
    public async Task GiveAnInvalidDicomDataset_WhenProcessed_ThenFailedEntryShouldBeAddedWithProcessingFailure()
    {
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns<DicomDataset>(_ => throw new Exception());

        await ExecuteAndValidateAsync(dicomInstanceEntry);

        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default, DefaultStoreValidationResult, Partition.Default);
        _storeResponseBuilder.Received(1).AddFailure(null, TestConstants.ProcessingFailureReasonCode);
        Assert.Equal(0, _dicomRequestContextAccessor.RequestContext.TotalDicomEgressToStorageBytes);
        await _storeOrchestrator.DidNotReceiveWithAnyArgs().StoreDicomInstanceEntryAsync(Arg.Any<IDicomInstanceEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenADicomDatasetFailsToOpenDueToDicomValidationException_WhenProcessed_ThenFailedEntryShouldBeAddedWithValidationFailure()
    {
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns<DicomDataset>(_ => throw new DicomValidationException("value", DicomVR.UI, string.Empty));

        await ExecuteAndValidateAsync(dicomInstanceEntry);

        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default, DefaultStoreValidationResult, Partition.Default);
        _storeResponseBuilder.Received(1).AddFailure(null, TestConstants.ValidationFailureReasonCode);
        Assert.Equal(0, _dicomRequestContextAccessor.RequestContext.TotalDicomEgressToStorageBytes);
        await _storeOrchestrator.DidNotReceiveWithAnyArgs().StoreDicomInstanceEntryAsync(Arg.Any<IDicomInstanceEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenAValidationError_WhenDropDataEnabled_ThenSucceedsWithErrorsInFailedAttributesSequence()
    {
        // setup
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);
        dicomDataset.Add(DicomTag.StudyDate, "NotAValidStudyDate");

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(dicomDataset);
        long bytesStored = 100L;
        _storeOrchestrator.StoreDicomInstanceEntryAsync(dicomInstanceEntry, DefaultCancellationToken).Returns(bytesStored);

        // call
        StoreResponse response = await _storeServiceDropData.ProcessAsync(
            new[] { dicomInstanceEntry },
            null,
            cancellationToken: DefaultCancellationToken);

        // assert response was successful
        Assert.Equal(StoreResponseStatus.PartialSuccess, response.Status);
        Assert.Null(response.Warning);

        // expect a single refSop sequence
        DicomSequence refSopSequence = response.Dataset.GetSequence(DicomTag.ReferencedSOPSequence);
        Assert.Single(refSopSequence);

        DicomDataset firstInstance = refSopSequence.Items[0];

        // expect a comment sequence present
        DicomSequence failedAttributesSequence = firstInstance.GetSequence(DicomTag.FailedAttributesSequence);
        Assert.Single(failedAttributesSequence);

        // expect comment sequence has single warning about single invalid attribute
        Assert.Equal(
            ExpectedAttributeError,
            failedAttributesSequence.Items[0].GetString(DicomTag.ErrorComment)
        );

        // expect that what we attempt to store has invalid attrs dropped
        Assert.Throws<DicomDataException>(() => dicomDataset.GetString(DicomTag.StudyDate));

        await _storeOrchestrator
            .Received(1)
            .StoreDicomInstanceEntryAsync(
                dicomInstanceEntry,
                DefaultCancellationToken
            );

        Assert.Equal(bytesStored, _dicomRequestContextAccessorLatestApi.RequestContext.TotalDicomEgressToStorageBytes);
    }

    [Fact]
    public async Task GivenAValidationErrorOnNonCoreTag_WhenDropDataEnabledAndFullDicomItemValidationEnabled_ThenSucceedsWithErrorsInFailedAttributesSequence()
    {
        // setup
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);
        dicomDataset.Add(DicomTag.StudyDate, "NotAValidStudyDate");

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(dicomDataset);
        long bytesStored = 100L;
        _storeOrchestrator.StoreDicomInstanceEntryAsync(dicomInstanceEntry, DefaultCancellationToken).Returns(bytesStored);

        // call
        StoreResponse response = await _storeServiceDropData.ProcessAsync(
            new[] { dicomInstanceEntry },
            null,
            cancellationToken: DefaultCancellationToken);

        // assert response was successful
        Assert.Equal(StoreResponseStatus.PartialSuccess, response.Status);
        Assert.Null(response.Warning);

        // expect a single refSop sequence
        DicomSequence refSopSequence = response.Dataset.GetSequence(DicomTag.ReferencedSOPSequence);
        Assert.Single(refSopSequence);

        DicomDataset firstInstance = refSopSequence.Items[0];

        // expect a comment sequence present
        DicomSequence failedAttributesSequence = firstInstance.GetSequence(DicomTag.FailedAttributesSequence);
        Assert.Single(failedAttributesSequence);

        // expect comment sequence has single warning about single invalid attribute
        Assert.Equal(
            ExpectedAttributeError,
            failedAttributesSequence.Items[0].GetString(DicomTag.ErrorComment)
        );

        // expect that what we attempt to store has invalid attrs dropped
        Assert.Throws<DicomDataException>(() => dicomDataset.GetString(DicomTag.StudyDate));

        await _storeOrchestrator
            .Received(1)
            .StoreDicomInstanceEntryAsync(
                dicomInstanceEntry,
                DefaultCancellationToken
            );

        Assert.Equal(bytesStored, _dicomRequestContextAccessorLatestApi.RequestContext.TotalDicomEgressToStorageBytes);
    }

    [Fact]
    public async Task GivenASequenceWithOnlyInvalidAttributes_WhenDropDataEnabled_ThenSequenceDropped()
    {
        // setup
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);

        dicomDataset.Add(
            new DicomSequence(
                DicomTag.IssuerOfAccessionNumberSequence,
                new DicomDataset
                {
                    { DicomTag.ReviewDate, "NotAValidReviewDate" },
                    { DicomTag.StudyDate, "NotAValidStudyDate" }
                })
            );

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(dicomDataset);

        // call
        StoreResponse response = await _storeServiceDropData.ProcessAsync(
            new[] { dicomInstanceEntry },
            null,
            cancellationToken: DefaultCancellationToken);

        // assert response was successful
        Assert.Equal(StoreResponseStatus.PartialSuccess, response.Status);
        Assert.Null(response.Warning);

        // expect a single refSop sequence
        DicomSequence refSopSequence = response.Dataset.GetSequence(DicomTag.ReferencedSOPSequence);
        Assert.Single(refSopSequence);

        DicomDataset firstInstance = refSopSequence.Items[0];

        // expect a failed attr sequence present
        DicomSequence failedAttributesSequence = firstInstance.GetSequence(DicomTag.FailedAttributesSequence);

        // even though the sequence has multiple attributes, we exist validation on the first error and only provide the
        // first error
        Assert.Single(failedAttributesSequence);

        // expect failed attr sequence has single warning about single invalid attribute, in this case the first we encounter
        // which is last in the sequence
        Assert.Equal(
            """DICOM100: (0008,0051) - Content "NotAValidStudyDate" does not validate VR DA: one of the date values does not match the pattern YYYYMMDD""",
            failedAttributesSequence.Items[0].GetString(DicomTag.ErrorComment)
        );

        // expect that what we attempt to store has invalid attrs dropped
        Assert.Throws<DicomDataException>(() => dicomDataset.GetString(DicomTag.IssuerOfAccessionNumberSequence));

        await _storeOrchestrator
            .Received(1)
            .StoreDicomInstanceEntryAsync(
                dicomInstanceEntry,
                DefaultCancellationToken
            );
    }

    [Fact]
    public async Task GivenASequenceWithOneValidAndOneInvalidAttribute_WhenDropDataEnabled_ThenSequenceDropped()
    {
        // setup
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);

        dicomDataset.Add(
            new DicomSequence(
                DicomTag.IssuerOfAccessionNumberSequence,
                new DicomDataset
                {
                    { DicomTag.StudyDate, "NotAValidStudyDate" },
                    { DicomTag.ReviewDate, "20220119" }
                })
            );

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(dicomDataset);

        // call
        StoreResponse response = await _storeServiceDropData.ProcessAsync(
            new[] { dicomInstanceEntry },
            null,
            cancellationToken: DefaultCancellationToken);

        // assert response was successful
        Assert.Equal(StoreResponseStatus.PartialSuccess, response.Status);
        Assert.Null(response.Warning);

        // expect a single refSop sequence
        DicomSequence refSopSequence = response.Dataset.GetSequence(DicomTag.ReferencedSOPSequence);
        Assert.Single(refSopSequence);

        DicomDataset firstInstance = refSopSequence.Items[0];

        // expect a failed attr sequence present
        DicomSequence failedAttributesSequence = firstInstance.GetSequence(DicomTag.FailedAttributesSequence);
        Assert.Single(failedAttributesSequence);

        // expect failed attr sequence has single warning about single invalid attribute
        Assert.Equal(
            ExpectedIssuerOfAccessionNumberSequenceError,
            failedAttributesSequence.Items[0].GetString(DicomTag.ErrorComment)
        );

        // expect that what we attempt to store has invalid attrs dropped
        Assert.Throws<DicomDataException>(() => dicomDataset.GetString(DicomTag.IssuerOfAccessionNumberSequence));

        await _storeOrchestrator
            .Received(1)
            .StoreDicomInstanceEntryAsync(
                dicomInstanceEntry,
                DefaultCancellationToken
            );
    }

    [Fact]
    public async Task GivenMultipleInstancesAndOneHasInvalidAttr_WhenDropDataEnabled_ThenSucceedsWithErrorsInFailedAttributesSequenceForOneButNotTheOther()
    {
        // setup
        IDicomInstanceEntry dicomInstanceEntryValid = Substitute.For<IDicomInstanceEntry>();
        DicomDataset validDicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);
        dicomInstanceEntryValid.GetDicomDatasetAsync(DefaultCancellationToken).Returns(validDicomDataset);

        IDicomInstanceEntry dicomInstanceEntryInvalid = Substitute.For<IDicomInstanceEntry>();
        DicomDataset invalidDicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);
        invalidDicomDataset.Add(DicomTag.StudyDate, "NotAValidStudyDate");
        dicomInstanceEntryInvalid.GetDicomDatasetAsync(DefaultCancellationToken).Returns(invalidDicomDataset);

        // call
        StoreResponse response = await _storeServiceDropData.ProcessAsync(
            new[] { dicomInstanceEntryValid, dicomInstanceEntryInvalid },
            null,
            cancellationToken: DefaultCancellationToken);

        // assert response was successful
        Assert.Equal(StoreResponseStatus.PartialSuccess, response.Status);
        Assert.Null(response.Warning);

        // expect a two refSop sequences, one for each instance
        DicomSequence refSopSequence = response.Dataset.GetSequence(DicomTag.ReferencedSOPSequence);
        Assert.Equal(2, refSopSequence.Items.Count);

        // first was valid, expect a comment sequence present, but empty value
        DicomDataset validInstanceResponse = refSopSequence.Items[0];
        Assert.Empty(validInstanceResponse.GetSequence(DicomTag.FailedAttributesSequence));

        // second was invalid, expect a comment sequence present, and not empty value
        DicomDataset invalidInstanceResponse = refSopSequence.Items[1];
        DicomSequence invalidFailedAttributesSequence = invalidInstanceResponse.GetSequence(DicomTag.FailedAttributesSequence);
        // expect comment sequence has single warning about single invalid attribute
        Assert.Equal(
            ExpectedAttributeError,
            invalidFailedAttributesSequence.Items[0].GetString(DicomTag.ErrorComment)
        );

        //expect that we stored both instances
        await _storeOrchestrator
            .Received()
            .StoreDicomInstanceEntryAsync(
                dicomInstanceEntryValid,
                DefaultCancellationToken
            );

        // expect that what we attempt to store has invalid attrs dropped for invalid instance
        Assert.Throws<DicomDataException>(() => invalidDicomDataset.GetString(DicomTag.StudyDate));
        await _storeOrchestrator
            .Received()
            .StoreDicomInstanceEntryAsync(
                dicomInstanceEntryInvalid,
                DefaultCancellationToken
            );
    }

    [Fact]
    public async Task GivenADicomInstanceAlreadyExistsExceptionWithConflictWhenStoring_WhenProcessed_ThenFailedEntryShouldBeAddedWithSopInstanceAlreadyExists()
    {
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset2);

        _storeOrchestrator
            .When(dicomStoreService => dicomStoreService.StoreDicomInstanceEntryAsync(dicomInstanceEntry, DefaultCancellationToken))
            .Do(_ => throw new InstanceAlreadyExistsException());

        await ExecuteAndValidateAsync(dicomInstanceEntry);

        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default, DefaultStoreValidationResult, Partition.Default);
        _storeResponseBuilder.Received(1).AddFailure(_dicomDataset2, TestConstants.SopInstanceAlreadyExistsReasonCode, null);
    }

    [Fact]
    public async Task GivenAnExceptionWhenStoring_WhenProcessed_ThenFailedEntryShouldBeAddedWithProcessingFailure()
    {
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset2);

        _storeOrchestrator
            .When(dicomStoreService => dicomStoreService.StoreDicomInstanceEntryAsync(dicomInstanceEntry, DefaultCancellationToken))
            .Do(_ => throw new DataStoreException("Simulated failure."));

        await ExecuteAndValidateAsync(dicomInstanceEntry);

        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default, DefaultStoreValidationResult, Partition.Default);
        _storeResponseBuilder.Received(1).AddFailure(_dicomDataset2, TestConstants.ProcessingFailureReasonCode);
    }

    [Fact]
    public async Task GivenAnExternalStoreExceptionWhenStoring_WhenProcessed_ThenExpectDataStoreExceptionRethrownAndResponseBuilderNotUsed()
    {
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset2);

        _storeOrchestrator
            .When(dicomStoreService => dicomStoreService.StoreDicomInstanceEntryAsync(dicomInstanceEntry, DefaultCancellationToken))
            .Do(_ => throw new DataStoreException("Simulated failure.", isExternal: true));

        DataStoreException exception = await Assert.ThrowsAsync<DataStoreException>(() => _storeService.ProcessAsync(new[] { dicomInstanceEntry }, null, cancellationToken: DefaultCancellationToken));

        Assert.True(exception.IsExternal);

        _storeResponseBuilder.DidNotReceiveWithAnyArgs().BuildResponse(Arg.Any<string>(), Arg.Any<bool>());

        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(Arg.Any<DicomDataset>(), Arg.Any<StoreValidationResult>(), Arg.Any<Partition>());
        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddFailure(Arg.Any<DicomDataset>(), Arg.Any<ushort>());
    }

    [Fact]
    public async Task GivenMultipleDicomInstanceEntriesWithFailure_WhenProcessed_ThenCorrespondingEntryShouldBeAdded()
    {
        IDicomInstanceEntry dicomInstanceEntryToSucceed = Substitute.For<IDicomInstanceEntry>();
        IDicomInstanceEntry dicomInstanceEntryToFail = Substitute.For<IDicomInstanceEntry>();
        long bytesStored = 100L;
        _storeOrchestrator.StoreDicomInstanceEntryAsync(Arg.Any<IDicomInstanceEntry>(), DefaultCancellationToken).Returns(bytesStored);

        dicomInstanceEntryToSucceed.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset1);
        dicomInstanceEntryToFail.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset2);

        _dicomDatasetValidator
            .When(datasetVaidator => datasetVaidator.ValidateAsync(_dicomDataset2, null, Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                throw new Exception();
            });

        await ExecuteAndValidateAsync(dicomInstanceEntryToSucceed, dicomInstanceEntryToFail);

        _storeResponseBuilder.Received(1).AddSuccess(_dicomDataset1, DefaultStoreValidationResult, _dicomRequestContext.DataPartition);
        _storeResponseBuilder.Received(1).AddFailure(_dicomDataset2, TestConstants.ProcessingFailureReasonCode);
        Assert.Equal(2, _dicomRequestContextAccessor.RequestContext.PartCount);

        await _storeOrchestrator
            .Received(1)
            .StoreDicomInstanceEntryAsync(
                dicomInstanceEntryToSucceed,
                DefaultCancellationToken
            );

        await _storeOrchestrator
            .DidNotReceive()
            .StoreDicomInstanceEntryAsync(
                dicomInstanceEntryToFail,
                DefaultCancellationToken
            );

        Assert.Equal(bytesStored, _dicomRequestContextAccessor.RequestContext.TotalDicomEgressToStorageBytes);
    }

    [Fact]
    public async Task GivenMultipleDicomInstanceEntries_WhenProcessed_ThenTotalBytesTracked()
    {
        IDicomInstanceEntry dicomInstanceEntryToSucceed = Substitute.For<IDicomInstanceEntry>();
        IDicomInstanceEntry dicomInstanceEntryToSucceed2 = Substitute.For<IDicomInstanceEntry>();
        long bytesStored = 100L;
        long totalExpectedBytesStored = bytesStored * 2;
        _storeOrchestrator.StoreDicomInstanceEntryAsync(Arg.Any<IDicomInstanceEntry>(), DefaultCancellationToken).Returns(bytesStored);

        dicomInstanceEntryToSucceed.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset1);
        dicomInstanceEntryToSucceed2.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset2);

        await ExecuteAndValidateAsync(dicomInstanceEntryToSucceed, dicomInstanceEntryToSucceed2);

        _storeResponseBuilder.Received(1).AddSuccess(_dicomDataset1, DefaultStoreValidationResult, _dicomRequestContext.DataPartition);
        _storeResponseBuilder.Received(1).AddSuccess(_dicomDataset2, DefaultStoreValidationResult, _dicomRequestContext.DataPartition);

        await _storeOrchestrator
            .Received(1)
            .StoreDicomInstanceEntryAsync(
                dicomInstanceEntryToSucceed,
                DefaultCancellationToken
            );

        await _storeOrchestrator
            .Received(1)
            .StoreDicomInstanceEntryAsync(
                dicomInstanceEntryToSucceed2,
                DefaultCancellationToken
            );

        Assert.Equal(totalExpectedBytesStored, _dicomRequestContextAccessor.RequestContext.TotalDicomEgressToStorageBytes);
    }

    [Fact]
    public async Task GivenRequiredStudyInstanceUid_WhenProcessed_ThenItShouldBePassed()
    {
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset2);

        await ExecuteAndValidateAsync(dicomInstanceEntry);
    }

    [Fact]
    public async Task GivenFetchCancellation_WhenProcessed_ThenItShouldHaveThrown()
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource();
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();
        dicomInstanceEntry.GetDicomDatasetAsync(tokenSource.Token).Returns(ValueTask.FromException<DicomDataset>(new TaskCanceledException()));

        await Assert.ThrowsAsync<TaskCanceledException>(() => _storeService.ProcessAsync(
            new IDicomInstanceEntry[] { dicomInstanceEntry },
            null,
            cancellationToken: tokenSource.Token));
    }

    [Fact]
    public async Task GivenValidationCancellation_WhenProcessed_ThenItShouldHaveThrown()
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource();
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();
        dicomInstanceEntry.GetDicomDatasetAsync(tokenSource.Token).Returns(_dicomDataset1);
        _dicomDatasetValidator.ValidateAsync(_dicomDataset1, null, tokenSource.Token).ThrowsAsync<TaskCanceledException>();

        await Assert.ThrowsAsync<TaskCanceledException>(() => _storeService.ProcessAsync(
            new IDicomInstanceEntry[] { dicomInstanceEntry },
            null,
            cancellationToken: tokenSource.Token));
    }

    [Fact]
    public async Task GivenStowCancellation_WhenProcessed_ThenItShouldHaveThrown()
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource();
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();
        dicomInstanceEntry.GetDicomDatasetAsync(tokenSource.Token).Returns(_dicomDataset1);
        _storeOrchestrator.StoreDicomInstanceEntryAsync(dicomInstanceEntry, tokenSource.Token).ThrowsAsync(new DataStoreException(new TaskCanceledException()));

        Exception actual = await Assert.ThrowsAsync<DataStoreException>(() => _storeService.ProcessAsync(
            new IDicomInstanceEntry[] { dicomInstanceEntry },
            null,
            cancellationToken: tokenSource.Token));
        Assert.IsType<TaskCanceledException>(actual.InnerException);
    }

    private Task ExecuteAndValidateAsync(params IDicomInstanceEntry[] dicomInstanceEntries)
        => ExecuteAndValidateAsync(requiredStudyInstanceUid: null, dicomInstanceEntries);

    private async Task ExecuteAndValidateAsync(
        string requiredStudyInstanceUid,
        params IDicomInstanceEntry[] dicomInstanceEntries)
    {
        StoreResponse response = await _storeService.ProcessAsync(
            dicomInstanceEntries,
            requiredStudyInstanceUid,
            cancellationToken: DefaultCancellationToken);

        Assert.Equal(DefaultResponse, response);

        _storeResponseBuilder.Received(1).BuildResponse(requiredStudyInstanceUid);

        if (dicomInstanceEntries != null)
        {
            foreach (IDicomInstanceEntry dicomInstanceEntry in dicomInstanceEntries)
            {
                await ValidateDisposeAsync(dicomInstanceEntry);
            }
        }
    }

    private static async Task ValidateDisposeAsync(IDicomInstanceEntry dicomInstanceEntry)
    {
        var timeout = DateTime.Now.AddSeconds(5);

        while (timeout < DateTime.Now)
        {
            if (dicomInstanceEntry.ReceivedCalls().Any())
            {
                await dicomInstanceEntry.Received(1).DisposeAsync();
                break;
            }

            await Task.Delay(100);
        }
    }
}
