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
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.Store;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;
using DicomValidationException = FellowOakDicom.DicomValidationException;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store;

public class DicomStoreServiceTests
{
    private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;
    private static readonly StoreResponse DefaultResponse = new StoreResponse(StoreResponseStatus.Success, new DicomDataset(), null);
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

    private readonly IStoreResponseBuilder _storeResponseBuilder = Substitute.For<IStoreResponseBuilder>();
    private readonly IStoreDatasetValidator _dicomDatasetValidator = Substitute.For<IStoreDatasetValidator>();
    private readonly IStoreOrchestrator _storeOrchestrator = Substitute.For<IStoreOrchestrator>();
    private readonly IElementMinimumValidator _minimumValidator = Substitute.For<IElementMinimumValidator>();
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
    private readonly IDicomRequestContext _dicomRequestContext = Substitute.For<IDicomRequestContext>();
    private readonly IDicomTelemetryClient _telemetryClient = Substitute.For<IDicomTelemetryClient>();

    private readonly StoreService _storeService;
    private readonly StoreService _storeServiceDropData;

    public DicomStoreServiceTests()
    {
        _storeResponseBuilder.BuildResponse(Arg.Any<string>()).Returns(DefaultResponse);
        _dicomRequestContextAccessor.RequestContext.Returns(_dicomRequestContext);

        _dicomDatasetValidator
            .ValidateAsync(Arg.Any<DicomDataset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(DefaultStoreValidationResult));

        _storeService = new StoreService(
            _storeResponseBuilder,
            _dicomDatasetValidator,
            _storeOrchestrator,
            _dicomRequestContextAccessor,
            _telemetryClient,
            NullLogger<StoreService>.Instance,
            Options.Create(new FeatureConfiguration { EnableDropInvalidDicomJsonMetadata = false }));

        IOptions<FeatureConfiguration> featureConfiguration = Options.Create(
            new FeatureConfiguration { EnableDropInvalidDicomJsonMetadata = true });

        _storeServiceDropData = new StoreService(
            new StoreResponseBuilder(new MockUrlResolver(), featureConfiguration),
            CreateStoreDatasetValidatorWithDropDataEnabled(),
            _storeOrchestrator,
            _dicomRequestContextAccessor,
            _telemetryClient,
            NullLogger<StoreService>.Instance,
            featureConfiguration);

        DicomValidationBuilderExtension.SkipValidation(null);
    }

    private static IStoreDatasetValidator CreateStoreDatasetValidatorWithDropDataEnabled()
    {
        IQueryTagService queryTagService = Substitute.For<IQueryTagService>();
        List<QueryTag> queryTags = new List<QueryTag>(QueryTagService.CoreQueryTags);
        queryTagService
            .GetQueryTagsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<QueryTag>(QueryTagService.CoreQueryTags));
        TelemetryClient telemetryClient = new TelemetryClient(new TelemetryConfiguration
        {
            TelemetryChannel = Substitute.For<ITelemetryChannel>(),
        });

