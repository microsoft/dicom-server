// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.Store;
using DicomValidationException = Dicom.DicomValidationException;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
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
        private readonly IDicomDatasetValidator _dicomDatasetValidator;
        private readonly IStoreOrchestrator _storeOrchestrator;
        private readonly ILogger _logger;

        private IReadOnlyList<IDicomInstanceEntry> _dicomInstanceEntries;
        private string _requiredStudyInstanceUid;

        public StoreService(
            IStoreResponseBuilder storeResponseBuilder,
            IDicomDatasetValidator dicomDatasetValidator,
            IStoreOrchestrator storeOrchestrator,
            ILogger<StoreService> logger)
        {
            EnsureArg.IsNotNull(storeResponseBuilder, nameof(storeResponseBuilder));
            EnsureArg.IsNotNull(dicomDatasetValidator, nameof(dicomDatasetValidator));
            EnsureArg.IsNotNull(storeOrchestrator, nameof(storeOrchestrator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _storeResponseBuilder = storeResponseBuilder;
            _dicomDatasetValidator = dicomDatasetValidator;
            _storeOrchestrator = storeOrchestrator;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<StoreResponse> ProcessAsync(
            IReadOnlyList<IDicomInstanceEntry> instanceEntries,
            string requiredStudyInstanceUid,
            CancellationToken cancellationToken)
        {
            if (instanceEntries != null)
            {
                _dicomInstanceEntries = instanceEntries;
                _requiredStudyInstanceUid = requiredStudyInstanceUid;

                for (int index = 0; index < instanceEntries.Count; index++)
                {
                    try
                    {
                        await ProcessDicomInstanceEntryAsync(index, cancellationToken);
                    }
                    finally
                    {
                        // Fire and forget.
                        int capturedIndex = index;

                        _ = Task.Run(() => DisposeResourceAsync(capturedIndex));
                    }
                }
            }

            return _storeResponseBuilder.BuildResponse(requiredStudyInstanceUid);
        }

        private async Task ProcessDicomInstanceEntryAsync(int index, CancellationToken cancellationToken)
        {
            IDicomInstanceEntry dicomInstanceEntry = _dicomInstanceEntries[index];

            DicomDataset dicomDataset = null;

            try
            {
                // Open and validate the DICOM instance.
                dicomDataset = await dicomInstanceEntry.GetDicomDatasetAsync(cancellationToken);

                _dicomDatasetValidator.Validate(dicomDataset, _requiredStudyInstanceUid);
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

                return;
            }

            try
            {
                // Store the instance.
                await _storeOrchestrator.StoreDicomInstanceEntryAsync(
                    dicomInstanceEntry,
                    cancellationToken);

                LogSuccessfullyStoredDelegate(_logger, index, null);

                _storeResponseBuilder.AddSuccess(dicomDataset);
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
            }
        }

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
    }
}
