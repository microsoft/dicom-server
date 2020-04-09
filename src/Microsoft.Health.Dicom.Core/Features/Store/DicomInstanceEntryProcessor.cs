// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
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
    /// Provides the functionality to process the list of <see cref="IDicomInstanceEntry"/>.
    /// </summary>
    public class DicomInstanceEntryProcessor : IDicomInstanceEntryProcessor
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

        private readonly IDicomStoreService _dicomStoreService;
        private readonly ILogger _logger;

        private readonly DicomStoreResponseBuilder _dicomStoreResponseBuilder;

        private IReadOnlyList<IDicomInstanceEntry> _dicomInstanceEntries;
        private string _requiredStudyInstanceUid;

        public DicomInstanceEntryProcessor(
            Func<DicomStoreResponseBuilder> dicomStoreResponseBuilderFactory,
            IDicomStoreService dicomStoreService,
            ILogger<DicomInstanceEntryProcessor> logger)
        {
            EnsureArg.IsNotNull(dicomStoreResponseBuilderFactory, nameof(dicomStoreResponseBuilderFactory));
            EnsureArg.IsNotNull(dicomStoreService, nameof(dicomStoreService));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _dicomStoreService = dicomStoreService;
            _dicomStoreResponseBuilder = dicomStoreResponseBuilderFactory();
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<DicomStoreResponse> ProcessAsync(IReadOnlyList<IDicomInstanceEntry> dicomInstanceEntries, string requiredStudyInstanceUid, CancellationToken cancellationToken)
        {
            if (dicomInstanceEntries != null)
            {
                _dicomInstanceEntries = dicomInstanceEntries;
                _requiredStudyInstanceUid = requiredStudyInstanceUid;

                for (int index = 0; index < dicomInstanceEntries.Count; index++)
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

            return _dicomStoreResponseBuilder.BuildResponse(requiredStudyInstanceUid);
        }

        private async Task ProcessDicomInstanceEntryAsync(int index, CancellationToken cancellationToken)
        {
            IDicomInstanceEntry dicomInstanceEntry = _dicomInstanceEntries[index];

            DicomDataset dicomDataset = null;

            try
            {
                // Open and validate the DICOM instance.
                dicomDataset = await dicomInstanceEntry.GetDicomDatasetAsync(cancellationToken);

                DicomDatasetMinimumRequirementValidator.Validate(dicomDataset, _requiredStudyInstanceUid);
            }
            catch (Exception ex)
            {
                ushort failureCode = DicomStoreFailureCodes.ProcessingFailure;

                switch (ex)
                {
                    case DicomValidationException _:
                        failureCode = DicomStoreFailureCodes.ValidationFailed;
                        break;

                    case DicomDatasetValidationException dicomDatasetMinimumRequirementException:
                        failureCode = dicomDatasetMinimumRequirementException.FailureCode;
                        break;
                }

                LogValidationFailedDelegate(_logger, index, failureCode, ex);

                _dicomStoreResponseBuilder.AddFailure(dicomDataset, failureCode);

                return;
            }

            try
            {
                // Store the instance.
                await _dicomStoreService.StoreDicomInstanceEntryAsync(
                    dicomInstanceEntry,
                    cancellationToken);

                LogSuccessfullyStoredDelegate(_logger, index, null);

                _dicomStoreResponseBuilder.AddSuccess(dicomDataset);
            }
            catch (Exception ex)
            {
                ushort failureCode = DicomStoreFailureCodes.ProcessingFailure;

                switch (ex)
                {
                    case DicomDataStoreException dicomDatasStoreException
                    when dicomDatasStoreException.StatusCode == (int)HttpStatusCode.Conflict:
                        failureCode = DicomStoreFailureCodes.SopInstanceAlreadyExists;
                        break;
                }

                LogFailedToStoreDelegate(_logger, index, failureCode, ex);

                _dicomStoreResponseBuilder.AddFailure(dicomDataset, failureCode);
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
