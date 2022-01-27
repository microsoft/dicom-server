// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;
using DicomValidationException = FellowOakDicom.DicomValidationException;

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

        private static readonly Action<ILogger, ushort, Exception> LogFailedToAddDelegate =
            LoggerMessage.Define<ushort>(
                LogLevel.Warning,
                default,
                "Failed to store the DICOM instance work-item entry. Failure code: {FailureCode}.");

        private static readonly Action<ILogger, Exception> LogSuccessfullyAddedDelegate =
            LoggerMessage.Define(
                LogLevel.Information,
                default,
                "Successfully stored the DICOM instance work-item entry.");

        private readonly IAddWorkitemResponseBuilder _responseBuilder;
        private readonly IAddWorkitemDatasetValidator _validator;
        private readonly IWorkitemOrchestrator _workitemOrchestrator;
        private readonly IElementMinimumValidator _minimumValidator;
        private readonly ILogger _logger;

        public WorkitemService(
            IAddWorkitemResponseBuilder storeResponseBuilder,
            IAddWorkitemDatasetValidator dicomDatasetValidator,
            IWorkitemOrchestrator storeOrchestrator,
            IElementMinimumValidator minimumValidator,
            ILogger<WorkitemService> logger)
        {
            _responseBuilder = EnsureArg.IsNotNull(storeResponseBuilder, nameof(storeResponseBuilder));
            _validator = EnsureArg.IsNotNull(dicomDatasetValidator, nameof(dicomDatasetValidator));
            _workitemOrchestrator = EnsureArg.IsNotNull(storeOrchestrator, nameof(storeOrchestrator));
            _minimumValidator = EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public async Task<AddWorkitemResponse> ProcessAsync(DicomDataset dataset, string workitemInstanceUid, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            Prepare(dataset);

            if (Validate(dataset, workitemInstanceUid))
            {
                await AddWorkitemAsync(dataset, cancellationToken).ConfigureAwait(false);
            }

            return _responseBuilder.BuildResponse();
        }

        private static void Prepare(DicomDataset dataset)
        {
            if (!dataset.TryGetString(DicomTag.ProcedureStepState, out var _))
            {
                dataset.Add(DicomTag.ProcedureStepState, ProcedureStepState.Scheduled);
            }
        }

        private bool Validate(DicomDataset dataset, string workitemInstanceUid)
        {
            try
            {
                _validator.Validate(dataset, workitemInstanceUid);
                return true;
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

                _responseBuilder.AddFailure(dataset, failureCode);

                return false;
            }
        }

        private async Task AddWorkitemAsync(DicomDataset dataset, CancellationToken cancellationToken)
        {
            try
            {
                await _workitemOrchestrator.AddWorkitemAsync(dataset, cancellationToken).ConfigureAwait(false);

                LogSuccessfullyAddedDelegate(_logger, null);

                _responseBuilder.AddSuccess(dataset);
            }
            catch (Exception ex)
            {
                ushort failureCode = FailureReasonCodes.ProcessingFailure;

                switch (ex)
                {
                    case WorkitemAlreadyExistsException _:
                        failureCode = FailureReasonCodes.SopInstanceAlreadyExists;
                        break;
                }

                LogFailedToAddDelegate(_logger, failureCode, ex);

                _responseBuilder.AddFailure(dataset, failureCode);
            }
        }
    }
}
