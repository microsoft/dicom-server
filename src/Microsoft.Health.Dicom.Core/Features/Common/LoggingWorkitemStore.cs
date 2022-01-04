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
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    public class LoggingWorkitemStore : IWorkitemStore
    {
        private static readonly Action<ILogger, string, Exception> LogAddWorkitemDelegate =
               LoggerMessage.Define<string>(
                   LogLevel.Debug,
                   default,
                   "Storing DICOM instance workitem file with '{WorkitemInstanceIdentifier}'.");

        private static readonly Action<ILogger, Exception> LogOperationSucceededDelegate =
            LoggerMessage.Define(
                LogLevel.Debug,
                default,
                "The operation completed successfully.");

        private static readonly Action<ILogger, Exception> LogOperationFailedDelegate =
            LoggerMessage.Define(
                LogLevel.Warning,
                default,
                "The operation failed.");

        private readonly IWorkitemStore _workitemStore;
        private readonly ILogger _logger;

        public LoggingWorkitemStore(IWorkitemStore workitemStore, ILogger<LoggingMetadataStore> logger)
        {
            EnsureArg.IsNotNull(workitemStore, nameof(workitemStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _workitemStore = workitemStore;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task AddWorkitemAsync(WorkitemInstanceIdentifier identifier, DicomDataset dataset, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(identifier, nameof(identifier));

            LogAddWorkitemDelegate(_logger, identifier.ToString(), null);

            try
            {
                await _workitemStore.AddWorkitemAsync(identifier, dataset, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }
    }
}
