// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
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
        public async Task<CancelWorkitemResponse> ProcessCancelAsync(DicomDataset dataset, string workitemInstanceUid, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            // Validate ProcedureStepState - Must be in SCHEDULED state
            // Validate No Transaction UID exists in the dataset
            // 

            // if (Validate(dataset, workitemInstanceUid))
            // {
            await CancelWorkitemAsync(dataset, cancellationToken).ConfigureAwait(false);
            // }

            return null;

            // return _responseBuilder.BuildCancelResponse();
        }

        private async Task CancelWorkitemAsync(DicomDataset dataset, CancellationToken cancellationToken)
        {
            try
            {
                // DB - Update the State
                // If there is a reason provided for cancelation, read the Blob from Blob store.
                // Add the reason

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
