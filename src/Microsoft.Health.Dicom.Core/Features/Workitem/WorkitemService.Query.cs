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
        private static readonly Action<ILogger, ushort, Exception> LogFailedToQueryDelegate =
            LoggerMessage.Define<ushort>(
                LogLevel.Warning,
                default,
                "Failed to query the DICOM instance work-item entry. Failure code: {FailureCode}.");

        private static readonly Action<ILogger, Exception> LogSuccessfullyQueriedDelegate =
            LoggerMessage.Define(
                LogLevel.Information,
                default,
                "Successfully queried the DICOM instance work-item entry.");

        public async Task<QueryWorkitemResourceResponse> ProcessQueryAsync(BaseQueryParameters parameters, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(parameters, nameof(parameters));

            try
            {
                var result = await _workitemOrchestrator.QueryAsync(parameters, cancellationToken).ConfigureAwait(false);

                LogSuccessfullyQueriedDelegate(_logger, null);

                return result;
            }
            catch (Exception ex)
            {
                ushort failureCode = FailureReasonCodes.ProcessingFailure;

                LogFailedToQueryDelegate(_logger, failureCode, ex);

                throw;
            }
        }
    }
}
