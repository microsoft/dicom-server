// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to process the list of <see cref="IDicomInstanceEntry"/>.
    /// </summary>
    public partial class WorkitemService
    {
        private static readonly Action<ILogger, ushort, Exception> LogFailedToCancelDelegate =
            LoggerMessage.Define<ushort>(
                LogLevel.Warning,
                default,
                "Failed to cancel the work-item entry. Failure code: {FailureCode}.");

        private static readonly Action<ILogger, Exception> LogSuccessfullyCanceledDelegate =
            LoggerMessage.Define(
                LogLevel.Information,
                default,
                "Successfully canceled the work-item entry.");

        public async Task<CancelWorkitemResponse> ProcessCancelAsync(DicomDataset dataset, string workitemInstanceUid, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            GetValidator<CancelWorkitemDatasetValidator>().Validate(dataset, workitemInstanceUid);

            await CancelWorkitemAsync(dataset, workitemInstanceUid, cancellationToken).ConfigureAwait(false);

            return _responseBuilder.BuildCancelResponse();
        }

        private async Task CancelWorkitemAsync(DicomDataset dataset, string workitemInstanceUid, CancellationToken cancellationToken)
        {
            try
            {
                await _workitemOrchestrator.CancelWorkitemAsync(workitemInstanceUid, cancellationToken).ConfigureAwait(false);

                LogSuccessfullyCanceledDelegate(_logger, null);

                _responseBuilder.AddSuccess(dataset);
            }
            catch (Exception ex)
            {
                ushort failureCode = FailureReasonCodes.ProcessingFailure;

                LogFailedToCancelDelegate(_logger, failureCode, ex);

                _responseBuilder.AddFailure(dataset, failureCode);
            }
        }
    }
}
