// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Messages.Workitem;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

/// <summary>
/// Provides functionality to process the search request based on the Workitem Instance UID.
/// </summary>
public partial class WorkitemService
{
    /// <inheritdoc />
    public async Task<RetrieveWorkitemResponse> ProcessRetrieveAsync(string workitemInstanceUid, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotEmptyOrWhiteSpace(workitemInstanceUid, nameof(workitemInstanceUid));

        try
        {
            var workitemMetadata = await _workitemOrchestrator
                .GetWorkitemMetadataAsync(workitemInstanceUid, cancellationToken)
                .ConfigureAwait(false);

            if (workitemMetadata == null)
            {
                _responseBuilder.AddFailure(
                    FailureReasonCodes.UpsInstanceNotFound,
                    DicomCoreResource.WorkitemInstanceNotFound);

                return _responseBuilder.BuildRetrieveWorkitemResponse();
            }

            var dicomDataset = await _workitemOrchestrator
                .RetrieveWorkitemAsync(workitemMetadata, cancellationToken)
                .ConfigureAwait(false);

            _responseBuilder.AddSuccess(dicomDataset);

            _logger.LogInformation("Successfully retrieved the DICOM instance work-item entry.");
        }
        catch (Exception ex)
        {
            ushort failureCode = FailureReasonCodes.ProcessingFailure;

            _logger.LogWarning(ex, "Failed to retrieve the DICOM instance work-item entry. Failure code: {FailureCode}.", failureCode);
            switch (ex)
            {
                case DataStoreException:
                case ItemNotFoundException:
                    failureCode = FailureReasonCodes.UpsInstanceNotFound;
                    break;
            }

            _responseBuilder.AddFailure(failureCode, ex.Message);
        }

        return _responseBuilder.BuildRetrieveWorkitemResponse();
    }
}
