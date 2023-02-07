// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Diagnostic;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Store;

/// <summary>
/// Provides functionality to process the list of <see cref="IDicomInstanceEntry"/>.
/// </summary>
public class StoreService : IStoreService
{
    private static readonly Action<ILogger, int, ushort, Exception> LogValidationFailedDelegate =
        LoggerMessage.Define<int, ushort>(
            LogLevel.Information,
            default,
            "Validation failed for the DICOM instance entry at index '{DicomInstanceEntryIndex}'. Failure code: {FailureCode}.");

    private static readonly Action<ILogger, int, ushort, Exception> LogValidationSucceededWithWarningDelegate =
        LoggerMessage.Define<int, ushort>(
            LogLevel.Warning,
            default,
            "Validation succeeded with warning(s) for the DICOM instance entry at index '{DicomInstanceEntryIndex}'. {WarningCode}");

    private static readonly Action<ILogger, int, ushort, Exception> LogFailedToStoreDelegate =
        LoggerMessage.Define<int, ushort>(
            LogLevel.Warning,
            default,
            "Failed to store the DICOM instance entry at index '{DicomInstanceEntryIndex}'. Failure code: {FailureCode}.");

    private static readonly Action<ILogger, int, Exception> LogSuccessfullyStoredDelegate =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            default,
            "Successfully stored the DICOM instance entry at index '{DicomInstanceEntryIndex}'.");

    private static readonly Action<ILogger, int, Exception> LogFailedToDisposeDelegate =
        LoggerMessage.Define<int>(
            LogLevel.Warning,
            default,
            "Failed to dispose the DICOM instance entry at index '{DicomInstanceEntryIndex}'.");

    private readonly IStoreResponseBuilder _storeResponseBuilder;
    private readonly IStoreDatasetValidator _dicomDatasetValidator;
    private readonly IStoreOrchestrator _storeOrchestrator;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly IDicomTelemetryClient _dicomTelemetryClient;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger _logger;

    private IReadOnlyList<IDicomInstanceEntry> _dicomInstanceEntries;
    private string _requiredStudyInstanceUid;
    private readonly bool _enableDropInvalidDicomJsonMetadata;

    public StoreService(
        IStoreResponseBuilder storeResponseBuilder,
        IStoreDatasetValidator dicomDatasetValidator,
        IStoreOrchestrator storeOrchestrator,
        IDicomRequestContextAccessor dicomRequestContextAccessor,
        IDicomTelemetryClient dicomTelemetryClient,
        ILogger<StoreService> logger,
        IOptions<FeatureConfiguration> featureConfiguration,
        TelemetryClient telemetryClient
        )
    {
        EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));
        _storeResponseBuilder = EnsureArg.IsNotNull(storeResponseBuilder, nameof(storeResponseBuilder));
        _dicomDatasetValidator = EnsureArg.IsNotNull(dicomDatasetValidator, nameof(dicomDatasetValidator));
        _storeOrchestrator = EnsureArg.IsNotNull(storeOrchestrator, nameof(storeOrchestrator));
        _dicomRequestContextAccessor = EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));
        _dicomTelemetryClient = EnsureArg.IsNotNull(dicomTelemetryClient, nameof(dicomTelemetryClient));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _enableDropInvalidDicomJsonMetadata = featureConfiguration.Value.EnableDropInvalidDicomJsonMetadata;
        _telemetryClient = EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
    }

    /// <inheritdoc />
    public async Task<StoreResponse> ProcessAsync(
        IReadOnlyList<IDicomInstanceEntry> instanceEntries,
        string requiredStudyInstanceUid,
        CancellationToken cancellationToken)
    {
        if (instanceEntries != null)
        {
            _dicomRequestContextAccessor.RequestContext.PartCount = instanceEntries.Count;
            _dicomInstanceEntries = instanceEntries;
            _requiredStudyInstanceUid = requiredStudyInstanceUid;
            _dicomTelemetryClient.TrackInstanceCount(instanceEntries.Count);

            InstanceByteMetrics? metrics = null;
            for (int index = 0; index < instanceEntries.Count; index++)
            {
                try
                {
                    long? length = await ProcessDicomInstanceEntryAsync(index, cancellationToken);
                    if (length != null)
                    {
                        long len = length.GetValueOrDefault();
                        metrics = metrics == null ? new InstanceByteMetrics(len) : metrics.GetValueOrDefault().Aggregate(len);
                    }
                }
                finally
                {
                    // Update Telemetry
                    if (metrics != null)
                    {
                        (long totalLength, long minLength, long maxLength) = metrics.GetValueOrDefault();
                        _dicomTelemetryClient.TrackTotalInstanceBytes(totalLength);
                        _dicomTelemetryClient.TrackMinInstanceBytes(minLength);
                        _dicomTelemetryClient.TrackMaxInstanceBytes(maxLength);
                    }

                    // Fire and forget.
                    int capturedIndex = index;

                    _ = Task.Run(() => DisposeResourceAsync(capturedIndex), CancellationToken.None);
                }
            }
        }

        return _storeResponseBuilder.BuildResponse(requiredStudyInstanceUid);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Will reevaluate exceptions when refactoring validation.")]
    private async Task<long?> ProcessDicomInstanceEntryAsync(int index, CancellationToken cancellationToken)
    {
        IDicomInstanceEntry dicomInstanceEntry = _dicomInstanceEntries[index];

        ushort? warningReasonCode = null;
        DicomDataset dicomDataset = null;
        StoreValidationResult storeValidatorResult = null;

        try
        {
            // Open and validate the DICOM instance.
            dicomDataset = await dicomInstanceEntry.GetDicomDatasetAsync(cancellationToken);

            storeValidatorResult = await _dicomDatasetValidator.ValidateAsync(dicomDataset, _requiredStudyInstanceUid, cancellationToken);

            // We have different ways to handle with warnings.
            // DatasetDoesNotMatchSOPClass is defined in Dicom Standards (https://dicom.nema.org/medical/dicom/current/output/chtml/part18/sect_I.2.html), put into Warning Reason dicom tag
            if ((storeValidatorResult.WarningCodes & ValidationWarnings.DatasetDoesNotMatchSOPClass) == ValidationWarnings.DatasetDoesNotMatchSOPClass)
            {
                warningReasonCode = WarningReasonCodes.DatasetDoesNotMatchSOPClass;

                LogValidationSucceededWithWarningDelegate(_logger, index, WarningReasonCodes.DatasetDoesNotMatchSOPClass, null);
            }

            // IndexedDicomTagHasMultipleValues is our warning, put into http Warning header.
            if ((storeValidatorResult.WarningCodes & ValidationWarnings.IndexedDicomTagHasMultipleValues) == ValidationWarnings.IndexedDicomTagHasMultipleValues)
            {
                _storeResponseBuilder.SetWarningMessage(DicomCoreResource.IndexedDicomTagHasMultipleValues);
            }

            // If there is an error in the required core tag, return immediately
            if (storeValidatorResult.InvalidTagErrors.Any(x => x.Value.IsRequiredCoreTag) ||
                !_enableDropInvalidDicomJsonMetadata && storeValidatorResult.InvalidTagErrors.Any())
            {
                ushort failureCode = FailureReasonCodes.ValidationFailure;
                LogValidationFailedDelegate(_logger, index, failureCode, null);
                _storeResponseBuilder.AddFailure(dicomDataset, failureCode, storeValidatorResult);
                return null;
            }

            if (_enableDropInvalidDicomJsonMetadata)
            {

                foreach (DicomTag tag in storeValidatorResult.InvalidTagErrors.Keys)
                {
                    // drop invalid metadata
                    dicomDataset.Remove(tag);

                    string message = storeValidatorResult.InvalidTagErrors[tag].Error;
                    LogForwarder.LogTrace(
                        _telemetryClient,
                        $"{message}. This attribute will be dropped from JSON metadata and can not be used to index." +
                        $" This attribute will remain in your Dicom binary file.",
                        dicomDataset);
                }
            }
        }
        catch (Exception ex)
        {
            ushort failureCode = FailureReasonCodes.ProcessingFailure;

            switch (ex)
            {
                case DicomValidationException _:
                    failureCode = FailureReasonCodes.ValidationFailure;
                    break;

                case DatasetValidationException dicomDatasetValidationException:
                    failureCode = dicomDatasetValidationException.FailureCode;
                    break;

                case ValidationException _:
                    failureCode = FailureReasonCodes.ValidationFailure;
                    break;
            }

            LogValidationFailedDelegate(_logger, index, failureCode, ex);

            _storeResponseBuilder.AddFailure(dicomDataset, failureCode);
            return null;
        }

        try
        {
            // Store the instance.
            long length = await _storeOrchestrator.StoreDicomInstanceEntryAsync(
                dicomInstanceEntry,
                cancellationToken);

            LogSuccessfullyStoredDelegate(_logger, index, null);

            _storeResponseBuilder.AddSuccess(dicomDataset, storeValidatorResult, warningReasonCode, _enableDropInvalidDicomJsonMetadata);
            return length;
        }
        catch (Exception ex)
        {
            ushort failureCode = FailureReasonCodes.ProcessingFailure;

            switch (ex)
            {
                case PendingInstanceException _:
                    failureCode = FailureReasonCodes.PendingSopInstance;
                    break;

                case InstanceAlreadyExistsException _:
                    failureCode = FailureReasonCodes.SopInstanceAlreadyExists;
                    break;
            }

            LogFailedToStoreDelegate(_logger, index, failureCode, ex);

            _storeResponseBuilder.AddFailure(dicomDataset, failureCode);
            return null;
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Ignore errors during disposal.")]
    private async Task DisposeResourceAsync(int index)
    {
        try
        {
            await _dicomInstanceEntries[index].DisposeAsync();
        }
        catch (Exception ex)
        {
            LogFailedToDisposeDelegate(_logger, index, ex);
        }
    }

    private readonly struct InstanceByteMetrics
    {
        private readonly long _total;
        private readonly long _min;
        private readonly long _max;

        public InstanceByteMetrics(long bytes)
            : this(bytes, bytes, bytes)
        { }

        private InstanceByteMetrics(long total, long min, long max)
        {
            _total = total;
            _min = min;
            _max = max;
        }

        public InstanceByteMetrics Aggregate(long length)
        {
            return new InstanceByteMetrics(_total + length, Math.Min(_min, length), Math.Max(_max, length));
        }

        public void Deconstruct(out long total, out long min, out long max)
        {
            total = _total;
            min = _min;
            max = _max;
        }
    }
}
