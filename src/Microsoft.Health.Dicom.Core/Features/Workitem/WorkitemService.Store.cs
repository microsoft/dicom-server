// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;
using DicomValidationException = Dicom.DicomValidationException;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to process the list of <see cref="IDicomInstanceEntry"/>.
    /// </summary>
    public partial class WorkitemService : IWorkitemService
    {
        private static readonly Action<ILogger, ushort, Exception> LogValidationFailedDelegate =
            LoggerMessage.Define<ushort>(
                LogLevel.Information,
                default,
                "Validation failed for the DICOM instance work-item entry. Failure code: {FailureCode}.");

        private static readonly Action<ILogger, ushort, Exception> LogFailedToStoreDelegate =
            LoggerMessage.Define<ushort>(
                LogLevel.Warning,
                default,
                "Failed to store the DICOM instance work-item entry. Failure code: {FailureCode}.");

        private static readonly Action<ILogger, Exception> LogSuccessfullyStoredDelegate =
            LoggerMessage.Define(
                LogLevel.Information,
                default,
                "Successfully stored the DICOM instance work-item entry.");

        private readonly IWorkitemStoreResponseBuilder _storeResponseBuilder;
        private readonly IWorkitemStoreDatasetValidator _dicomDatasetValidator;
        private readonly IWorkitemOrchestrator _storeOrchestrator;
        private readonly IElementMinimumValidator _minimumValidator;
        private readonly ILogger _logger;

        public WorkitemService(
            IWorkitemStoreResponseBuilder storeResponseBuilder,
            IWorkitemStoreDatasetValidator dicomDatasetValidator,
            IWorkitemOrchestrator storeOrchestrator,
            IElementMinimumValidator minimumValidator,
            ILogger<StoreService> logger)
        {
            _storeResponseBuilder = EnsureArg.IsNotNull(storeResponseBuilder, nameof(storeResponseBuilder));
            _dicomDatasetValidator = EnsureArg.IsNotNull(dicomDatasetValidator, nameof(dicomDatasetValidator));
            _storeOrchestrator = EnsureArg.IsNotNull(storeOrchestrator, nameof(storeOrchestrator));
            _minimumValidator = EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public async Task<WorkitemStoreResponse> ProcessAsync(DicomDataset dataset, string workitemInstanceUid, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            try
            {
                dataset.Add(DicomTag.SOPInstanceUID, workitemInstanceUid);
                // TODO: Add a method to setup workitem with additional data-points. (including, may be "creating" a workitem instance uid)
                // await _dicomDatasetValidator.ValidateAsync(dataset, workitemInstanceUid, cancellationToken);
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

                LogValidationFailedDelegate(_logger, failureCode, ex);

                _storeResponseBuilder.AddFailure(dataset, failureCode);

                return _storeResponseBuilder.BuildResponse(workitemInstanceUid);
            }

            try
            {
                // Store the instance.
                await _storeOrchestrator.AddWorkitemAsync(dataset, cancellationToken);

                LogSuccessfullyStoredDelegate(_logger, null);

                _storeResponseBuilder.AddSuccess(dataset);
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

                LogFailedToStoreDelegate(_logger, failureCode, ex);

                _storeResponseBuilder.AddFailure(dataset, failureCode);
            }

            return _storeResponseBuilder.BuildResponse(workitemInstanceUid);
        }
    }
}
