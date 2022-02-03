// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.Workitem;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to process the list of <see cref="IDicomInstanceEntry"/>.
    /// </summary>
    public partial class WorkitemService
    {
        public async Task<QueryWorkitemResourceResponse> ProcessQueryAsync(BaseQueryParameters parameters, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(parameters, nameof(parameters));

            try
            {
                var result = await _workitemOrchestrator.QueryAsync(parameters, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Successfully queried the DICOM instance work-item entry.");

                return result;
            }
            catch (Exception ex)
            {
                ushort failureCode = FailureReasonCodes.ProcessingFailure;

                _logger.LogWarning(ex, "Failed to query the DICOM instance work-item entry. Failure code: {FailureCode}.", failureCode);

                throw;
            }
        }
    }
}