        IStoreDatasetValidator validator = new StoreDatasetValidator(
            Options.Create(new FeatureConfiguration()
            {
                EnableFullDicomItemValidation = false,
                EnableDropInvalidDicomJsonMetadata = true
            }),
            new ElementMinimumValidator(),
            queryTagService,
            telemetryClient);
        return validator;
    }

    [Fact]
    public async Task GivenNullDicomInstanceEntries_WhenProcessed_ThenNoContentShouldBeReturned()
    {
        await ExecuteAndValidateAsync(dicomInstanceEntries: null);

        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default, DefaultStoreValidationResult);
        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddFailure(default);
    }

    [Fact]
    public async Task GivenEmptyDicomInstanceEntries_WhenProcessed_ThenNoContentShouldBeReturned()
    {
        await ExecuteAndValidateAsync(new IDicomInstanceEntry[0]);

        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default, DefaultStoreValidationResult);
        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddFailure(default);
    }

    [Fact]
    public async Task GivenAValidDicomInstanceEntry_WhenProcessed_ThenSuccessfulEntryShouldBeAdded()
    {
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset1);

        await ExecuteAndValidateAsync(dicomInstanceEntry);

        _storeResponseBuilder.Received(1).AddSuccess(_dicomDataset1, DefaultStoreValidationResult, null);
        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddFailure(default);
        Assert.Equal(1, _dicomRequestContextAccessor.RequestContext.PartCount);
    }

    [Fact]
    public async Task GiveAnInvalidDicomDataset_WhenProcessed_ThenFailedEntryShouldBeAddedWithProcessingFailure()
    {
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns<DicomDataset>(_ => throw new Exception());

        await ExecuteAndValidateAsync(dicomInstanceEntry);

        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default, DefaultStoreValidationResult);
        _storeResponseBuilder.Received(1).AddFailure(null, TestConstants.ProcessingFailureReasonCode);
    }

    [Fact]
    public async Task GivenADicomDatasetFailsToOpenDueToDicomValidationException_WhenProcessed_ThenFailedEntryShouldBeAddedWithValidationFailure()
    {
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns<DicomDataset>(_ => throw new DicomValidationException("value", DicomVR.UI, string.Empty));

        await ExecuteAndValidateAsync(dicomInstanceEntry);

        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default, DefaultStoreValidationResult);
        _storeResponseBuilder.Received(1).AddFailure(null, TestConstants.ValidationFailureReasonCode);
    }

    [Fact]
    public async Task GivenAValidationError_WhenProcessed_ThenFailedEntryShouldBeAddedWithValidationFailure()
    {
        const ushort failureCode = 500;

        _dicomDatasetValidator
            .When(validator => validator.ValidateAsync(Arg.Any<DicomDataset>(), Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new DatasetValidationException(failureCode, "test"));

        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset2);

        await ExecuteAndValidateAsync(dicomInstanceEntry);

        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default, DefaultStoreValidationResult);
        _storeResponseBuilder.Received(1).AddFailure(_dicomDataset2, failureCode);
    }

    [Fact]
    public async Task GivenAValidationError_WhenDropDataEnabled_ThenSucceedsWithErrorsInCommentsSequence()
    {
        // setup
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);
        dicomDataset.Add(DicomTag.StudyDate, "NotAValidStudyDate");

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(dicomDataset);

        // call
        StoreResponse response = await _storeServiceDropData.ProcessAsync(
            new[] { dicomInstanceEntry },
            null,
            cancellationToken: DefaultCancellationToken);

        // assert response was successful
        Assert.Equal(StoreResponseStatus.Success, response.Status);
        Assert.Null(response.Warning);

        // expect a single refSop sequence
        DicomSequence refSopSequence = response.Dataset.GetSequence(DicomTag.ReferencedSOPSequence);
        Assert.Single(refSopSequence);

        DicomDataset firstInstance = refSopSequence.Items[0];

        // expect a comment sequence present
        DicomSequence commentSequence = firstInstance.GetSequence(DicomTag.CalculationCommentSequence);
        Assert.Single(commentSequence);

        // expect comment sequence has single warning about single invalid attribute
        Assert.Equal(
            "Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag.QueryTag - Dicom element 'StudyDate' failed validation for VR 'DA': Value cannot be parsed as a valid date.",
            commentSequence.Items[0].GetString(DicomTag.ErrorComment)
        );

        // expect that what we attempt to store has invalid attrs dropped
        Assert.Throws<DicomDataException>(() => dicomDataset.GetString(DicomTag.StudyDate));

        await _storeOrchestrator
            .Received(1)
            .StoreDicomInstanceEntryAsync(
                dicomInstanceEntry,
                dicomDataset,
                DefaultCancellationToken
            );
    }

    [Fact]
    public async Task GivenADicomInstanceAlreadyExistsExceptionWithConflictWhenStoring_WhenProcessed_ThenFailedEntryShouldBeAddedWithSopInstanceAlreadyExists()
    {
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset2);

        _storeOrchestrator
            .When(dicomStoreService => dicomStoreService.StoreDicomInstanceEntryAsync(dicomInstanceEntry, _dicomDataset2, DefaultCancellationToken))
            .Do(_ => throw new InstanceAlreadyExistsException());

        await ExecuteAndValidateAsync(dicomInstanceEntry);

        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default, DefaultStoreValidationResult);
        _storeResponseBuilder.Received(1).AddFailure(_dicomDataset2, TestConstants.SopInstanceAlreadyExistsReasonCode);
    }

    [Fact]
    public async Task GivenAnExceptionWhenStoring_WhenProcessed_ThenFailedEntryShouldBeAddedWithProcessingFailure()
    {
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset2);

        _storeOrchestrator
            .When(dicomStoreService => dicomStoreService.StoreDicomInstanceEntryAsync(dicomInstanceEntry, _dicomDataset2, DefaultCancellationToken))
            .Do(_ => throw new DataStoreException("Simulated failure."));

        await ExecuteAndValidateAsync(dicomInstanceEntry);

        _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default, DefaultStoreValidationResult);
        _storeResponseBuilder.Received(1).AddFailure(_dicomDataset2, TestConstants.ProcessingFailureReasonCode);
    }

    [Fact]
    public async Task GivenMultipleDicomInstanceEntries_WhenProcessed_ThenCorrespondingEntryShouldBeAdded()
    {
        IDicomInstanceEntry dicomInstanceEntryToSucceed = Substitute.For<IDicomInstanceEntry>();
        IDicomInstanceEntry dicomInstanceEntryToFail = Substitute.For<IDicomInstanceEntry>();

        dicomInstanceEntryToSucceed.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset1);
        dicomInstanceEntryToFail.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset2);

        _dicomDatasetValidator
            .When(datasetVaidator => datasetVaidator.ValidateAsync(_dicomDataset2, null, Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                throw new Exception();
            });

        await ExecuteAndValidateAsync(dicomInstanceEntryToSucceed, dicomInstanceEntryToFail);

        _storeResponseBuilder.Received(1).AddSuccess(_dicomDataset1, DefaultStoreValidationResult, null);
        _storeResponseBuilder.Received(1).AddFailure(_dicomDataset2, TestConstants.ProcessingFailureReasonCode);
        Assert.Equal(2, _dicomRequestContextAccessor.RequestContext.PartCount);
    }

    [Fact]
    public async Task GivenRequiredStudyInstanceUid_WhenProcessed_ThenItShouldBePassed()
    {
        IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

        dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset2);

        await ExecuteAndValidateAsync(dicomInstanceEntry);
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
